from pydantic_settings import BaseSettings, SettingsConfigDict


class Settings(BaseSettings):
    model_config = SettingsConfigDict(env_prefix="AGENT_", env_file=".env", extra="ignore")

    host: str = "127.0.0.1"
    port: int = 8100
    shared_secret: str = "dev-internal-agent-secret"
    ollama_base_url: str = "http://127.0.0.1:11434"
    ollama_model: str = "llama3.1"
    backend_base_url: str = "https://localhost:7443"
    environment: str = "development"

    @property
    def is_production(self) -> bool:
        return self.environment.lower() == "production"


settings = Settings()
