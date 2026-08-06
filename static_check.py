#!/usr/bin/env python3
"""Static verification for the Portfolio solution (no .NET SDK available)."""
import json, os, re, sys, hashlib

ROOT = "/home/user/portfolio"
errors, warnings = [], []

def err(msg): errors.append(msg)
def warn(msg): warnings.append(msg)

# ── 1. Balanced braces/parens/brackets in .cs and .razor @code ───────────
def strip_strings_comments(src):
    out, i, n = [], 0, len(src)
    while i < n:
        c = src[i]
        if c == '"':
            j = i + 1
            while j < n and src[j] != '"':
                if src[j] == '\\': j += 1
                j += 1
            i = j + 1
        elif src.startswith("//", i):
            j = src.find("\n", i); i = n if j == -1 else j
        elif src.startswith("/*", i):
            j = src.find("*/", i + 2); i = n if j == -1 else j + 2
        elif src.startswith('@"', i):
            j = i + 2
            while j < n:
                if src[j] == '"' and src[j-1] == '"': j += 1
                elif src[j] == '"': break
                j += 1
            i = j + 1
        elif src.startswith("@'", i):
            j = src.find("'", i + 2); i = n if j == -1 else j + 1
        else:
            out.append(c); i += 1
    return "".join(out)

def check_balance(path, pairs):
    src = open(path, encoding="utf-8").read()
    clean = strip_strings_comments(src)
    stack = []
    for ch in clean:
        if ch in "([{": stack.append(ch)
        elif ch in ")]}":
            if not stack: return False, f"unmatched {ch}"
            op = stack.pop()
            if "([{".index(op) != ")]}".index(ch): return False, f"mismatch {op}{ch}"
    if stack: return False, f"unclosed: {''.join(stack)[:20]}"
    return True, ""

for root, _, files in os.walk(os.path.join(ROOT, "src")):
    for f in files:
        if f.endswith(".cs"):
            p = os.path.join(root, f)
            ok, why = check_balance(p, "(){}[]")
            if not ok: err(f"brace balance FAIL {p}: {why}")
        elif f.endswith(".razor"):
            p = os.path.join(root, f)
            src = open(p, encoding="utf-8").read()
            for m in re.finditer(r"@code\s*\{(.*?)\n\}", src, re.S):
                block = m.group(0)
                ok, why = check_balance(p, "(){}[]")
                if not ok: err(f"razor @code balance FAIL {p}: {why}")

# ── 2. Razor injects are registered in Program.cs ─────────────────────────
prog = open(os.path.join(ROOT, "src/Portfolio.Web/Program.cs"), encoding="utf-8").read()
injects = set()
for root, _, files in os.walk(os.path.join(ROOT, "src/Portfolio.Web")):
    for f in files:
        if f.endswith(".razor"):
            src = open(os.path.join(root, f), encoding="utf-8").read()
            injects.update(re.findall(r'@inject\s+([\w\.]+)\s+(\w+)', src))
injects |= set(re.findall(r'@inject\s+([\w\.]+)\s+(\w+)', open(os.path.join(ROOT, "src/Portfolio.Web/App.razor"), encoding="utf-8").read()))
for typ, name in sorted(injects):
    simple = typ.split(".")[-1]
    # built-in / framework types
    if simple in ("HttpClient", "IJSRuntime", "NavigationManager", "AuthenticationStateProvider"):
        continue
    if f"AddScoped<{simple}>()" not in prog:
        err(f"service not registered in Program.cs: {typ} (as {name})")

# ── 3. API URL cross-check (client side vs controllers) ──────────────────
def collect_routes():
    routes = []
    ctrl_dir = os.path.join(ROOT, "src/Portfolio.Api/Controllers")
    for f in os.listdir(ctrl_dir):
        src = open(os.path.join(ctrl_dir, f), encoding="utf-8").read()
        cls_route = re.search(r'Route\("([^"]+)"\)', src)
        base = cls_route.group(1) if cls_route else ""
        # ASP.NET [controller] token → lowercase class name without "Controller"
        if "[controller]" in base:
            cls_name = f[:-3]  # strip ".cs"
            base = base.replace("[controller]", cls_name.lower().replace("controller", ""))
        for m in re.finditer(r'\[Http(Get|Post|Put|Delete)\(?"?([^"\)]*)"?\)?\]', src):
            verb, sub = m.group(1), m.group(2).strip()
            sub = sub.replace("{", "{").replace("}", "}")
            path = (base + "/" + sub).replace("//", "/")
            path = path.rstrip("/")
            routes.append((verb.upper(), path))
    return routes

