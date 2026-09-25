# Staff portal deployment (Vercel)

Free Hobby tier. The project root on Vercel is `web-staff/`. `web-staff/vercel.json` builds with Vite and rewrites client-side routes to `index.html`.

## Environment

In the Vercel project settings:

```text
VITE_API_BASE_URL=https://<render-host>/api
```

The value must include the `/api` prefix and must not have a trailing slash. Vite reads it at build time, so change it and redeploy when the API host changes.

## CORS

Add the deployed site origin to the API variable `AllowedOrigins` (see [deployment-backend.md](deployment-backend.md)). Example:

```text
https://<project>.vercel.app
```

Origin only: scheme and host, no path, no trailing slash. Redeploy the API after changing it.
