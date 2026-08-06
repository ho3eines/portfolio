# PROJECT HANDOFF — Complete Knowledge Base
> **Read this FIRST before any code change. Update it AFTER every change.**
> This is the SINGLE SOURCE OF TRUTH — replaces reading all 60+ source files.
> Last updated: 2026-08-06 (responsive bilingual redesign, resilient language loading, and RTL admin layout repair)

## 🆕 LATEST UPDATE — Static dictionaries load from the app origin, not the API (2026-08-06)

- **FIXED `GET /lang/fa.json → 404`:** `TranslationService` previously fetched `wwwroot/lang/{code}.json` through the shared `HttpClient`, whose `BaseAddress` is the **API** base URL. When `ApiBaseUrl` is configured as a separate API host (e.g. `https://localhost:49325`), `lang/fa.json` was resolved against the API server — which has no `/lang/` files — returning 404 and blocking the language gate. `TranslationService` now gets its **own** `HttpClient` rooted at `builder.HostEnvironment.BaseAddress` (the Blazor app origin). `HelpPanel` had the identical latent bug for `lang/help/{lang}.json` and now resolves an absolute URL via `NavigationManager.BaseUri`.
- **Language loading is transactional:** `TranslationService` now validates supported languages, loads and validates a dictionary before committing the UI change, caches both dictionaries, times out safely, and preserves the existing UI on a failed fetch. Invalid/stale storage is cleared and returns to the language gate instead of rendering raw translation keys.
- **Document direction is synchronized:** `LanguageGate` calls `portfolio.applyDocumentLanguage()` after render, while `index.html` applies the persisted `lang`/`dir` before the first paint. This removes the LTR flash for returning Persian visitors. The PWA pre-caches both translation JSON files and uses a new cache version.
- **RTL/LTR admin shell is logical-property based:** `.admin-side` uses `inset-inline-start`, `.admin-main` uses `margin-inline-start`, and the mobile drawer reverses its off-canvas transform in RTL. Forms, select arrows, tables, action rows, the help drawer, and mobile navigation now follow the active direction.
- **All current Blazor interface chrome is localized:** EN/FA dictionaries now cover the public landing page, auth pages, dashboard, all CRUD/admin surfaces, errors, resource runner, help panel, empty/loading states, and access-denied view. The public Persian mode also localizes bundled hero, project-card, and sample-testimonial copy.
- **Visual system:** `wwwroot/css/styles.css` was rebuilt as a midnight studio design (Manrope + DM Serif Display / Vazirmatn), with responsive public bento cards, mobile menu, polished auth pages, and usable admin cards/forms/tables down to narrow phones. New admin item editors also fix the previous inability to add a brand-new skill.


---

## ⚡ QUICK START

```
Stack:     Blazor WASM → HttpClient → ASP.NET Core API → Dapper → SQL Server
Auth:      JWT (Bearer token in LocalStorage). Roles: Admin, Editor, Viewer
DB:        Database-First, T-SQL in wwwroot/resources/, Dapper for all access
UI:        Premium dark #0b0d10 · champagne accent #c9a96a · minimal editorial Bento
           (2026-08-05 redesign: hairline rules, serif display, Admin/Login pill in header+footer)
Bilingual: TranslationService — T["key"] pattern, no reload, JSON in wwwroot/lang/
Run:       cd src/Portfolio.Api && dotnet run   |   cd src/Portfolio.Web && dotnet run
Login:     admin / Admin@123
Preview:   http://localhost:5173 (npx serve wwwroot)
          Admin accessible from the home page → /login.html (themable, posts to /api/auth/login)
          → on success redirects to /admin.html (dashboard, populated from config.js).
```

---

## 📂 FILE MAP

