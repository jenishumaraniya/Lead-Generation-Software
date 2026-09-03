import { Component, OnInit, AfterViewInit, OnDestroy, ElementRef, ViewChild, HostListener } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { ApiService } from '../../core/services/api.service';

interface Particle {
  x: number;
  y: number;
  vx: number;
  vy: number;
  baseVx: number;
  baseVy: number;
  radius: number;
  alpha: number;
  baseHue: number;
}

interface BurstParticle {
  x: number;
  y: number;
  vx: number;
  vy: number;
  radius: number;
  alpha: number;
  hue: number;
  life: number;
}

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [RouterLink, CommonModule],
  templateUrl: './home.component.html',
  styleUrl: './home.component.css'
})
export class HomeComponent implements OnInit, AfterViewInit, OnDestroy {
  @ViewChild('particleCanvas') canvasRef!: ElementRef<HTMLCanvasElement>;

  categories: any[] = [];
  private animationFrameId: number | null = null;
  private ctx: CanvasRenderingContext2D | null = null;
  private particles: Particle[] = [];
  private burstParticles: BurstParticle[] = [];
  private mouse = { x: -1000, y: -1000, isHovering: false };
  private colorCycle = 0;

  constructor(
    private router: Router,
    private apiService: ApiService
  ) {}

  ngOnInit(): void {
    this.apiService.getCategories().subscribe({
      next: (cats) => this.categories = cats,
      error: (err) => console.error('Failed to load categories on home:', err)
    });
  }

  ngAfterViewInit(): void {
    this.initCanvas();
  }

  ngOnDestroy(): void {
    if (this.animationFrameId) {
      cancelAnimationFrame(this.animationFrameId);
    }
  }

  @HostListener('window:resize')
  onResize(): void {
    this.resizeCanvas();
  }

  @HostListener('mousemove', ['$event'])
  onMouseMove(event: MouseEvent): void {
    const canvas = this.canvasRef?.nativeElement;
    if (!canvas) return;
    const rect = canvas.getBoundingClientRect();
    if (event.clientY >= rect.top && event.clientY <= rect.bottom &&
        event.clientX >= rect.left && event.clientX <= rect.right) {
      this.mouse.x = event.clientX - rect.left;
      this.mouse.y = event.clientY - rect.top;
      this.mouse.isHovering = true;
    } else {
      this.mouse.isHovering = false;
    }
  }

  @HostListener('click', ['$event'])
  onClick(event: MouseEvent): void {
    this.triggerParticleBurst(event.clientX, event.clientY);
  }

  @HostListener('touchstart', ['$event'])
  onTouchStart(event: TouchEvent): void {
    if (event.touches && event.touches.length > 0) {
      const t = event.touches[0];
      this.triggerParticleBurst(t.clientX, t.clientY);
    }
  }

  private triggerParticleBurst(clientX: number, clientY: number): void {
    const canvas = this.canvasRef?.nativeElement;
    if (!canvas) return;
    const rect = canvas.getBoundingClientRect();
    const clickX = clientY >= rect.top && clientY <= rect.bottom ? clientX - rect.left : -1000;
    const clickY = clientY >= rect.top && clientY <= rect.bottom ? clientY - rect.top : -1000;

    if (clickX < 0 || clickY < 0) return;

    // 1. Repel existing particles outwards in a powerful shockwave
    for (const p of this.particles) {
      const dx = p.x - clickX;
      const dy = p.y - clickY;
      const dist = Math.sqrt(dx * dx + dy * dy);
      if (dist < 260 && dist > 0) {
        const force = ((260 - dist) / 260) * 11;
        const angle = Math.atan2(dy, dx);
        p.vx += Math.cos(angle) * force;
        p.vy += Math.sin(angle) * force;
      }
    }

    // 2. Spawn 30 glowing burst micro-particles radiating outwards from tap point
    for (let i = 0; i < 30; i++) {
      const angle = Math.random() * Math.PI * 2;
      const speed = Math.random() * 7 + 2;
      this.burstParticles.push({
        x: clickX,
        y: clickY,
        vx: Math.cos(angle) * speed,
        vy: Math.sin(angle) * speed,
        radius: Math.random() * 2.5 + 1.2,
        alpha: 1.0,
        hue: 200 + Math.random() * 80, // Electric blue to bright magenta
        life: 1.0
      });
    }
  }

  private initCanvas(): void {
    const canvas = this.canvasRef?.nativeElement;
    if (!canvas) return;
    this.ctx = canvas.getContext('2d');
    if (!this.ctx) return;

    this.resizeCanvas();
    this.createParticles();
    this.animate();
  }

  private resizeCanvas(): void {
    const canvas = this.canvasRef?.nativeElement;
    if (!canvas) return;
    const parent = canvas.parentElement;
    const width = parent ? parent.clientWidth : window.innerWidth;
    const height = parent ? parent.clientHeight : 600;

    const dpr = Math.min(window.devicePixelRatio || 1, 2);
    canvas.width = width * dpr;
    canvas.height = height * dpr;
    canvas.style.width = `${width}px`;
    canvas.style.height = `${height}px`;

    if (this.ctx) {
      this.ctx.scale(dpr, dpr);
    }
  }

  private createParticles(): void {
    const canvas = this.canvasRef?.nativeElement;
    if (!canvas) return;
    const width = canvas.width / Math.min(window.devicePixelRatio || 1, 2);
    const height = canvas.height / Math.min(window.devicePixelRatio || 1, 2);

    const particleCount = Math.floor(Math.min(width, 1400) * 0.055);
    this.particles = [];

    for (let i = 0; i < particleCount; i++) {
      const bVx = (Math.random() - 0.5) * 0.4;
      const bVy = (Math.random() - 0.5) * 0.4;
      this.particles.push({
        x: Math.random() * width,
        y: Math.random() * height,
        vx: bVx,
        vy: bVy,
        baseVx: bVx,
        baseVy: bVy,
        radius: Math.random() * 2 + 1,
        alpha: Math.random() * 0.5 + 0.4,
        baseHue: 210 + Math.random() * 60
      });
    }
  }

