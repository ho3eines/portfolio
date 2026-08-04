---
name: portfolio-stack
description: |
  Portfolio project conventions. TRIGGER on ANY mention of: portfolio, resume, Blazor, .NET, SQL Server, Dapper, dark theme, bento grid, landing page, admin panel, UI, backend, API, database, add feature, fix bug, modify, change, build, create, check, review, mahbod.
  ALWAYS read .arena/handoff.md FIRST before writing any code. That file is the SINGLE SOURCE OF TRUTH.
  After completing any task, update handoff.md if files, patterns, or endpoints changed.
---

# Portfolio Stack — Complete Conventions

## 1. ARCHITECTURE (3-Tier)
```
Portfolio.Web (Blazor WASM) → HttpClient → Portfolio.Api (ASP.NET Core) → Dapper → SQL Server
                                              ↑
                                        Portfolio.Data (Shared POCOs + SqlConnectionFactory)
```

## 2. BACKEND (MANDATORY)

### Data Access
- **Dapper only** — NEVER Entity Framework. Use `SqlConnectionFactory`.
- `using var c = _db.Create();` pattern in every endpoint.
- **Parameterized SQL** — never string concatenation.
- `SqlConnectionFactory` registered as **Singleton** in Program.cs.

### API Responses
- **ALWAYS** wrap in `ApiResponse<T>`: `{ success, data, message }`.
- `ApiResponse<T>.Ok(data)` for success, `.Fail(msg)` for errors.
- AdminService on Blazor side uses `GetAsync<T>()` / `GetListAsync<T>()` to unwrap.

### Error Handling
- **Every controller method**: `try/catch` → `_err.LogAsync(ex, "Source.Method")` → return 500.
- `ErrorHandlingMiddleware` catches unhandled exceptions → logs to ErrorLogs table.
- `ErrorLogService` uses `SqlConnectionFactory` (not DbContext).
- 3-layer: Toast (user-friendly) → Middleware (server log) → Admin Panel (full detail).

### Auth
- JWT with roles: Admin, Editor, Viewer.
- `TokenService` generates HS256 tokens + refresh tokens.
- Password: SHA256(password + "PortfolioSalt2026!").
- Blazor: token in LocalStorage, `AuthService` + `ApiAuthStateProvider`.

### Resource Auto-Start
- `ResourceAutoStartService : IHostedService` — runs on API startup.
- Scans `wwwroot/resources/*.sql` alphabetically.
- Skips files where `Resources.IsSuccess = 1`.
- Executes missing/failed files via Dapper.
- On failure: `Resources.ErrorMessage` = full error detail.

### ErrorLogs Table
- Fields: Level, Source, Message, StackTrace, RequestPath, Method, StatusCode, UserId, UserAgent, IpAddress, IsResolved, ResolvedAt, ResolvedBy.
- `/api/admin/errors` — filter by level, unresolved, paginated.
- Admin can resolve, resolve-all, delete, clear.

## 3. FRONTEND (MANDATORY)

### Blazor WASM Pages (13)
- `Index.razor` — Dynamic Bento Grid landing (all content from API).
- `Auth/Login.razor`, `Auth/Register.razor`.
- `Admin/Dashboard, Profile, Projects, Skills, Experiences, Testimonials, Messages, Resources, ErrorLogs`.

### Services (5)
- `AuthService`, `ApiAuthStateProvider`, `PortfolioService`, `AdminService`, `TranslationService`.

### Shared Components (4)
- `MainLayout.razor` — dual layout: public Bento Grid / admin sidebar.
- `AdminNavLink.razor`, `LangSwitcher.razor`, `LanguageGate.razor`.

### Rendering Rules
- Every page: `@if (!loaded) { loading... } else { content }`.
- Every page using `T`: `@implements IDisposable` + `OnChange += Refresh`.
- Every form: `DataAnnotationsValidator` + `ValidationSummary`.

## 4. BILINGUAL — T Service (MANDATORY FOR ALL TEXT)

### TranslationService
- Scoped service. `T["nav.work"]` returns translated string.
- `T.OnChange` event — fire-and-forget pattern, components re-render.
- `T.SetLangAsync("fa"/"en")`, `T.ToggleAsync()`, `T.Lang`, `T.IsRtl`, `T.Direction`, `T.FontFamily`.
- `T.LanguageSelected` — false until user picks language.
- Loads `wwwroot/lang/{en,fa}.json` via HttpClient.
- Persists via `Blazored.LocalStorage`.
- Initialized in `Program.cs` before `host.RunAsync()`.

### Required Pattern (EVERY page that shows text)
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

### Adding Text
1. Add key to `wwwroot/lang/en.json` (English value).
2. Add same key to `wwwroot/lang/fa.json` (Persian value).
3. Use `@T["parent.child"]` in any .razor file.
4. JSON structure: nested objects → dot notation (`T["nav.work"]`).

### RTL + Font
- `MainLayout.razor` applies `T.Direction` and `T.FontFamily` as inline styles.
- Vazirmatn CDN for Persian, Inter for English.
- `html.rtl` class triggers 29 CSS RTL overrides.

### Language Switcher
- `Shared/LangSwitcher.razor` — standalone button (فا / EN).
- Can be placed anywhere. Currently in hero header and admin sidebar.
- Toggles via `T.ToggleAsync()`.