api_routes = collect_routes()
client_calls = []
for root, _, files in os.walk(os.path.join(ROOT, "src/Portfolio.Web")):
    for f in files:
        if f.endswith((".cs", ".razor")):
            p = os.path.join(root, f)
            src = open(p, encoding="utf-8").read()
            for m in re.finditer(r'"(api/[a-z0-9\-_/\$\{\}\.]+)"', src):
                client_calls.append((f, m.group(1)))

def matches(route_tpl, client):
    # split on {param} placeholders and escape the literal parts
    parts = re.split(r"\{[^}]+\}", route_tpl)
    rx = "[^/]+".join(re.escape(p) for p in parts)
    return re.fullmatch(rx, client) is not None

for f, call in sorted(set(client_calls)):
    c = call.split("?")[0].rstrip("/")
    # resolve known prefixes
    if c.startswith("api/"):
        ok = any(matches(rp, c) for _, rp in api_routes)
        if not ok:
            err(f"client call has no matching API route: {f} → {c}")

# ── 4. JSON validity + translation keys + help sections ──────────────────
lang_dir = os.path.join(ROOT, "src/Portfolio.Web/wwwroot/lang")
for lang in ("en", "fa"):
    json.load(open(os.path.join(lang_dir, f"{lang}.json"), encoding="utf-8"))
    json.load(open(os.path.join(lang_dir, f"help/{lang}.json"), encoding="utf-8"))
json.load(open(os.path.join(ROOT, "src/Portfolio.Web/wwwroot/manifest.json"), encoding="utf-8"))
for root, _, files in os.walk(os.path.join(ROOT, "src/Portfolio.Web")):
    for f in files:
        if f.endswith(".json") and "wwwroot" in root:
            json.load(open(os.path.join(root, f), encoding="utf-8"))

# T[] keys
keys = set()
for root, _, files in os.walk(os.path.join(ROOT, "src/Portfolio.Web")):
    for f in files:
        if f.endswith(".razor"):
            src = open(os.path.join(root, f), encoding="utf-8").read()
            keys.update(re.findall(r'T\["([^"]+)"\]', src))
def flat(d, p=""):
    out = {}
    for k, v in d.items():
        kk = f"{p}.{k}" if p else k
        if isinstance(v, dict): out.update(flat(v, kk))
        else: out[kk] = v
    return out
for lang in ("en", "fa"):
    d = flat(json.load(open(os.path.join(lang_dir, f"{lang}.json"), encoding="utf-8")))
    missing = [k for k in sorted(keys) if k not in d]
    if missing: err(f"{lang}.json missing keys: {missing}")

# ── 5. Referenced assets exist ───────────────────────────────────────────
www = os.path.join(ROOT, "src/Portfolio.Web/wwwroot")
refs = set()
for root, _, files in os.walk(os.path.join(ROOT, "src/Portfolio.Web")):
    for f in files:
        if f.endswith((".razor", ".html", ".js")):
            src = open(os.path.join(root, f), encoding="utf-8").read()
            refs.update(re.findall(r'(?:src|href)="(/?(?:images|css|js|lang|resources|manifest[^"]*|robots[^"]*|sitemap[^"]*)[^"]*)"', src))
for r in refs:
    r = r.lstrip("/")
    if r.startswith(("http", "_framework")): continue
    if r.startswith("lang/"):
        p = os.path.join(www, r)
    else:
        p = os.path.join(www, r)
    if not os.path.exists(p):
        # allow query strings / fragments
        p2 = p.split("?")[0].split("#")[0]
        if not os.path.exists(p2):
            err(f"missing referenced asset: {r}")

