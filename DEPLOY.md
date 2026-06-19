# Deploy falas: Render + Neon (Version A)

## Para fillimit

1. Krijoni llogari falas në [Neon](https://neon.tech) dhe [Render](https://render.com)
2. Ngarkoni projektin në GitHub (GitHub Desktop ose `git push`)

---

## Hapi 1 — Neon (PostgreSQL)

1. Hyni në [console.neon.tech](https://console.neon.tech)
2. **New Project** → emri: `chess-camp`
3. Kopjoni **Connection string** (formati `postgresql://...`)
4. Ruajeni — do t'ju duhet për Render

---

## Hapi 2 — GitHub

```powershell
cd C:\Users\Marin Collaku\source\repos\ChessCampRegistration
git init
git add .
git commit -m "Prepare for Render + Neon deployment"
```

Krijoni repo në GitHub dhe bëni push:

```powershell
git remote add origin https://github.com/<username>/ChessCampRegistration.git
git branch -M main
git push -u origin main
```

---

## Hapi 3 — Render (API + frontend)

1. Hyni në [dashboard.render.com](https://dashboard.render.com)
2. **New** → **Blueprint**
3. Lidheni repo-n GitHub `ChessCampRegistration`
4. Render lexon `render.yaml` automatikisht

### Variablat e mjedisit (API: `chess-camp-api`)

| Key | Vlera |
|-----|-------|
| `DATABASE_URL` | Connection string nga Neon |
| `Admin__ApiKey` | Çelësi i fortë për admin (p.sh. `kampi-shahut-2026-secret`) |
| `Email__SmtpUser` | `marincollaku@gmail.com` |
| `Email__SmtpPassword` | App password Gmail |
| `Email__FromAddress` | `marincollaku@gmail.com` |
| `CORS_ORIGIN` | URL e frontend-it (vendoset pas Hapi 4) |

### Variablat e mjedisit (Web: `chess-camp-web`)

| Key | Vlera |
|-----|-------|
| `VITE_API_URL` | URL e API-së, p.sh. `https://chess-camp-api.onrender.com` |

5. Deploy — API dhe frontend nisen automatikisht

---

## Hapi 4 — Lidhja e CORS

Pas deploy të frontend-it, merrni URL-në e web-it (p.sh. `https://chess-camp-web.onrender.com`).

Në Render → **chess-camp-api** → **Environment** → vendosni:

```
CORS_ORIGIN=https://chess-camp-web.onrender.com
```

Ruani dhe prisni ri-deploy.

---

## Hapi 5 — Test

- **Regjistrimi:** `https://chess-camp-web.onrender.com`
- **Admin:** `https://chess-camp-web.onrender.com/admin`
- **Çelësi admin:** vlera që vendosët te `Admin__ApiKey`

---

## Kufizimet e planit falas

- **Render API** fle pas ~15 min pa aktivitet (ngarkim i parë i ngadaltë)
- **Neon** ka limit ruajtjeje/trafiku
- **Gmail SMTP** funksionon normalisht

---

## Zhvillim lokal (pas migrimit në PostgreSQL)

Vendosni connection string në `appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "postgresql://user:pass@host/db?sslmode=require"
  },
  "Email": {
    "SmtpPassword": "your-app-password"
  }
}
```

Pastaj:

```powershell
cd ChessCampRegistration.Api
dotnet run

cd ..\client
npm run dev
```
