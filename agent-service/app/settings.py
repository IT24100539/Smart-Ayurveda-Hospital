from pathlib import Path

from pydantic import AliasChoices, Field, SecretStr
from pydantic_settings import BaseSettings, SettingsConfigDict

_REPO_ROOT = Path(__file__).resolve().parents[2]


class Settings(BaseSettings):
    model_config = SettingsConfigDict(
        env_prefix="AGENT_",
        env_file=(str(_REPO_ROOT / ".env"), ".env"),
        extra="ignore",
    )

    host: str = "127.0.0.1"
    port: int = 8001
    shared_secret: SecretStr = SecretStr("")
    ollama_base_url: str = "http://127.0.0.1:11434"
    ollama_model: str = "llama3.1"
    ollama_timeout_seconds: float = 90
    hospital_api_base_url: str = "http://127.0.0.1:5080"
    backend_base_url: str = "http://127.0.0.1:5080"
    environment: str = "development"
    internal_service_key: SecretStr = Field(
        default=SecretStr(""),
        validation_alias=AliasChoices("INTERNAL_SERVICE_KEY", "AGENT_INTERNAL_SERVICE_KEY"),
    )

    @property
    def is_production(self) -> bool:
        return self.environment.lower() == "production"

    def ensure_secrets(self) -> None:
        """Refuse to boot when the API→agent secret is missing. Production also rejects placeholders."""
        if not self.shared_secret.get_secret_value().strip():
            raise RuntimeError(
                "AGENT_SHARED_SECRET is missing. Set the same value in .env and as "
                "AgentService:SharedSecret on the API."
            )
        self.ensure_production_secrets()

    def ensure_production_secrets(self) -> None:
        """Refuse to boot in production when a secret is missing or still a placeholder."""
        if not self.is_production:
            return
        missing: list[str] = []
        if _is_rejected_secret(self.shared_secret.get_secret_value()):
            missing.append("AGENT_SHARED_SECRET")
        if _is_rejected_secret(self.internal_service_key.get_secret_value()):
            missing.append("INTERNAL_SERVICE_KEY")
        if missing:
            names = ", ".join(missing)
            raise RuntimeError(
                "Production startup refused. Missing or development-placeholder secrets: " + names
            )


def _is_rejected_secret(value: str) -> bool:
    trimmed = value.strip()
    if not trimmed:
        return True
    lowered = trimmed.lower()
    markers = (
        "change-me",
        "dev-only",
        "dev-internal",
        "replace-me",
        "replace-with-a-random",
        "your_",
    )
    return lowered in {"postgres", "change_me"} or any(marker in lowered for marker in markers)


settings = Settings()
settings.ensure_secrets()