"""ASGI entrypoint. Bound to localhost so the service is never public."""

import uvicorn

from app.main import app
from app.settings import settings

if __name__ == "__main__":
    uvicorn.run(
        app,
        host=settings.host,
        port=settings.port,
        factory=False,
    )