  private animate = (): void => {
    const canvas = this.canvasRef?.nativeElement;
    if (!canvas || !this.ctx) return;

    const dpr = Math.min(window.devicePixelRatio || 1, 2);
    const width = canvas.width / dpr;
    const height = canvas.height / dpr;

    this.ctx.clearRect(0, 0, width, height);
    this.colorCycle += 0.004;

    const currentHueShift = Math.sin(this.colorCycle) * 25;

    // Update and render main particles
    for (let i = 0; i < this.particles.length; i++) {
      const p = this.particles[i];

      // Smooth velocity decay after shockwave burst back to normal velocity
      p.vx = p.vx * 0.95 + p.baseVx * 0.05;
      p.vy = p.vy * 0.95 + p.baseVy * 0.05;

      // Mouse repulsion while hovering
      if (this.mouse.isHovering) {
        const mDx = p.x - this.mouse.x;
        const mDy = p.y - this.mouse.y;
        const mDist = Math.sqrt(mDx * mDx + mDy * mDy);
        if (mDist < 140 && mDist > 0) {
          const mForce = (140 - mDist) / 140;
          const mAngle = Math.atan2(mDy, mDx);
          p.x += Math.cos(mAngle) * mForce * 3.5;
          p.y += Math.sin(mAngle) * mForce * 3.5;
        }
      }

      p.x += p.vx;
      p.y += p.vy;

      // Screen edge bounce
      if (p.x < 0 || p.x > width) p.vx *= -1;
      if (p.y < 0 || p.y > height) p.vy *= -1;

      const dynamicHue = (p.baseHue + currentHueShift + 360) % 360;

      // Draw particle dot
      this.ctx.beginPath();
      this.ctx.arc(p.x, p.y, p.radius, 0, Math.PI * 2);
      this.ctx.fillStyle = `hsla(${dynamicHue}, 85%, 65%, ${p.alpha})`;
      this.ctx.shadowBlur = 10;
      this.ctx.shadowColor = `hsla(${dynamicHue}, 90%, 70%, 0.7)`;
      this.ctx.fill();

      // Connect mouse cursor to nearby particles
      if (this.mouse.isHovering) {
        const mDx = p.x - this.mouse.x;
        const mDy = p.y - this.mouse.y;
        const mDist = Math.sqrt(mDx * mDx + mDy * mDy);
        if (mDist < 160) {
          const lineAlpha = (1 - mDist / 160) * 0.45;
          this.ctx.beginPath();
          this.ctx.moveTo(p.x, p.y);
          this.ctx.lineTo(this.mouse.x, this.mouse.y);
          this.ctx.strokeStyle = `hsla(${dynamicHue}, 90%, 75%, ${lineAlpha})`;
          this.ctx.lineWidth = 0.9;
          this.ctx.stroke();
        }
      }

      // Connect particles with luminous lines
      for (let j = i + 1; j < this.particles.length; j++) {
        const p2 = this.particles[j];
        const dx = p.x - p2.x;
        const dy = p.y - p2.y;
        const dist = Math.sqrt(dx * dx + dy * dy);
        const maxDist = 125;

        if (dist < maxDist) {
          const lineAlpha = (1 - dist / maxDist) * 0.3;
          const lineHue = (dynamicHue + p2.baseHue) / 2;

          this.ctx.beginPath();
          this.ctx.moveTo(p.x, p.y);
          this.ctx.lineTo(p2.x, p2.y);
          this.ctx.strokeStyle = `hsla(${lineHue}, 85%, 65%, ${lineAlpha})`;
          this.ctx.lineWidth = 0.7;
          this.ctx.shadowBlur = 3;
          this.ctx.shadowColor = `hsla(${lineHue}, 90%, 70%, 0.4)`;
          this.ctx.stroke();
        }
      }
    }

    // Render sparkling burst particles on click/tap
    for (let b = this.burstParticles.length - 1; b >= 0; b--) {
      const bp = this.burstParticles[b];
      bp.x += bp.vx;
      bp.y += bp.vy;
      bp.vx *= 0.94;
      bp.vy *= 0.94;
      bp.life -= 0.025;
      bp.alpha = bp.life;

      if (bp.life <= 0) {
        this.burstParticles.splice(b, 1);
        continue;
      }

      this.ctx.beginPath();
      this.ctx.arc(bp.x, bp.y, bp.radius, 0, Math.PI * 2);
      this.ctx.fillStyle = `hsla(${bp.hue}, 95%, 75%, ${bp.alpha})`;
      this.ctx.shadowBlur = 12;
      this.ctx.shadowColor = `hsla(${bp.hue}, 100%, 80%, ${bp.alpha})`;
      this.ctx.fill();
    }

    this.animationFrameId = requestAnimationFrame(this.animate);
  };

  exploreProducts(categoryId?: number): void {
    if (categoryId) {
      this.router.navigate(['/products'], { queryParams: { categoryId } });
    } else {
      this.router.navigate(['/products']);
    }
  }

  getCategoryType(name: string): string {
    const n = (name || '').toLowerCase();
    if (n.includes('laptop')) return 'laptop';
    if (n.includes('desktop')) return 'desktop';
    if (n.includes('server')) return 'server';
    if (n.includes('network')) return 'network';
    if (n.includes('cloud')) return 'cloud';
    return 'default';
  }
}