```
portfolio/
├── Portfolio.sln                         ← 3 projects: Web, Api, Data
├── README.md                             ← User-facing docs
│
├── wwwroot/                              ← ★ STATIC PREVIEW (live on port 5173)
│   ├── index.html                        ← Landing page (checks localStorage for lang)
│   ├── css/styles.css                    ← ALL styles (responsive, RTL, animations, gate)
│   ├── js/
│   │   ├── animations.js                 ← 17 animation modules + toast + scroll-spy
│   │   ├── lang.js                       ← i18n engine for static HTML (170 lines)
│   │   ├── config.js                     ← Static-only site data
│   │   └── splash.js                     ← NOT USED (legacy)
│   ├── images/icon-192.png, icon-512.png ← PWA icons
│   ├── lang/
│   │   ├── en.json                       ← English translations (85 keys, 9 sections)
│   │   ├── fa.json                       ← Persian translations (85 keys, 9 sections)
│   │   └── gate.html                     ← Static language selection gate
│   ├── manifest.json, service-worker.js, robots.txt, sitemap.xml
│   └── resources/                        ← ★ ALL T-SQL SCRIPTS
│       ├── 01-create-database.sql        ← 12 tables + indexes
│       ├── 02-seed-data.sql              ← Admin user + sample data
│       ├── 03-resource-runner.sql        ← SP: usp_ExecuteResource
│       └── 04-error-log.sql              ← ErrorLogs table
│
├── src/
│   ├── Portfolio.Data/                   ← ★ SHARED LIBRARY
│   │   ├── Portfolio.Data.csproj         ← Dapper + Microsoft.Data.SqlClient
│   │   ├── SqlConnectionFactory.cs       ← IDbConnection factory (Singleton)
│   │   ├── PortfolioDbContext.cs         ← EMPTY STUB (backward compat)
│   │   ├── Models/Entities.cs            ← 12 POCO classes (NO EF annotations)
│   │   └── DTOs/DTOs.cs                  ← Request/Response models
│   │
│   ├── Portfolio.Api/                    ← ★ BACKEND (6 Controllers, 3 Services, 1 Middleware)
│   │   ├── Program.cs                    ← DI: SqlConnectionFactory, JWT, TranslationService, AutoStart
│   │   ├── Controllers/
│   │   │   ├── AuthController.cs         ← POST login/register/refresh, GET me
│   │   │   ├── PortfolioController.cs    ← Public: profile, projects, skills, exp, testimonials, contact
│   │   │   ├── AdminController.cs        ← Full CRUD for all content tables + error count
│   │   │   ├── ResourcesController.cs    ← Execute SQL files + history with ErrorMessage
│   │   │   ├── SeoController.cs          ← robots.txt, sitemap.xml, seo-info
│   │   │   └── ErrorLogController.cs     ← List/resolve/delete/clear errors
│   │   ├── Services/
│   │   │   ├── TokenService.cs           ← JWT generation (HS256)
│   │   │   ├── ErrorLogService.cs        ← Log errors/warnings/info to DB
│   │   │   └── ResourceAutoStartService.cs ← ★ Auto-execute SQL on API startup
│   │   └── Middleware/
│   │       └── ErrorHandlingMiddleware.cs ← Catch unhandled → log → return 500 JSON
│   │
│   └── Portfolio.Web/                    ← ★ FRONTEND (13 Pages, 5 Services, 4 Shared)
│       ├── Program.cs                    ← DI + TranslationService.InitAsync() before RunAsync
│       ├── App.razor                     ← <LanguageGate> wrapper around <Router>
│       ├── Pages/
│       │   ├── Index.razor               ← ★ Landing: dynamic Bento Grid, SEO, JSON-LD, 48×T["..."]
│       │   ├── Auth/Login.razor          ← JWT login (T service)
│       │   ├── Auth/Register.razor       ← Registration (T service)
│       │   └── Admin/ (9 pages)          ← Dashboard, Profile, Projects, Skills, Exp, Testimonials,
│       │                                    Messages, Resources, ErrorLogs
│       ├── Services/
│       │   ├── TranslationService.cs     ← ★ T["key"] indexer, OnChange, LanguageSelected, RTL
│       │   ├── AuthService.cs            ← Login/Register/Refresh/Logout
│       │   ├── ApiAuthStateProvider.cs   ← Custom JWT auth state
│       │   ├── PortfolioService.cs       ← Public API calls
│       │   └── AdminService.cs           ← Admin CRUD (unwrap ApiResponse<T>)
│       ├── Shared/
│       │   ├── MainLayout.razor          ← Dual layout + T.Direction/T.FontFamily + LangSwitcher
│       │   ├── LanguageGate.razor        ← ★ First-visit language selection (ChildContent wrapper)
│       │   ├── LangSwitcher.razor        ← EN/فا toggle button
│       │   └── AdminNavLink.razor        ← Active-state sidebar link
│       └── wwwroot/                      ← Mirrors root wwwroot/ (lang/, css/, js/, images/)
```

---

## 🔌 KEY DATA FLOWS

