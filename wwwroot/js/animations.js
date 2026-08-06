/**
 * portfolio.animations.js - Premium UX Engine (re-initializable)
 * Preloader . Scroll-Spy . Back-to-Top . Box Entrance . Section Dividers
 * Parallax . Text Reveal . Stagger . Pin/Scrub . Counter . Glass . Tilt
 *
 * Every effect is registered as a guarded module in MODULES[], so
 * window.portfolio.init() can (re)run them idempotently. This makes the
 * engine work BOTH in the static preview (auto-runs on DOMContentLoaded,
 * when the DOM already exists) AND in the Blazor WASM app (which calls
 * portfolio.init() again AFTER it renders content into #app).
 */
(function(){'use strict';

// -- Core state (shared) --
var $=function(s,p){return(p||document).querySelector(s);};
var $$=function(s,p){var n=(p||document).querySelectorAll(s);var a=[];for(var i=0;i<n.length;i++)a.push(n[i]);return a;};
var reduced=window.matchMedia('(prefers-reduced-motion: reduce)').matches;
var isTouch=window.matchMedia('(pointer: coarse)').matches;
var isMobile=isTouch||window.innerWidth<769;
var scrollY=0,viewH=window.innerHeight,docH=0,rafId=0;

// Scroll listeners are re-registered on every init() so stale nodes are dropped.
var listeners=[];
function listen(fn){listeners.push(fn);}
function onScroll(){scrollY=window.scrollY;docH=document.documentElement.scrollHeight;if(!rafId)rafId=requestAnimationFrame(tick);}
window.addEventListener('scroll',onScroll,{passive:true});
window.matchMedia('(prefers-reduced-motion: reduce)').addEventListener('change',function(e){reduced=e.matches;});
window.addEventListener('resize',function(){viewH=window.innerHeight;docH=document.documentElement.scrollHeight;isMobile=isTouch||window.innerWidth<769;});
function tick(){rafId=0;var dh=docH-viewH,prog=dh>0?Math.min(scrollY/dh,1):0;for(var i=0;i<listeners.length;i++)listeners[i](scrollY,viewH,prog);}

// -- Module registry --
var MODULES=[];
function register(fn){MODULES.push(fn);}
var observers=[];

// Marks an element as already-bound for a module (prevents duplicate listeners).
function bound(el,key){
  if(!el)return true;
  var k='pf_'+key;
  if(el.dataset&&el.dataset[k])return true;
  if(el.dataset)el.dataset[k]='1';
  return false;
}

// -- init(): reset stale state, then re-run every module against current DOM --
function init(){
  var p=document.getElementById('preloader');if(p&&p.parentNode)p.parentNode.removeChild(p);
  var b=document.getElementById('backToTop');if(b&&b.parentNode)b.parentNode.removeChild(b);
  var divs=document.querySelectorAll('.section-divider');
  for(var j=0;j<divs.length;j++)if(divs[j].parentNode)divs[j].parentNode.removeChild(divs[j]);
  listeners.length=0;
  for(var i=0;i<observers.length;i++){try{observers[i].disconnect();}catch(e){}}
  observers.length=0;
  for(var m=0;m<MODULES.length;m++){try{MODULES[m]();}catch(e){}}
}

window.portfolio={init:init,refresh:init,dismissSplash:function(){var p=document.getElementById('preloader');if(p){p.style.opacity='0';setTimeout(function(){if(p.parentNode)p.remove();},600);}}};

// Auto-run for static preview (and safe for Blazor pre-render shell).
if(document.readyState==='loading'){document.addEventListener('DOMContentLoaded',init);}else{init();}

// ==== 0. PRELOADER (guarded: only created once) ====
var preloaderDone=false;
register(function(){if(reduced||preloaderDone)return;preloaderDone=true;var el=document.createElement('div');el.id='preloader';el.innerHTML='<div class="preloader-bg"></div><div class="preloader-ring"><svg viewBox="0 0 100 100"><circle cx="50" cy="50" r="44" fill="none" stroke="rgba(255,255,255,0.06)" stroke-width="2"/><circle id="pArc" cx="50" cy="50" r="44" fill="none" stroke="var(--accent)" stroke-width="2" stroke-linecap="round" stroke-dasharray="276.46" stroke-dashoffset="276.46" transform="rotate(-90 50 50)"/></svg><span class="preloader-pct" id="pPct">0%</span></div><div class="preloader-label">Loading</div>';document.body.appendChild(el);var arc=el.querySelector('#pArc'),pctEl=el.querySelector('#pPct'),prog=0,target=0,done=false;function check(){if(done)return;var res=performance.getEntriesByType('resource');target=Math.max(prog,Math.min(98,(res.filter(function(r){return r.transferSize>0||r.duration>0}).length/(res.length||8))*100));if(document.readyState==='complete')target=100;if(!done)setTimeout(check,200);}function update(){if(done)return;prog+=(target-prog)*0.07;var p=Math.round(Math.min(prog,100));arc.style.strokeDashoffset=276.46-(276.46*p/100);pctEl.textContent=p+'%';if(p>=100){finish();return;}requestAnimationFrame(update);}function finish(){if(done)return;done=true;el.style.transition='opacity 0.5s ease';el.style.opacity='0';setTimeout(function(){if(el.parentNode)el.remove();},600);}check();update();});

// ==== 1. SCROLL-SPY - active nav link ====
register(function(){var sections=$$('[data-section]');var navLinks=$$('.nav-link, .mobile-nav a[href^="#"]');if(!sections.length||!navLinks.length)return;listen(function(sy,wh){var cur='',closest=1e9;sections.forEach(function(sec){var r=sec.getBoundingClientRect(),d=Math.abs(r.top-wh*0.35);if(d<closest){closest=d;cur=sec.getAttribute('id')||'';}});navLinks.forEach(function(l){l.classList.toggle('nav-active',l.getAttribute('href').replace('#','')===cur);});});});

// ==== 2. BACK-TO-TOP ====
register(function(){var b=document.createElement('button');b.id='backToTop';b.innerHTML='<svg width="20" height="20" viewBox="0 0 20 20" fill="none"><path d="M10 16V4M5 9l5-5 5 5" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/></svg>';b.setAttribute('aria-label','Back to top');document.body.appendChild(b);listen(function(sy){b.classList.toggle('visible',sy>500);});b.addEventListener('click',function(){window.scrollTo({top:0,behavior:'smooth'});});});

// ==== 3. BOX ENTRANCE - scale+fade on scroll ====
register(function(){var boxes=$$('.bento-box');if(!boxes.length)return;var obs=new IntersectionObserver(function(es){es.forEach(function(e){if(e.isIntersecting){e.target.classList.add('box-entered');obs.unobserve(e.target);}});},{threshold:0.04,rootMargin:'0px 0px -30px 0px'});observers.push(obs);boxes.forEach(function(bx){obs.observe(bx);});});

// ==== 4. SECTION DIVIDERS ====
register(function(){if(isMobile)return;var boxes=$$('.bento-box');boxes.forEach(function(box,i){if(i<boxes.length-1){var d=document.createElement('div');d.className='section-divider';box.insertAdjacentElement('afterend',d);}});var divs=$$('.section-divider');if(!divs.length)return;var obs=new IntersectionObserver(function(es){es.forEach(function(e){if(e.isIntersecting){e.target.classList.add('divider-revealed');obs.unobserve(e.target);}});},{threshold:0.4});observers.push(obs);divs.forEach(function(d){obs.observe(d);});});

// ==== 5. PARALLAX ====
register(function(){if(reduced||isTouch)return;var layers=$$('[data-parallax]').map(function(el){return{el:el,speed:parseFloat(el.dataset.speed)||0.12};});if(!layers.length)return;listen(function(sy,wh){for(var i=0;i<layers.length;i++){var l=layers[i],r=l.el.getBoundingClientRect();if(r.bottom>-150&&r.top<wh+150)l.el.style.transform='translateY('+((r.top+r.height/2-wh/2)*l.speed).toFixed(1)+'px) translateZ(0)';}});});

// ==== 6. TEXT REVEAL (guarded: skips already-revealed elements) ====
register(function(){if(reduced){$$('[data-text-reveal]').forEach(function(e){e.style.opacity='1';});return;}var els=$$('[data-text-reveal]');if(!els.length)return;els.forEach(function(el){if(bound(el,'tr'))return;var text=el.textContent.trim();if(!text)return;el.textContent='';el.style.opacity='1';var words=text.split(' ').filter(Boolean);words.forEach(function(word,wi){var w=document.createElement('span');w.className='reveal-word';w.style.display='inline-block';w.style.transitionDelay=(wi*40)+'ms';if(isMobile){w.textContent=word;}else{for(var ci=0;ci<word.length;ci++){var c=document.createElement('span');c.className='reveal-char';c.textContent=word[ci];c.style.display='inline-block';w.appendChild(c);}}el.appendChild(w);if(wi<words.length-1)el.appendChild(document.createTextNode(' '));});});var obs=new IntersectionObserver(function(entries){entries.forEach(function(e){if(e.isIntersecting){var ch=e.target.querySelectorAll('.reveal-char');if(ch.length){ch.forEach(function(c,i){setTimeout(function(){c.classList.add('visible')},i*10);});}else{e.target.querySelectorAll('.reveal-word').forEach(function(w){w.classList.add('visible');});}obs.unobserve(e.target);}});},{threshold:0.25});observers.push(obs);els.forEach(function(el){obs.observe(el);});});

// ==== 7. STAGGER ====
register(function(){var obs=new IntersectionObserver(function(entries){entries.forEach(function(e){if(e.isIntersecting){var kids=e.target.children;for(var i=0;i<kids.length;i++){kids[i].style.transitionDelay=(i*60)+'ms';kids[i].classList.add('revealed');}e.target.classList.add('revealed');obs.unobserve(e.target);}});},{threshold:0.06,rootMargin:'0px 0px -15px 0px'});observers.push(obs);$$('[data-stagger]').forEach(function(el){obs.observe(el);});});

// ==== 8. PIN/SCRUB ====
register(function(){if(reduced||isTouch)return;var sections=$$('[data-pin]').map(function(el){return{el:el,h:el.offsetHeight,top:0};});if(!sections.length)return;function cache(){var sy=window.scrollY;sections.forEach(function(s){s.h=s.el.offsetHeight;s.top=s.el.getBoundingClientRect().top+sy;});}cache();window.addEventListener('resize',cache);listen(function(sy,wh){for(var i=0;i<sections.length;i++){var s=sections[i],start=s.top-wh*0.15,end=s.top+s.h-wh*0.25;if(sy>=start&&sy<=end){var p=Math.min(1,Math.max(0,(sy-start)/(end-start)));s.el.style.setProperty('--pin-progress',p.toFixed(3));s.el.classList.add('is-pinned');}}});});

// ==== 9. COUNTER ====
register(function(){var obs=new IntersectionObserver(function(entries){entries.forEach(function(e){if(e.isIntersecting){var el=e.target,target=parseInt(el.getAttribute('data-target'))||5,pre=el.dataset.prefix||'',suf=el.dataset.suffix||'',start=performance.now();(function tickf(){var t=Math.min((performance.now()-start)/1800,1),v=Math.round(target*(1-Math.pow(1-t,3)));el.textContent=pre+v+suf;if(t<1)requestAnimationFrame(tickf);else el.textContent=pre+target+suf;})();obs.unobserve(e.target);}});},{threshold:0.5});observers.push(obs);$$('[data-counter]').forEach(function(el){if(bound(el,'ct'))return;var t=parseInt(el.dataset.counter)||parseInt(el.textContent)||0;if(t<=0)return;el.textContent='0';el.setAttribute('data-target',String(t));obs.observe(el);});});

// ==== 10. COLOR SHIFT ====
register(function(){if(reduced||isTouch)return;var root=document.documentElement;listen(function(sy,wh,prog){var r=212+Math.round(prog*20),g=160+Math.round(prog*10),b=76-Math.round(prog*20);root.style.setProperty('--accent','#'+[r,g,b].map(function(v){var h=Math.max(0,Math.min(255,v)).toString(16);return h.length===1?'0'+h:h;}).join(''));});});

// ==== 11. GLASSMORPHISM ====
register(function(){if(isTouch||reduced)return;$$('.glass-panel').forEach(function(el){if(bound(el,'gl'))return;el.addEventListener('mousemove',function(e){var r=el.getBoundingClientRect(),x=(e.clientX-r.left)/r.width,y=(e.clientY-r.top)/r.height;el.style.setProperty('--glass-x',x.toFixed(2));el.style.setProperty('--glass-y',y.toFixed(2));el.style.setProperty('--glass-rotateX',((y-0.5)*3).toFixed(2)+'deg');el.style.setProperty('--glass-rotateY',((x-0.5)*3).toFixed(2)+'deg');});el.addEventListener('mouseleave',function(){el.style.setProperty('--glass-rotateX','0deg');el.style.setProperty('--glass-rotateY','0deg');});});});

// ==== 12. CURSOR GLOW + MAGNETIC ====
register(function(){if(isTouch||reduced)return;var glow=$('#cursorGlow');if(!glow)return;var mx=0,my=0,cx=0,cy=0;document.addEventListener('mousemove',function(e){mx=e.clientX;my=e.clientY;glow.classList.add('visible');},{passive:true});document.addEventListener('mouseleave',function(){glow.classList.remove('visible');});(function loop(){cx+=(mx-cx)*0.05;cy+=(my-cy)*0.05;glow.style.transform='translate('+cx.toFixed(0)+'px,'+cy.toFixed(0)+'px) translate(-50%,-50%)';requestAnimationFrame(loop);})();$$('[data-magnetic]').forEach(function(btn){if(bound(btn,'mg'))return;btn.addEventListener('mousemove',function(e){var r=btn.getBoundingClientRect();btn.style.transform='translate('+((e.clientX-r.left-r.width/2)*0.12).toFixed(1)+'px,'+((e.clientY-r.top-r.height/2)*0.12).toFixed(1)+'px)';});btn.addEventListener('mouseleave',function(){btn.style.transform='';});});});

// ==== 13. CARD TILT/TAP ====
register(function(){var cards=$$('.project-card');if(!cards.length)return;if(!isTouch){cards.forEach(function(card){if(bound(card,'ct'))return;card.addEventListener('mousemove',function(e){var r=card.getBoundingClientRect(),rx=((e.clientY-r.top-r.height/2)/(r.height/2))*-10,ry=((e.clientX-r.left-r.width/2)/(r.width/2))*10;card.style.transform='rotateY('+ry.toFixed(1)+'deg) rotateX('+rx.toFixed(1)+'deg) translateY(-18px) scale(1.05) translateZ(0)';card.style.zIndex='10';});card.addEventListener('mouseleave',function(){card.style.transform='';card.style.zIndex='';});});}else{cards.forEach(function(card){if(bound(card,'ct'))return;card.addEventListener('touchstart',function(){card.style.transform='scale(0.96)';card.style.transition='transform .15s ease';},{passive:true});card.addEventListener('touchend',function(){card.style.transform='';setTimeout(function(){card.style.transition='';},200);});});}});

// ==== 14. SKILL BARS ====
register(function(){var done=false,obs=new IntersectionObserver(function(entries){entries.forEach(function(en){if(en.isIntersecting&&!done){$$('.skill-fill').forEach(function(b,i){setTimeout(function(){b.classList.add('visible')},i*60);});done=true;obs.unobserve(en.target);}});},{threshold:0.1});var box=$('.box-skills');if(box){observers.push(obs);obs.observe(box);}});

// ==== 15. CAROUSEL ====
register(function(){var track=$('#testimonialTrack'),dots=$('#testimonialDots');if(!track||!dots)return;if(bound(track,'cr'))return;var total=track.querySelectorAll('.testimonial-slide').length,cur=0,interval;if(total<2)return;function go(i){cur=i;track.style.transform='translateX(-'+(i*100)+'%) translateZ(0)';dots.querySelectorAll('.dot').forEach(function(d,j){d.classList.toggle('active',j===i);});}dots.querySelectorAll('.dot').forEach(function(d,i){d.addEventListener('click',function(){go(i);rst();});});var sx=0;track.addEventListener('touchstart',function(e){sx=e.touches[0].clientX;},{passive:true});track.addEventListener('touchend',function(e){var dx=sx-e.changedTouches[0].clientX;if(Math.abs(dx)>35){dx>0?go((cur+1)%total):go((cur-1+total)%total);rst();}});function rst(){clearInterval(interval);interval=setInterval(function(){go((cur+1)%total);},4500);}rst();});

// ==== 16. MOBILE MENU ====
register(function(){var btn=$('#mobileMenuBtn'),nav=$('#mobileNav');if(!btn||!nav)return;if(bound(btn,'mm'))return;var open=false;btn.addEventListener('click',function(){open=!open;btn.classList.toggle('open',open);nav.classList.toggle('open',open);document.body.style.overflow=open?'hidden':'';});nav.querySelectorAll('a').forEach(function(a){a.addEventListener('click',function(){open=false;btn.classList.remove('open');nav.classList.remove('open');document.body.style.overflow='';});});document.addEventListener('keydown',function(e){if(e.key==='Escape'&&open){open=false;btn.classList.remove('open');nav.classList.remove('open');document.body.style.overflow='';}});});

// ==== 17. HERO PROGRESS ====
register(function(){var bar=$('#heroProgress');if(!bar)return;listen(function(sy,wh,prog){bar.style.transform='scaleX('+prog.toFixed(3)+')';});});

// ==== 18. SHOWREEL + CONTACT ====
register(function(){var btn=$('#showreelBtn');if(btn&&!bound(btn,'sr'))btn.addEventListener('click',function(){btn.style.transform='scale(0.94)';setTimeout(function(){btn.style.transform='';},150);});var f=$('#contactForm');if(f&&!bound(f,'cf'))f.addEventListener('submit',function(e){e.preventDefault();var b=f.querySelector('button[type="submit"]');if(!b)return;var orig=b.innerHTML;b.innerHTML='<span>Sending...</span>';b.style.pointerEvents='none';setTimeout(function(){b.innerHTML='<span>✓ Sent!</span>';b.style.background='var(--accent)';b.style.color='#000';setTimeout(function(){b.innerHTML=orig;b.style.background='';b.style.color='';b.style.pointerEvents='';f.reset();},2500);},1200);});});

// ==== 19. DATA-REVEAL (generic fade-up on scroll) ====
register(function(){
  var els=$$('[data-reveal]');
  if(!els.length)return;
  if(reduced){els.forEach(function(e){e.style.opacity='1';e.style.transform='none';});return;}
  var obs=new IntersectionObserver(function(entries){
    entries.forEach(function(e){
      if(e.isIntersecting){e.target.classList.add('revealed');obs.unobserve(e.target);}
    });
  },{threshold:0.12,rootMargin:'0px 0px -8% 0px'});
  observers.push(obs);
  els.forEach(function(el){if(bound(el,'rv'))return;bound(el,'rv');obs.observe(el);});
});


})();

// ==== TOAST NOTIFICATION SYSTEM ====
window.PortfolioToast = (function() {
  var container = null;
  function ensure() {
    if (container) return;
    container = document.createElement('div'); container.id = 'toastContainer';
    container.style.cssText = 'position:fixed;bottom:24px;right:24px;z-index:9999;display:flex;flex-direction:column;gap:8px;max-width:380px;';
    document.body.appendChild(container);
  }
  function show(msg, type) {
    ensure();
    var t = document.createElement('div');
    t.className = 'toast-item toast-' + (type || 'info');
    t.innerHTML = '<span class="toast-msg">' + msg + '</span><button class="toast-close" onclick="this.parentElement.remove()">x</button>';
    container.appendChild(t);
    requestAnimationFrame(function() { t.classList.add('visible'); });
    setTimeout(function() { t.classList.remove('visible'); setTimeout(function() { if (t.parentNode) t.remove(); }, 400); }, 4000);
  }
  return {
    error: function(msg) { show(msg, 'error'); },
    warning: function(msg) { show(msg, 'warning'); },
    success: function(msg) { show(msg, 'success'); },
    info: function(msg) { show(msg, 'info'); }
  };
})();

