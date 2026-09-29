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
    shared_secret: SecretStr = SecretStr("dev-internal-agent-secret")
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


settings = Settings()