### Language Gate (First Visit)
```
1. Program.cs → T.InitAsync() → localStorage.getItem('lang') → null
2. T.LanguageSelected = false
3. App.razor → <LanguageGate> → @if (!T.LanguageSelected) → show gate
4. User clicks 🇮🇷 → T.SetLangAsync("fa") → localStorage.setItem + load JSON
5. T.LanguageSelected = true → Gate re-renders → @ChildContent → Router → Index.razor
6. Index.razor renders with all @T["..."] returning Persian text
7. NO reload — pure conditional rendering + StateHasChanged
```

### API routing (browser-safe)
```
The WASM client defaults to builder.HostEnvironment.BaseAddress.
Therefore requests are relative to the page origin: /api/portfolio/profile,
not https://localhost:49325. The hosting reverse proxy must forward /api to
Portfolio.Api. ApiBaseUrl / Api:BaseUrl is an opt-in only for a separately
hosted, browser-reachable API URL.
```

### Language Switch (Mid-Session)
```
1. User clicks فا/EN button → LangSwitcher → T.ToggleAsync()
2. T.SetLangAsync("fa") → localStorage + load JSON → OnChange?.Invoke()
3. MainLayout: Refresh() → StateHasChanged → direction=rtl + Vazirmatn font
4. Index.razor: Refresh() → StateHasChanged → all @T["..."] now Persian
5. All other pages subscribed to OnChange also refresh
6. NO reload — all via event + StateHasChanged
```

### Resource Auto-Start
```
1. API starts → ResourceAutoStartService.StartAsync()
2. Scan wwwroot/resources/*.sql alphabetically
3. For each file: SELECT FROM Resources WHERE FileName=@Name
4. If IsSuccess=1 → skip. Else → Dapper.ExecuteAsync(sql)
5. Success → INSERT/UPDATE Resources SET IsSuccess=1, ErrorMessage=NULL
6. Failure → INSERT/UPDATE Resources SET IsSuccess=0, ErrorMessage=full error
7. Log summary
```

### Error Handling
```
Controller exception → _err.LogAsync(ex, "Source.Method")
  → Dapper INSERT INTO ErrorLogs (Level, Source, Message, StackTrace, RequestPath...)
  → Return 500 + ApiResponse.Fail("Friendly message")
  → Frontend catches → PortfolioToast.error("Friendly message")

Unhandled → ErrorHandlingMiddleware → _err.LogAsync → 500 JSON

Admin → Dashboard shows ⚠ N unresolved → /admin/errors → full details → Resolve
```

---

## 🗄 DATABASE (12 tables)

```
Roles, Users, Resources, Profile, Projects, ProjectImages,
Skills, Experiences, Testimonials, ContactMessages, SiteSettings, ErrorLogs
```
- **Resources**: FileName, FileContent, ExecutedAt, IsSuccess, ErrorMessage ★
- **ErrorLogs**: Level, Source, Message, StackTrace, RequestPath, Method, StatusCode,
  UserId, UserAgent, IpAddress, IsResolved, ResolvedAt, ResolvedBy ★

---

## 🌐 BILINGUAL SYSTEM

### TranslationService (T Pattern)
- `T["nav.work"]` → translated string
- `T.OnChange` → event, components subscribe + StateHasChanged
- `T.LanguageSelected` → false until user picks language
- `T.SetLangAsync("fa"/"en")`, `T.ToggleAsync()`
- `T.Lang`, `T.IsRtl`, `T.Direction`, `T.FontFamily`
- Loads `wwwroot/lang/{code}.json` (85 keys, 9 sections)
- Persists via Blazored.LocalStorage

### Required Pattern for Every Page
```razor
@inject TranslationService T
@implements IDisposable
@T["section.key"]
@code {
    protected override void OnInitialized() => T.OnChange += Refresh;
    private void Refresh() => InvokeAsync(StateHasChanged);
    public void Dispose() => T.OnChange -= Refresh;
}
```

### JSON Files (wwwroot/lang/)
- `en.json` — 9 sections: site, nav, hero, work, about, skills, contact, admin, toasts
- `fa.json` — same structure, Persian values
- Flat keys via dot notation: `T["contact.form_name_placeholder"]`

### RTL + Fonts
- `MainLayout.razor`: `style="direction:@T.Direction;font-family:@T.FontFamily"`
- `Index.razor`: a visible `LangSwitcher` is present in the public hero header as well as admin.
- Vazirmatn for Persian (CDN), Inter for English
- `html.rtl` → 29 CSS RTL overrides in styles.css

