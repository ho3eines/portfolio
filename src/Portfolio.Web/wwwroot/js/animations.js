/**
 * portfolio.animations.js — lightweight UX engine for the redesigned bento site.
 * Re-initializable: window.portfolio.init() is called by Blazor after render.
 */
(function () {
    'use strict';

    var $ = function (s, p) { return (p || document).querySelector(s); };
    var $$ = function (s, p) { return Array.prototype.slice.call((p || document).querySelectorAll(s)); };
    var reduced = window.matchMedia('(prefers-reduced-motion: reduce)').matches;

    var observers = [];
    function mark(el, k) {
        if (!el) return true;
        var key = 'pf_' + k;
        if (el.dataset[key]) return true;
        el.dataset[key] = '1';
        return false;
    }

    function reveals() {
        var els = $$('[data-reveal]');
        if (!els.length) return;
        if (reduced) { els.forEach(function (e) { e.classList.add('in'); }); return; }
        var obs = new IntersectionObserver(function (entries) {
            entries.forEach(function (e) {
                if (e.isIntersecting) { e.target.classList.add('in'); obs.unobserve(e.target); }
            });
        }, { threshold: 0.12, rootMargin: '0px 0px -8% 0px' });
        observers.push(obs);
        els.forEach(function (el) { if (!mark(el, 'rv')) obs.observe(el); });
    }

    function skillBars() {
        var box = $('.box-skills');
        var fills = $$('.skill-fill');
        if (!box || !fills.length) return;
        if (reduced) { fills.forEach(function (f) { f.style.width = (f.style.getPropertyValue('--fill') || '80%'); }); return; }
        var obs = new IntersectionObserver(function (entries) {
            entries.forEach(function (e) {
                if (e.isIntersecting) {
                    fills.forEach(function (f, i) {
                        setTimeout(function () { f.style.width = (f.style.getPropertyValue('--fill') || '80%'); }, i * 90);
                    });
                    obs.unobserve(e.target);
                }
            });
        }, { threshold: 0.2 });
        observers.push(obs);
        obs.observe(box);
    }

    function carousel() {
        var track = $('#tsTrack');
        var prev = $('#tsPrev');
        var next = $('#tsNext');
        if (!track || !prev || !next || mark(track, 'cr')) return;
        var slides = track.querySelectorAll('.ts-slide');
        var total = slides.length;
        if (total < 2) return;
        var cur = 0;
        var timer = null;

        function go(i) {
            cur = (i + total) % total;
            track.style.transform = 'translateX(-' + (cur * 100) + '%)';
        }
        function rst() { clearInterval(timer); timer = setInterval(function () { go(cur + 1); }, 5500); }

        prev.addEventListener('click', function () { go(cur - 1); rst(); });
        next.addEventListener('click', function () { go(cur + 1); rst(); });

        var sx = 0;
        track.addEventListener('touchstart', function (e) { sx = e.touches[0].clientX; }, { passive: true });
        track.addEventListener('touchend', function (e) {
            var dx = sx - e.changedTouches[0].clientX;
            if (Math.abs(dx) > 35) { dx > 0 ? go(cur + 1) : go(cur - 1); rst(); }
        });
        rst();
    }

    function mobileMenu() {
        var btn = $('#mobileMenuBtn');
        var nav = $('#mobileNav');
        if (!btn || !nav || mark(btn, 'mm')) return;
        function set(open) {
            nav.classList.toggle('open', open);
            document.body.style.overflow = open ? 'hidden' : '';
        }
        btn.addEventListener('click', function () { set(!nav.classList.contains('open')); });
        $$('a', nav).forEach(function (a) { a.addEventListener('click', function () { set(false); }); });
        document.addEventListener('keydown', function (e) { if (e.key === 'Escape') set(false); });
    }

    function heroProgress() {
        var fill = document.querySelector('.hero-progress .fill');
        if (!fill) return;
        var ticking = false;
        function update() {
            var dh = document.documentElement.scrollHeight - window.innerHeight;
            var p = dh > 0 ? Math.min(window.scrollY / dh, 1) : 0.2;
            fill.style.width = (Math.max(0.2, p) * 100).toFixed(1) + '%';
            ticking = false;
        }
        window.addEventListener('scroll', function () { if (!ticking) { requestAnimationFrame(update); ticking = true; } }, { passive: true });
        update();
    }

    function init() {
        // Disconnect previous observers (re-init after Blazor re-render).
        observers.forEach(function (o) { try { o.disconnect(); } catch (e) { } });
        observers = [];
        reveals();
        skillBars();
        carousel();
        mobileMenu();
        heroProgress();
    }

    window.portfolio = window.portfolio || {};
    window.portfolio.init = init;

    // Auto-run once DOM is ready (safe no-op if Blazor re-runs later).
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();

// ==== TOAST NOTIFICATION SYSTEM ====
window.PortfolioToast = (function () {
    var container = null;
    function ensure() {
        if (container) return;
        container = document.createElement('div');
        container.id = 'toastContainer';
        container.style.cssText = 'position:fixed;bottom:24px;right:24px;z-index:9999;display:flex;flex-direction:column;gap:8px;max-width:380px;';
        document.body.appendChild(container);
    }
    function show(msg, type) {
        ensure();
        var t = document.createElement('div');
        t.className = 'toast-item toast-' + (type || 'info');
        t.innerHTML = '<span class="toast-msg">' + msg + '</span><button class="toast-close" onclick="this.parentElement.remove()">×</button>';
        container.appendChild(t);
        requestAnimationFrame(function () { t.classList.add('visible'); });
        setTimeout(function () { t.classList.remove('visible'); setTimeout(function () { if (t.parentNode) t.remove(); }, 400); }, 4000);
    }
    return {
        error: function (msg) { show(msg, 'error'); },
        warning: function (msg) { show(msg, 'warning'); },
        success: function (msg) { show(msg, 'success'); },
        info: function (msg) { show(msg, 'info'); }
    };
})();