# ── 6. Dead-code leftovers ───────────────────────────────────────────────
def grep_src(name, pat):
    hits = []
    for root, _, files in os.walk(ROOT):
        if ".git" in root or ".arena" in root: continue
        for f in files:
            if f.endswith((".cs", ".razor", ".json", ".sql", ".html", ".md")):
                p = os.path.join(root, f)
                src = open(p, encoding="utf-8", errors="ignore").read()
                if re.search(pat, src):
                    hits.append(os.path.relpath(p, ROOT))
    return hits

checks = [
    ("PagedResult", r"\bPagedResult\b"),
    ("AuthService.IsLoggedIn", r"\bIsLoggedIn\b"),
    ("PortfolioService.GetSettingsAsync", r"\bGetSettingsAsync\b"),
    ("PortfolioService.GetExperiencesAsync", r"PortfolioService\s*\.\s*GetExperiencesAsync|\bexperiences\s*=\s*await\s+Portfolio\.GetExperiencesAsync"),
    ("AdminService.GetDashboardStatsAsync", r"\bGetDashboardStatsAsync\b"),
    ("AdminService.ResourceHistory class", r"\bResourceHistory\b"),
    ("login.html", r"login\.html"),
    ("old SHA256 salt", r"PortfolioSalt2026"),
    ("hardcoded JWT key", r"PortfolioSuperSecretKey"),
]
for label, pat in checks:
    hits = grep_src(label, pat)
    if hits:
        err(f"leftover '{label}': {hits}")

# ── 7. Seed PBKDF2 hash matches Python-computed value ────────────────────
seed = open(os.path.join(ROOT, "src/Portfolio.Web/wwwroot/resources/02-seed-data.sql"), encoding="utf-8").read()
m = re.search(r"PBKDF2\$(\d+)\$([0-9a-f]+)\$([0-9a-f]+)", seed)
if not m:
    err("PBKDF2 hash not found in seed")
else:
    it, salt, hsh = int(m.group(1)), bytes.fromhex(m.group(2)), m.group(3)
    expect = hashlib.pbkdf2_hmac("sha256", b"Admin@123", salt, it, 32).hex()
    if expect != hsh:
        err("seed PBKDF2 hash does not match Admin@123")
    else:
        print(f"  ✓ seed PBKDF2 verified (iterations={it})")

# ── 8. appsettings sanity ────────────────────────────────────────────────
api_cfg = json.load(open(os.path.join(ROOT, "src/Portfolio.Api/appsettings.json"), encoding="utf-8"))
cs = api_cfg["ConnectionStrings"]["PortfolioDb"]
if "User Id=sa" not in cs: err("API connection string missing sa user")
if "Password=" not in cs: err("API connection string missing password")
if "Trusted_Connection=true" in cs: err("API connection string still uses Trusted_Connection")
if "Jwt" in api_cfg and "Key" in api_cfg["Jwt"]: err("Jwt:Key must not be in committed appsettings.json")
dev_cfg = json.load(open(os.path.join(ROOT, "src/Portfolio.Api/appsettings.Development.json"), encoding="utf-8"))
if "Key" not in dev_cfg.get("Jwt", {}): err("Development Jwt:Key missing")
web_cfg = json.load(open(os.path.join(ROOT, "src/Portfolio.Web/appsettings.json"), encoding="utf-8"))
if "ApiBaseUrl" in web_cfg: err("ApiBaseUrl must not be in committed Web appsettings.json")
web_dev = json.load(open(os.path.join(ROOT, "src/Portfolio.Web/appsettings.Development.json"), encoding="utf-8"))
if web_dev.get("ApiBaseUrl") != "https://localhost:49325": warn("Web Development ApiBaseUrl differs from expected dev URL")

# ── 9. All @code field references exist (rough) ──────────────────────────
# skip — covered by manual review

print("\n── RESULTS ──")
if errors:
    print(f"✗ {len(errors)} ERROR(S):")
    for e in errors: print("  -", e)
    sys.exit(1)
print("✓ all static checks passed")
if warnings:
    print(f"({len(warnings)} warning(s):)")
    for w in warnings: print("  ~", w)