### LanguageGate
- `App.razor` wraps `<Router>` in `<LanguageGate>`
- First visit → shows selection screen (🇬🇧 / 🇮🇷) with premium design
- After selection → conditional render → site appears
- Static: `index.html` → checks localStorage → `/lang/gate.html` if unset

### ⛔ ZERO-RELOAD
- No `Nav.Refresh()`, no `forceReload`, no `window.location.href` in Blazor
- `Nav.NavigateTo(url)` for SPA routing ✓
- LanguageGate: conditional rendering ✓
- Language switch: `OnChange` → `StateHasChanged` ✓

---

## ⚠️ GOTCHAS

1. Dapper requires exact column name match — POCO properties = SQL columns
2. ApiResponse wrapping — AdminService unwraps via GetAsync<T>/GetListAsync<T>
3. Progress bar uses `scaleX()` not `width` — JS must set `transform`
4. Skill bars use `--fill` as number (95 not 95%) → CSS `calc(var(--fill)/100)`
5. Mobile menu locks body scroll — restore on close
6. Resource Auto-Start alphabetical — name files 01-, 02-, 03-
7. `blazor.webassembly.js` loads last — SW registration after
8. cursor-glow hidden on mobile — CSS `display:none` on touch devices
9. ErrorLogService uses SqlConnectionFactory, not DbContext
10. `T.InitAsync()` must be called in Program.cs BEFORE `host.RunAsync()`
11. Every component using T must `@implements IDisposable` + unsubscribe OnChange
12. `languageSelected` guard prevents gate flash for returning users
13. `LangSwitcher` must be inside a component subscribed to `T.OnChange`
14. Static `wwwroot/lang/*.json` must always be fetched from the **app origin** (`builder.HostEnvironment.BaseAddress` / `NavigationManager.BaseUri`), never through the API `HttpClient`. When `ApiBaseUrl` is a separate host, the API has no `/lang/` files → 404.

---

## 📡 API REFERENCE

### Public: GET /api/portfolio/{profile, projects, skills, experiences, testimonials, settings, seo}
### Public: POST /api/portfolio/contact
### Auth: POST /api/auth/{login, register, refresh} — GET /api/auth/me
### Admin: CRUD /api/admin/{profile, projects, skills, experiences, testimonials, messages, settings}
### Resources: GET/POST /api/resources — POST /api/resources/execute/{file}
### Errors: GET /api/admin/errors — PUT /api/admin/errors/{id}/resolve — DELETE clear
### SEO: GET /robots.txt — GET /sitemap.xml — GET /api/seo-info

---


---

## 📖 IN-APP HELP SYSTEM (Beginner-Friendly Guide)

### How it works
- **Access**: Click the gold "📖 Help & Guide" button in the admin sidebar
- **Context-aware**: Opens the guide for the current page (Dashboard, Profile, Projects, etc.)
- **Panel**: Slides in from the right — dark theme, responsive, close with ✕ or overlay click
- **Content**: What is this? + How to Use It (step-by-step) + Pro Tips

### Help Content Files
-  — English help (12 sections)
-  — Persian help (12 sections)

### Sections Covered


### Adding/Updating Help Content
1. Open 
2. Each section has: , ,  (array of {step, desc} or strings),  (string array)
3. Update both language files
4. Copy to 

### Component
-  — slide-in panel, loads JSON via HttpClient
-  — "Help & Guide" button in sidebar → calls 

## ✅ PRE-CHANGE CHECKLIST

- [ ] Read this handoff
- [ ] Check FILE MAP for the file to modify
- [ ] Follow T pattern for any visible text
- [ ] Subscribe/unsubscribe T.OnChange in new components
- [ ] No reload code (Nav.Refresh, window.location)
- [ ] Dapper + parameterized SQL
- [ ] ApiResponse<T> wrapping
- [ ] try/catch + _err.LogAsync
- [ ] Update this handoff after changes

---

## 📖 IN-APP HELP SYSTEM

- **Button**: "📖 Help & Guide" in admin sidebar (gold, below language switcher)
- **Component**: `Shared/HelpPanel.razor` — slides in from right, dark theme
- **Content**: `wwwroot/lang/help/{en,fa}.json` — 12 sections
- **Sections**: getting_started, dashboard, profile, projects, contracts, phases, skills, experiences, testimonials, messages, resources, errors
- Each section has: `title`, `what` (explanation), `how` (steps array), `tips` (tips array)
