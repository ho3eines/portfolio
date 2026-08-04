/**
 * portfolio.splash.js
 * Premium splash screen with animated icon reveal
 * Fades out gracefully when the app is ready
 */
(function() {
  'use strict';

  const Splash = {
    overlay: null,
    icon: null,
    particles: [],
    canvas: null,
    ctx: null,
    animationId: null,
    dismissed: false,

    init() {
      // Create splash overlay
      this.createDOM();
      // Start particle animation
      this.initParticles();
      // Animate icon entrance
      this.animateIcon();
      // Auto-dismiss after site loads
      this.waitForReady();
    },

    createDOM() {
      this.overlay = document.createElement('div');
      this.overlay.id = 'splashScreen';
      this.overlay.innerHTML = `
        <div class="splash-bg"></div>
        <canvas id="splashParticles"></canvas>
        <div class="splash-content">
          <div class="splash-icon-wrapper">
            <div class="splash-glow"></div>
            <img src="/images/icon-192.png" alt="Loading..." class="splash-icon" />
          </div>
          <div class="splash-text">
            <span class="splash-name">Mahbod Pour</span>
            <span class="splash-title">Crafting Digital Experiences</span>
            <div class="splash-loader"></div>
          </div>
        </div>
      `;
      document.body.appendChild(this.overlay);
      this.icon = this.overlay.querySelector('.splash-icon');

      // Force layout
      this.overlay.offsetHeight;

      // Trigger entrance animation
      requestAnimationFrame(() => {
        this.overlay.querySelector('.splash-icon-wrapper').classList.add('visible');
        this.overlay.querySelector('.splash-text').classList.add('visible');
      });
    },

    initParticles() {
      this.canvas = document.getElementById('splashParticles');
      if (!this.canvas) return;
      this.ctx = this.canvas.getContext('2d');

      const resize = () => {
        this.canvas.width = window.innerWidth;
        this.canvas.height = window.innerHeight;
      };
      resize();
      window.addEventListener('resize', resize);

      // Create floating golden particles
      const count = 25;
      for (let i = 0; i < count; i++) {
        this.particles.push({
          x: Math.random() * this.canvas.width,
          y: Math.random() * this.canvas.height,
          r: Math.random() * 2 + 0.5,
          speedX: (Math.random() - 0.5) * 0.4,
          speedY: (Math.random() - 0.5) * 0.4 - 0.2,
          opacity: Math.random() * 0.5 + 0.1,
          pulse: Math.random() * Math.PI * 2
        });
      }

      this.animateParticles();
    },

    animateParticles() {
      if (this.dismissed) return;
      const ctx = this.ctx;
      if (!ctx) return;

      ctx.clearRect(0, 0, this.canvas.width, this.canvas.height);

      this.particles.forEach(p => {
        p.x += p.speedX;
        p.y += p.speedY;
        p.pulse += 0.02;

        // Wrap around
        if (p.x < -10) p.x = this.canvas.width + 10;
        if (p.x > this.canvas.width + 10) p.x = -10;
        if (p.y < -10) p.y = this.canvas.height + 10;
        if (p.y > this.canvas.height + 10) p.y = -10;

        const alpha = p.opacity + Math.sin(p.pulse) * 0.15;

        ctx.beginPath();
        ctx.arc(p.x, p.y, p.r, 0, Math.PI * 2);
        ctx.fillStyle = `rgba(212, 160, 76, ${Math.max(0.05, alpha)})`;
        ctx.fill();

        // Glow
        ctx.beginPath();
        ctx.arc(p.x, p.y, p.r * 3, 0, Math.PI * 2);
        ctx.fillStyle = `rgba(212, 160, 76, ${Math.max(0.01, alpha * 0.3)})`;
        ctx.fill();
      });

      this.animationId = requestAnimationFrame(() => this.animateParticles());
    },

    animateIcon() {
      // The CSS handles the entrance animation
      // This is for any additional JS-driven effects
    },

    waitForReady() {
      // For static site: dismiss after short delay
      const dismiss = () => {
        if (this.dismissed) return;
        this.dismissed = true;

        if (this.animationId) cancelAnimationFrame(this.animationId);

        // Exit animation
        this.overlay.style.transition = 'opacity 0.6s cubic-bezier(0.4, 0, 0.2, 1), visibility 0.6s';
        this.overlay.style.opacity = '0';
        this.overlay.style.visibility = 'hidden';

        // Remove after animation
        setTimeout(() => {
          if (this.overlay.parentNode) {
            this.overlay.parentNode.removeChild(this.overlay);
          }
        }, 700);
      };

      // Dismiss when page is fully loaded
      if (document.readyState === 'complete') {
        setTimeout(dismiss, 1800);
      } else {
        window.addEventListener('load', () => {
          setTimeout(dismiss, 1200);
        });
      }

      // Export dismiss for Blazor
      window.dismissSplash = dismiss;
    }
  };

  // Start when DOM is ready
  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', () => Splash.init());
  } else {
    Splash.init();
  }

})();
