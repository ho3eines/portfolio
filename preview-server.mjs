#!/usr/bin/env node
/**
 * preview-server.mjs — Zero-dependency static + mock-API server for Portfolio
 *
 * Serves `src/Portfolio.Web/wwwroot` on 0.0.0.0 so Arena previews and local
 * `npx serve` both work without `dotnet`. It prevents the classic
 *   GET https://localhost:49325/_framework/blazor.webassembly.js 404
 * by serving the checked-in stub at `wwwroot/_framework/blazor.webassembly.js`
 * (static preview) and by mocking the public `/api/portfolio/*` endpoints from
 * `js/config.js` when the real API on 49325 isn't running.
 *
 * Usage:
 *   node preview-server.mjs              # → http://localhost:5173 (serves wwwroot)
 *   PORT=49323 node preview-server.mjs  # emulate Blazor WASM dev server port
 *   node preview-server.mjs 5173         # explicit port
 *
 * For Arena previews: binds 0.0.0.0, sets permissive CORS / host handling,
 * and prints the preview-ready URLs.
 */

import http from 'node:http';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const wwwroot = path.join(__dirname, 'src', 'Portfolio.Web', 'wwwroot');
let rawPort = process.argv[2];
if (rawPort && isNaN(parseInt(rawPort, 10))) rawPort = null;
const port = parseInt(rawPort || process.env.PORT || '5173', 10);
const host = '0.0.0.0';

// ── tiny mime map ──────────────────────────────────────────────────────────
const MIME = {
    '.html': 'text/html; charset=utf-8',
    '.js': 'application/javascript; charset=utf-8',
    '.mjs': 'application/javascript; charset=utf-8',
    '.css': 'text/css; charset=utf-8',
    '.json': 'application/json; charset=utf-8',
    '.png': 'image/png',
    '.jpg': 'image/jpeg',
    '.jpeg': 'image/jpeg',
    '.webp': 'image/webp',
    '.svg': 'image/svg+xml',
    '.ico': 'image/x-icon',
    '.woff': 'font/woff',
    '.woff2': 'font/woff2',
    '.wasm': 'application/wasm',
    '.txt': 'text/plain; charset=utf-8',
    '.xml': 'text/xml; charset=utf-8',
};
function mimeFor(p) {
    const ext = path.extname(p).toLowerCase();
    return MIME[ext] || 'application/octet-stream';
}

// ── load config for mock API ───────────────────────────────────────────────
let mockConfig = null;
try {
    const cfgText = fs.readFileSync(path.join(wwwroot, 'js', 'config.js'), 'utf8');
    // Extract the JS object literal: `const PORTFOLIO_CONFIG = { ... };`
    // Use greedy match so the full outer object is captured (lazy would stop at first }).
    const m = cfgText.match(/const\s+PORTFOLIO_CONFIG\s*=\s*(\{[\s\S]*\})\s*;/);
    if (m) {
        // Use Function to evaluate the object in isolation (no global leak)
        mockConfig = Function('"use strict"; return (' + m[1] + ');')();
    }
} catch (e) {
    console.warn('[preview] could not load js/config.js for mock API:', e.message);
}
if (!mockConfig) {
    mockConfig = {
        profile: { name: 'Mahbod Pour', title: 'Frontend Developer', bio: 'Portfolio preview' },
        projects: [], skills: [], experiences: [], testimonials: []
    };
}

// Normalize mock shapes to resemble the real API DTOs
function mockProfile() {
    const p = mockConfig.profile || {};
    return {
        id: 1, fullName: p.name || 'Mahbod Pour', title: p.title || 'Frontend Developer',
        bio: p.bio || '', email: p.email || 'mahbod@example.com',
        location: p.location || 'Frankfurt am Main, Germany',
        linkedIn: p.social?.linkedin || '#', gitHub: p.social?.github || '#',
        twitter: p.social?.twitter || '#', showreelUrl: null, avatarUrl: 'images/hero-portrait.png'
    };
}
function mockProjects() {
    return (mockConfig.projects || []).map((pr, i) => ({
        id: pr.id ?? i + 1, title: pr.title, category: pr.category, description: pr.desc || pr.description || '',
        imageUrl: `images/project-${(i % 3) + 1}.png`, link: pr.link || '#', displayOrder: i,
        technologies: pr.tech || []
    }));
}
function mockSkills() {
    const all = mockConfig.skills || [];
    // Original API returns { bars, tags } — bars are the same items
    return { bars: all, tags: all };
}
function mockExperiences() {
    return (mockConfig.experiences || []).map((e, i) => ({
        id: i + 1, company: e.company, role: e.role, period: e.period, description: e.desc || e.description || ''
    }));
}
function mockTestimonials() {
    return (mockConfig.testimonials || []).map((t, i) => ({
        id: i + 1, name: t.name, role: t.role, content: t.text || t.content || '', rating: 5
    }));
}

