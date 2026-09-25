# Backend deployment (Render + Neon)

Free tier only. The API is a Docker web service on [Render](https://render.com). Postgres is [Neon](https://neon.tech) (free). The agent service is not part of this cloud deploy; see [deployment-agent-service.md](deployment-agent-service.md).

Nothing is deployed yet. Replace the placeholder host below after the first Render deploy.

## What gets built

`render.yaml` at the repo root points Render at `backend/Dockerfile` (multi-stage .NET 8 SDK build, ASP.NET runtime image). The container listens on `$PORT` (8080 locally). Render's health check path is `/api/health`, which is anonymous and returns 200.

On startup the API applies pending EF migrations and seeds accounts when the user table is empty.

## Environment variables

Set these on the Render service. Do not commit real values.

| Variable | Purpose |
| --- | --- |
| `ConnectionStrings__DefaultConnection` | Neon connection string. See below. |
| `Jwt__Secret` | HMAC signing key, at least 32 characters. Overrides the dev key baked into `appsettings.json`. |
| `Jwt__Issuer` | `smart-ayurveda-hospital` |
| `Jwt__Audience` | `smart-ayurveda-staff` |
| `InternalServiceKey` | Shared secret for `X-Internal-Service-Key`. The agent process must send the same value. |
| `AllowedOrigins` | Comma-separated browser origins. Include the deployed staff portal, for example `https://smart-ayurveda-staff.vercel.app`. No trailing slash. |

`ASPNETCORE_ENVIRONMENT` is `Production` in `render.yaml`.

Optional, only if a demo must call the agent through a tunnel:

| Variable | Purpose |
| --- | --- |
| `AgentService__BaseUrl` | Public base URL of the agent, such as the ngrok HTTPS URL, with no path suffix. |
| `AgentService__SharedSecret` | Must match `AGENT_SHARED_SECRET` on the agent. |

## PostgreSQL (Neon free tier)

Create a Neon project and copy the **pooled** connection string. Npgsql form:

```text
Host=<endpoint>.neon.tech;Port=5432;Database=neondb;Username=<user>;Password=<password>;SSL Mode=Require;Trust Server Certificate=true
```

Put that value in `ConnectionStrings__DefaultConnection`.

Apply migrations from a machine that has the .NET 8 SDK. From `backend/`:

```bash
# PowerShell
$env:ConnectionStrings__DefaultConnection = "<neon connection string>"
dotnet ef database update --project src/Hospital.Infrastructure --startup-project src/Hospital.Api
```

```bash
# bash
export ConnectionStrings__DefaultConnection="<neon connection string>"
dotnet ef database update --project src/Hospital.Infrastructure --startup-project src/Hospital.Api
```

A CI step uses the same command with that variable stored as a secret. The API also runs pending migrations on boot, so a successful container start against an empty Neon database creates the schema. Prefer the `dotnet ef` command when you want the schema in place before the first boot.

## After deploy

- Health: `https://<render-host>/api/health`
- Swagger: `https://<render-host>/swagger`
- Set `AllowedOrigins` to the Vercel origin, then redeploy the API so CORS picks it up.
