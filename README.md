# Chess Camp Registration System

Full-stack registration system for a kids chess camp with public signup, parent confirmation emails, and an admin panel.

## Stack

- **Frontend:** React + TypeScript (Vite)
- **Backend:** ASP.NET Core (.NET 10) Web API
- **Database:** SQL Server on `DESKTOP-U0DKNR1` (Windows Authentication)

## Project structure

```
ChessCampRegistration/
├── ChessCampRegistration.Api/   # .NET 10 API
└── client/                      # React UI
```

## Database

Database `ChessCampRegistration` is created on `DESKTOP-U0DKNR1` using Windows Authentication.

Connection string (in `appsettings.json`):

```
Server=DESKTOP-U0DKNR1;Database=ChessCampRegistration;Trusted_Connection=True;TrustServerCertificate=True;
```

EF Core migrations run automatically when the API starts.

## Run locally

### 1. API

```powershell
cd ChessCampRegistration.Api
dotnet run
```

API runs at `http://localhost:5075`.

### 2. React client

```powershell
cd client
npm install
npm run dev
```

UI runs at `http://localhost:5173`.

## Configuration

Edit `ChessCampRegistration.Api/appsettings.json`:

| Setting | Purpose |
|---------|---------|
| `Admin:ApiKey` | Key required for admin endpoints (sent as `X-Admin-Key` header) |
| `Email:SmtpHost` | SMTP server for confirmation emails |
| `Email:SmtpPort` | SMTP port (default 587) |
| `Email:SmtpUser` / `Email:SmtpPassword` | SMTP credentials |
| `Email:FromAddress` / `Email:FromName` | Sender details |

If email is not configured, registrations still save; the API logs a warning and skips sending.

Default admin key: `change-me-to-a-secure-key`

## API endpoints

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| POST | `/api/registrations` | Public | Submit registration + send confirmation email |
| GET | `/api/admin/registrations` | Admin key | List all registrations |
| GET | `/api/admin/registrations/{id}` | Admin key | Get one registration |
| POST | `/api/admin/registrations` | Admin key | Manual create (no email) |
| PUT | `/api/admin/registrations/{id}` | Admin key | Update registration |

## UI pages

- `/` — Public registration form
- `/admin` — Admin panel (list, add, edit)

## Development phases

### Phase 1 — Foundation (done)
- Solution scaffold, SQL Server database, EF Core model, CRUD API

### Phase 2 — Public registration (done)
- React registration form connected to API

### Phase 3 — Email notifications (done)
- MailKit SMTP confirmation email on public registration

### Phase 4 — Admin panel (done)
- List, manual add, edit with API key protection

### Phase 5 — Optional next steps
- Export registrations to Excel/CSV
- Search and filter in admin table
- User-friendly admin login (instead of raw API key)
- Deploy to IIS or Azure