// ── helpers ────────────────────────────────────────────────────────────────
function send(res, status, body, headers = {}) {
    const h = { 'Access-Control-Allow-Origin': '*', 'Access-Control-Allow-Methods': 'GET,POST,PUT,DELETE,OPTIONS', 'Access-Control-Allow-Headers': 'Content-Type, Authorization', ...headers };
    res.writeHead(status, h);
    res.end(body);
}
function json(res, status, data) {
    send(res, status, JSON.stringify(data), { 'Content-Type': 'application/json; charset=utf-8' });
}
function safeJoin(base, reqPath) {
    // Prevent directory traversal, strip query
    const clean = reqPath.split('?')[0].split('#')[0];
    const decoded = decodeURIComponent(clean);
    // Normalize and ensure it stays under base
    const full = path.normalize(path.join(base, decoded));
    if (!full.startsWith(base)) return path.join(base, 'index.html');
    return full;
}
function exists(p) {
    try { fs.accessSync(p, fs.constants.R_OK); return true; } catch { return false; }
}

// ── request handler ────────────────────────────────────────────────────────
function handler(req, res) {
    // CORS preflight
    if (req.method === 'OPTIONS') {
        return send(res, 204, '', { 'Access-Control-Allow-Origin': '*', 'Access-Control-Allow-Methods': 'GET,POST,PUT,DELETE,OPTIONS', 'Access-Control-Allow-Headers': 'Content-Type, Authorization' });
    }

    // Normalize URL: need to handle preview host where origin is https://{port}-{id}.e2b.app
    // For API mock, treat /api/* same regardless of host
    const url = new URL(req.url, `http://${req.headers.host || 'localhost'}`);
    let pathname = url.pathname;

    // ── Mock API endpoints (so the Blazor app works without the real API) ────
    // Only mock public portfolio endpoints; auth/admin return diagnostic 503
    if (pathname.startsWith('/api/')) {
        // Log for debugging
        // console.log(`[preview mock] ${req.method} ${pathname}`);

        if (req.method === 'GET' && pathname === '/api/portfolio/profile') return json(res, 200, mockProfile());
        if (req.method === 'GET' && pathname === '/api/portfolio/projects') return json(res, 200, mockProjects());
        if (req.method === 'GET' && pathname === '/api/portfolio/skills') return json(res, 200, mockSkills());
        if (req.method === 'GET' && pathname === '/api/portfolio/experiences') return json(res, 200, mockExperiences());
        if (req.method === 'GET' && pathname === '/api/portfolio/testimonials') return json(res, 200, mockTestimonials());
        if (req.method === 'GET' && pathname === '/api/portfolio/settings') return json(res, 200, { siteName: 'Mahbod Pour' });
        if (req.method === 'POST' && pathname === '/api/portfolio/contact') {
            // Consume body and echo success
            let body = '';
            req.on('data', chunk => body += chunk);
            req.on('end', () => {
                console.log('[preview mock] contact received:', body.slice(0, 300));
                json(res, 200, { success: true, message: 'Message received (preview mock — not persisted). Run the real API for persistence.' });
            });
            return;
        }
        if (pathname.startsWith('/api/auth/') || pathname.startsWith('/api/admin/') || pathname.startsWith('/api/resources')) {
            return json(res, 503, {
                success: false,
                message: 'Preview server has no database/auth. Run `cd src/Portfolio.Api && dotnet run` for full API. Public /api/portfolio/* is mocked from js/config.js.'
            });
        }
        if (pathname === '/api/portfolio/contact' && req.method !== 'POST') {
            return json(res, 405, { success: false, message: 'Method not allowed' });
        }
        // Unknown API — 404 JSON
        return json(res, 404, { success: false, message: `Preview mock has no handler for ${pathname}. Check .arena/handoff.md for API reference.` });
    }

    // ── Health ───────────────────────────────────────────────────────────────
    if (pathname === '/health' || pathname === '/api/health') {
        return json(res, 200, { status: 'ok', server: 'preview-server', wwwroot, time: new Date().toISOString() });
    }

    // ── Swagger hint when hitting API-like port ─────────────────────────────
    if (pathname === '/swagger' || pathname === '/swagger/') {
        return send(res, 200,
            '<!doctype html><meta charset="utf-8"><title>Preview — no Swagger</title>'
            + '<body style="font-family:system-ui;padding:40px;max-width:640px;margin:auto"><h1>Preview server (no Swagger)</h1>'
            + '<p>This is the <strong>static preview server</strong> (port ' + port + '). Swagger is served by the real API at <code>https://localhost:49325/swagger</code> when you run <code>cd src/Portfolio.Api && dotnet run</code>.</p>'
            + '<p><a href="/">Back to portfolio</a></p>',
            { 'Content-Type': 'text/html; charset=utf-8' });
    }

    // ── Static file serving ────────────────────────────────────────────────
    // Map / → /index.html
    if (pathname === '/') pathname = '/index.html';

    let filePath = safeJoin(wwwroot, pathname);

    // If file doesn't exist, SPA fallback: serve index.html for non-file routes
    // (so Blazor routing works: /login, /admin, etc.)
    let isFallback = false;
    if (!exists(filePath) || fs.statSync(filePath, { throwIfNoEntry: false })?.isDirectory()) {
        // Don't fallback for obvious file requests (with extension) — return 404 instead
        const hasExt = path.extname(pathname) !== '';
        if (hasExt) {
            // Special case: missing _framework file beyond the stub → return diagnostic
            if (pathname.startsWith('/_framework/')) {
                console.warn(`[preview] missing framework file: ${pathname}`);
                return send(res, 404,
                    '/* Portfolio preview: missing ' + pathname + ' — run `dotnet build` / `dotnet publish` for the real framework. The stub at /_framework/blazor.webassembly.js handles the boot; other framework files are only needed for the compiled app. */',
                    { 'Content-Type': 'application/javascript; charset=utf-8', 'Cache-Control': 'no-store' });
            }
            return send(res, 404, `Not found: ${pathname}`, { 'Content-Type': 'text/plain; charset=utf-8' });
        }
        // SPA fallback
        filePath = path.join(wwwroot, 'index.html');
        isFallback = true;
    }

    try {
        const data = fs.readFileSync(filePath);
        const mime = mimeFor(filePath);
        const headers = { 'Content-Type': mime, 'Cache-Control': isFallback ? 'no-cache' : 'public, max-age=300' };
        // Security: allow embedding in Arena preview iframe
        headers['X-Frame-Options'] = 'ALLOWALL';
        // Let preview host wrap correctly
        headers['Content-Security-Policy'] = "frame-ancestors *";
        return send(res, 200, data, headers);
    } catch (e) {
        console.error('[preview] error serving', filePath, e.message);
        return send(res, 500, 'Internal error', { 'Content-Type': 'text/plain' });
    }
}

