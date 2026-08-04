/**
 * portfolio.lang.js — Bilingual Engine (en/fa)
 * Loads wwwroot/lang/{code}.json, rewrites all [data-i18n] elements
 * RTL/LTR direction, font switching, language persistence in localStorage
 */
(function(){
'use strict';

var DEFAULT_LANG = 'en';
var currentLang = localStorage.getItem('lang') || DEFAULT_LANG;
var translations = {};
var loaded = false;

// ── Public API ──
window.__ = window.__i18n = function(key) {
  var parts = key.split('.');
  var val = translations;
  for (var i = 0; i < parts.length; i++) {
    if (val == null) return key;
    val = val[parts[i]];
  }
  return val != null ? val : key;
};

window.setLanguage = function(lang, callback) {
  currentLang = lang;
  localStorage.setItem('lang', lang);
  loadLang(lang, function() { applyDOM(); if (callback) callback(); });
};

window.getLanguage = function() { return currentLang; };

// ── Load JSON ──
function loadLang(lang, callback) {
  var xhr = new XMLHttpRequest();
  xhr.open('GET', '/lang/' + lang + '.json?t=' + Date.now(), true);
  xhr.onload = function() {
    if (xhr.status === 200) {
      try { translations = JSON.parse(xhr.responseText); loaded = true; }
      catch(e) { console.warn('lang: JSON parse error', e); translations = {}; }
    }
    if (callback) callback();
  };
  xhr.onerror = function() { if (callback) callback(); };
  xhr.send();
}

// ── Apply translations to DOM ──
function applyDOM() {
  if (!loaded || !translations.site) return;

  var lang = translations.site.lang || 'en';
  var dir  = translations.site.direction || 'ltr';

  // HTML attributes
  document.documentElement.lang = lang;
  document.documentElement.dir = dir;
  document.documentElement.classList.toggle('rtl', dir === 'rtl');
  document.documentElement.classList.toggle('ltr', dir === 'ltr');

  // Font switch for Persian
  if (dir === 'rtl') {
    document.documentElement.style.setProperty('--font-sans', "'Vazirmatn', 'Inter', 'Tahoma', sans-serif");
    document.documentElement.style.setProperty('--font-display', "'Vazirmatn', 'Playfair Display', serif");
  } else {
    document.documentElement.style.setProperty('--font-sans', "'Inter', -apple-system, BlinkMacSystemFont, system-ui, sans-serif");
    document.documentElement.style.setProperty('--font-display', "'Playfair Display', Georgia, serif");
  }

  // <title>
  document.title = __i18n('site.title');

  // All [data-i18n] elements
  var els = document.querySelectorAll('[data-i18n]');
  for (var i = 0; i < els.length; i++) {
    var el = els[i];
    var key = el.getAttribute('data-i18n');
    var val = __i18n(key);
    if (!val) continue;
    
    // If it has children (like SVG icons), only change text nodes
    if (el.children.length > 0) {
      for (var j = 0; j < el.childNodes.length; j++) {
        if (el.childNodes[j].nodeType === 3) { // Text node
          el.childNodes[j].textContent = val;
          break;
        }
      }
    } else {
      el.textContent = val;
    }
  }

  // All [data-i18n-placeholder]
  var phs = document.querySelectorAll('[data-i18n-placeholder]');
  for (var i = 0; i < phs.length; i++) {
    var p = phs[i];
    p.placeholder = __i18n(p.getAttribute('data-i18n-placeholder')) || '';
  }

  // All [data-i18n-aria]
  var ars = document.querySelectorAll('[data-i18n-aria]');
  for (var i = 0; i < ars.length; i++) {
    var a = ars[i];
    a.setAttribute('aria-label', __i18n(a.getAttribute('data-i18n-aria')) || '');
  }

  // Update toast language
  if (window.PortfolioToast) {
    var t = window.PortfolioToast;
    if (lang === 'fa') {
      t._lang = 'fa';
    } else {
      t._lang = 'en';
    }
  }

  // Store current
  window._currentLang = lang;
}

// ── Language Switcher Button ──
function createSwitcher() {
  var container = document.querySelector('.hero-header');
  if (!container) return;

  // Remove existing
  var old = document.getElementById('langSwitcher');
  if (old) old.remove();

  var btn = document.createElement('button');
  btn.id = 'langSwitcher';
  btn.className = 'btn-lang';
  btn.textContent = currentLang === 'en' ? 'فا' : 'EN';
  btn.setAttribute('aria-label', 'Switch language');
  btn.title = currentLang === 'en' ? 'Switch to Persian' : 'Switch to English';

  btn.addEventListener('click', function() {
    var next = currentLang === 'en' ? 'fa' : 'en';
    window.setLanguage(next);
  });

  // Insert before "Let's Talk"
  var talkBtn = container.querySelector('.btn-pill');
  if (talkBtn) {
    container.insertBefore(btn, talkBtn);
  } else {
    container.appendChild(btn);
  }
}

// ── Init ──
loadLang(currentLang, function() {
  applyDOM();
  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', function() {
      setTimeout(createSwitcher, 100);
    });
  } else {
    setTimeout(createSwitcher, 100);
  }
});

// Expose for Blazor interop
window.i18nInit = function(lang) {
  if (lang) currentLang = lang;
  loadLang(currentLang, function() { applyDOM(); createSwitcher(); });
};

})();