## 5. LANGUAGE GATE (First Visit)

### Flow
```
App.razor
  └─ <LanguageGate>
       ├─ if T.LanguageSelected → @ChildContent (Router + full site)
       └─ else → premium selection screen (🇬🇧 English / 🇮🇷 فارسی)
```

- **NO redirect, NO reload** — `LanguageGate` is a `ChildContent` wrapper.
- First visit: `T.InitAsync()` finds no `lang` in localStorage → `LanguageSelected = false` → Gate shown.
- User selects → `T.SetLangAsync()` → `LanguageSelected = true` → Gate conditionally hides → Router renders.
- Return visits: `T.LanguageSelected = true` immediately → straight to site.
- Static fallback: `wwwroot/index.html` checks localStorage → redirects to `/lang/gate.html` if no lang.
- `gate.html` is a standalone HTML page matching the Blazor gate design.

## 6. DESIGN SYSTEM

### Colors
```
--bg:#0a0a0a --surface:#141414 --card:#1a1a1a --card-alt:#202020
--text:#ffffff --text-secondary:#9a9a9a --accent:#d4a04c(gold)
--border:rgba(255,255,255,0.06) --border-strong:rgba(255,255,255,0.10)
```

### Fonts
- Body: Inter (300-900). Display: Playfair Display italic (gold tagline).
- Persian: Vazirmatn CDN (auto-loaded by TranslationService).

### Layout
- Bento Grid 5-box. Desktop: 1.35fr/0.65fr. Mobile: single column.
- Breakpoints: 481, 769, 1025.

### Components
- `.btn-solid` (white bg), `.btn-outline` (border), `.btn-pill` (rounded).
- `.project-card` with `rotate(var(--card-rotate))` + 3D tilt.
- `.skill-fill` with `scaleX()` GPU animation.
- `.glass-panel` with backdrop-blur + tilt (desktop only).

## 7. ANIMATIONS (17 modules)

1-Preloader, 2-Scroll-Spy, 3-Back-to-Top, 4-Box-Entrance, 5-Section-Dividers,
6-Text-Reveal, 7-Stagger, 8-Parallax(desktop), 9-Pin/Scrub(desktop), 10-Counter,
11-Cursor-Glow(desktop), 12-Magnetic-CTA, 13-Card-Tilt, 14-Glassmorphism,
15-Skill-Bars, 16-Carousel, 17-Toast

### JS Rules
- **Single master RAF loop** for all scroll animations.
- GPU transforms ONLY: `scaleX()`, `translateY()`, `rotateX/Y()`.
- NEVER: `width`, `height`, `top`, `left` for animation.
- ALL scroll listeners: `{ passive: true }`.
- `IntersectionObserver` for reveal/stagger/counter.
- `translateZ(0)` + `backface-visibility:hidden` on all animated elements.
- `prefers-reduced-motion` respected everywhere.
- `touch-action: manipulation` on all interactive elements.
- Mobile: no parallax/pin/glow/glass (lighter animations).
- Toast: `window.PortfolioToast.error/success/warning/info(msg)`.

## 8. SEO (per page)
- Dynamic `<title>` from Profile API.
- Meta description, keywords, robots, canonical from Profile.
- Open Graph (6 tags) + Twitter Card (4 tags) + JSON-LD Person schema.
- `robots.txt` at root, `sitemap.xml` dynamic from API.
- Semantic HTML5, alt text, heading hierarchy.

## 9. PWA
- `manifest.json` (icons 192+512, display:standalone, shortcuts).
- `service-worker.js` (cache-first, pre-cache, stale-while-revalidate).
- Apple/Win meta tags. Icons: gold geometric on black.

## 10. ZERO-RELOAD RULE ⛔

- **NEVER** use `Nav.Refresh()` or `forceReload: true`.
- **NEVER** use `window.location.href` in Blazor code.
- ✅ `Nav.NavigateTo(url)` is SPA routing — allowed (no page reload).
- ✅ LanguageGate uses **conditional rendering** (`@if T.LanguageSelected`), NOT redirect.
- ✅ Language switch uses `T.OnChange` → `StateHasChanged()`, NOT reload.
- ✅ Login → Admin, Register → Login, Logout → Home: all SPA navigation.

## 11. PRE-COMMIT CHECKLIST

- [ ] Dapper used (not EF Core). Parameterized SQL.
- [ ] `ApiResponse<T>` wrapping on API responses.
- [ ] `try/catch` + `_err.LogAsync()` on controller methods.
- [ ] Dark theme colors match palette.
- [ ] Bento Grid 5-box layout.
- [ ] Responsive (4 breakpoints).
- [ ] Single RAF + GPU transforms for animations.
- [ ] Text uses `@T["key"]` pattern (never hardcoded).
- [ ] Component subscribes `T.OnChange` + implements `IDisposable`.
- [ ] No `Nav.Refresh()`, no `window.location.href` in Blazor.
- [ ] Translation keys exist in both `en.json` and `fa.json`.
- [ ] SEO meta tags present. PWA files present.
- [ ] SQL scripts in `wwwroot/resources/`.
- [ ] Resource Auto-Start skips already-successful files.
