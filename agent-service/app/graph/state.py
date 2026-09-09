from typing import TypedDict


class AgentState(TypedDict):
    agent: str
    prompt: str
    context: dict[str, str]
    route: str
    reply: str
    metadata: dict[str, str]