// ── start ──────────────────────────────────────────────────────────────────
const server = http.createServer(handler);
server.on('error', err => {
    console.error('[preview] server error:', err);
    process.exit(1);
});
server.listen(port, host, () => {
    const addr = server.address();
    console.log(`\n✓ Portfolio preview server running`);
    console.log(`  wwwroot: ${wwwroot}`);
    console.log(`  local:   http://localhost:${port}/`);
    console.log(`  network: http://${host}:${port}/`);
    // Arena preview hint
    if (process.env.E2B_SANDBOX_ID) {
        const id = process.env.E2B_SANDBOX_ID;
        console.log(`  preview: https://${port}-${id}.e2b.app/  (Arena)`);
        console.log(`  note:    API mocked from js/config.js — run 'cd src/Portfolio.Api && dotnet run' for real API on 49325, and 'cd src/Portfolio.Web && dotnet run' for Blazor on 49323.`);
    }
    console.log(`  health:  http://localhost:${port}/health`);
    console.log(`  mock API: /api/portfolio/profile, /api/portfolio/projects, /api/portfolio/skills, ...`);
    console.log(`  stub:    /_framework/blazor.webassembly.js → static preview (prevents 404 on API port)\n`);
});

// Graceful shutdown
process.on('SIGINT', () => server.close(() => process.exit(0)));
process.on('SIGTERM', () => server.close(() => process.exit(0)));
