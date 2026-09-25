# Agent service (local)

Ollama does not fit a free web-service tier, and `agent-service` stays off the public internet. **This group runs the whole stack locally for the live demo** (API, Postgres, staff portal, and the agent). The Render and Vercel deploy is for the hosted API and staff portal only. Do not depend on the cloud API for the agent portion of the demo.

Use a tunnel only if a demo must keep the cloud API and still reach an agent on a laptop. That is a fallback, not the plan.

## Local startup

1. Start Ollama and pull the model once (this matches `AGENT_OLLAMA_MODEL`, default `llama3.1`):

```bash
ollama serve
ollama pull llama3.1
```

2. Start the agent on loopback:

```bash
cd agent-service
python -m venv .venv
.venv\Scripts\activate
pip install -r requirements.txt
python run.py
```

It listens on `127.0.0.1:8100`. Confirm:

```bash
curl http://127.0.0.1:8100/health
```

3. Point it at the API you are actually running. For the local demo the defaults are enough when the API is on `http://127.0.0.1:5080`. Otherwise set:

| Variable | Purpose |
| --- | --- |
| `AGENT_HOSPITAL_API_BASE_URL` | API origin for workflow persistence, no `/api` suffix, for example `http://127.0.0.1:5000`. |
| `AGENT_BACKEND_BASE_URL` | Same origin. Scheduling tools call this host, not `AGENT_HOSPITAL_API_BASE_URL`. |
| `AGENT_INTERNAL_SERVICE_KEY` | Same value as the API's `InternalServiceKey`. |
| `AGENT_SHARED_SECRET` | Same value as the API's `AgentService:SharedSecret` (`X-Internal-Secret`). |
| `AGENT_OLLAMA_BASE_URL` | Default `http://127.0.0.1:11434`. |

4. Confirm the API can reach the agent: `AgentService:BaseUrl` in the API must be `http://127.0.0.1:8100` when both processes are on the same machine.

## Fallback: cloud API, agent on a laptop

Only for a short demo window, and only if the laptop and the people calling it can reach the tunnel.

1. Start Ollama and `python run.py` as above.
2. In another terminal: `ngrok http 8100`
3. On the Render service set `AgentService__BaseUrl` to the ngrok `https` origin (no path) and `AgentService__SharedSecret` to the same value as `AGENT_SHARED_SECRET`.
4. On the laptop set `AGENT_HOSPITAL_API_BASE_URL` and `AGENT_BACKEND_BASE_URL` to the Render origin (no `/api` suffix) and `AGENT_INTERNAL_SERVICE_KEY` to the Render `InternalServiceKey`.
5. Stop ngrok when the demo ends. Do not leave the agent on a public URL.
