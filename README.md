# 🚀 Mahbod Pour — Portfolio Platform

**Blazor WASM + ASP.NET Core API + Dapper + SQL Server**  
**Premium Dark Theme · Bento Grid · Bilingual (EN/FA) · Admin Panel · PWA · SEO**

---

## ⚡ Quick Start

```bash
# 1. Database (SQL Server required — Windows or Docker/Linux)
sqlcmd -S localhost -U sa -P "Portfolio@2026" -i wwwroot/resources/01-create-database.sql
sqlcmd -S localhost -U sa -P "Portfolio@2026" -i wwwroot/resources/02-seed-data.sql

# 2. API Server (auto-executes SQL on startup)
cd src/Portfolio.Api && dotnet run

# 3. Blazor Client
cd src/Portfolio.Web && dotnet run

# Development note: configure the Web dev server / reverse proxy so its
# /api path forwards to the API address printed above. Do not put
# https://localhost:49325 in client configuration: in a remote browser,
# localhost is the visitor's computer, not your server.

# (Production) publish the Blazor client and serve the API + wwwroot
# through a reverse proxy so both are same-origin:
cd src/Portfolio.Web && dotnet publish -c Release

# Default Admin: admin / Admin@123
```

### 🔐 Configuration

| Setting | Where | Default (development) |
|---|---|---|
| SQL connection | `ConnectionStrings:PortfolioDb` in `src/Portfolio.Api/appsettings.json` | `Server=localhost;User Id=sa;Password=Portfolio@2026;TrustServerCertificate=True` |
| API base URL | Reverse-proxy `/api` to `Portfolio.Api` (recommended) | Same origin — no browser request to `localhost` |
| Separate API (optional) | `ApiBaseUrl` / `Api__BaseUrl` in the Web app configuration | Must be an absolute, browser-reachable HTTPS URL |
| JWT signing key | `Jwt:Key` in `src/Portfolio.Api/appsettings.Development.json` | Dev-only key; **must be set via env var `Jwt__Key` in production** |

> ⚠️ The JWT signing key is intentionally **not** in `appsettings.json` — the API
> refuses to start without `Jwt:Key` configured (dev key comes from
> `appsettings.Development.json`, production from the `Jwt__Key` environment
> variable or user-secrets).
>
> 🔑 Passwords are hashed with **PBKDF2** (SHA-256, 100 000 iterations, per-user
> salt) — stored as `PBKDF2$iterations$saltHex$hashHex`. The seed admin uses the
> password `Admin@123`.
>
> 👀 **Roles:** registration creates a **Viewer** (read-only dashboard access).
> Admin/Editor can manage content; Admin alone can run SQL scripts and view
> error logs. Signed-in users without permission see "Access Denied" instead of
> a redirect loop.

---

## 🏗 Architecture

```
Blazor WASM → HttpClient → ASP.NET Core API → Dapper → SQL Server
     │                           │
     ├─ 13 Pages                 ├─ 6 Controllers (60+ endpoints)
     ├─ 5 Services               ├─ 3 Services
     └─ 5 Shared Components      └─ JWT Auth (Admin/Editor/Viewer)
```

---

## ✨ Features

### 🎨 Premium UI
- Dark theme (#0a0a0a) with gold accent (#d4a04c)
- Bento Grid 5-box asymmetric layout
- Fully responsive (4 breakpoints: mobile, tablet, desktop)
- 17 animation modules — Single RAF loop, GPU transforms
- Glassmorphism, 3D card tilt, magnetic buttons, cursor glow
- Fonts: Inter (body) + Playfair Display italic (tagline)

### 🌐 Bilingual (EN/FA)
- `T["key"]` pattern — TranslationService with Change events
- 85 translation keys in 9 sections per language
- RTL support for Persian — Vazirmatn font, direction switch
- Language Gate — First-visit language picker
- Zero reload — All language changes via StateHasChanged()

### 🛠 Admin Panel (11 pages)
Dashboard · Profile · Projects · Contracts · Phases · Skills · Experiences · Testimonials · Messages · Resources · Error Logs

### 📖 In-App Help System
- "Help & Guide" button in admin sidebar
- 12 context-aware sections with step-by-step guides
- Bilingual (EN + FA), loaded from JSON

### ⭐ Resource Auto-Start
- Executes wwwroot/resources/*.sql on API startup
- Skips already-successful files (tracks via Resources table)
- Logs ErrorMessage on failure

### 🛡 Error Handling (3-Layer)
1. User: Toast notifications (friendly)
2. Middleware: Catch unhandled → log to DB
3. Admin Panel: Full details, resolve, clear

### 📱 PWA + 🔍 SEO
- Installable, offline support, custom icons
- Dynamic meta tags, JSON-LD, sitemap, robots.txt

---

## 📂 Project Structure

```
portfolio/
├── wwwroot/                    ← Static preview (port 5173)
│   ├── css/styles.css          ← All styles (2200+ lines)
│   ├── js/animations.js        ← 17 animation modules
│   ├── js/lang.js              ← i18n for static HTML
│   ├── lang/{en,fa}.json       ← Translations (85 keys)
│   ├── lang/help/{en,fa}.json  ← Help system content
│   └── resources/              ← 5 T-SQL scripts
│
├── src/
│   ├── Portfolio.Data/         ← Shared Library
│   │   ├── SqlConnectionFactory.cs
│   │   ├── Models/Entities.cs  ← 14 POCOs
│   │   └── DTOs/DTOs.cs
│   │
│   ├── Portfolio.Api/          ← Backend API
│   │   ├── Controllers/ (6)
│   │   ├── Services/ (3)
│   │   └── Middleware/ (1)
│   │
│   └── Portfolio.Web/          ← Blazor WASM
│       ├── Pages/ (13)
│       ├── Services/ (5)
│       └── Shared/ (5)
│
└── .arena/
    ├── handoff.md              ← Complete project knowledge
    └── skills/portfolio-stack/ ← Conventions
```

---

## 🗄 Database (14 Tables)

Roles, Users, Resources, Profile, Projects, ProjectImages, Skills, Experiences, Testimonials, ContactMessages, SiteSettings, ErrorLogs, Contracts, ProjectPhases

---

## 🔑 Key Design Decisions

| Decision | Why |
|----------|-----|
| Dapper (not EF Core) | ~3x faster, raw SQL control |
| Database-First | SQL scripts are source of truth |
| JWT Auth | Stateless for Blazor WASM SPA |
| Bento Grid | Asymmetric = premium feel |
| Single RAF loop | 17 animations, no jank |
| T["key"] pattern | Reactive i18n, no reload |
| Language Gate | Conditional render, not redirect |
| Zero reloads | SPA routing + StateHasChanged |

---

## 📡 API (60+ Endpoints — Full CRUD for all entities)

Public, Auth, Admin CRUD, Resources, Error Logs — see `.arena/handoff.md` for full reference.

---

## 🎨 Design Tokens

```css
--bg: #0a0a0a; --surface: #141414; --card: #1a1a1a;
--text: #ffffff; --text-secondary: #9a9a9a; --accent: #d4a04c;
--border: rgba(255,255,255,0.06);
```

---

> **70+ source files · 60+ API endpoints · 14 DB tables · 17 animations · 2 languages · Full admin panel · In-app help · Zero reloads**
