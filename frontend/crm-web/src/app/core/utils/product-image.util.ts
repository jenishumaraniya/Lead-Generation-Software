/**
 * Generates self-contained, high-resolution SVG product artwork.
 * 100% local, zero external network dependency, guaranteed to render on all browsers/networks.
 */
function createSvgDataUri(bgGradient: [string, string], title: string, subtitle: string, iconSvg: string): string {
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 600 400" width="600" height="400">
    <defs>
      <linearGradient id="bg" x1="0%" y1="0%" x2="100%" y2="100%">
        <stop offset="0%" stop-color="${bgGradient[0]}" />
        <stop offset="100%" stop-color="${bgGradient[1]}" />
      </linearGradient>
      <filter id="shadow" x="-10%" y="-10%" width="120%" height="120%">
        <feDropShadow dx="0" dy="12" stdDeviation="16" flood-color="#0f172a" flood-opacity="0.22" />
      </filter>
    </defs>
    
    <!-- Background Card -->
    <rect width="600" height="400" fill="url(#bg)" />
    
    <!-- Decorative Grid Elements -->
    <g opacity="0.1" stroke="#ffffff" stroke-width="1.5">
      <line x1="0" y1="100" x2="600" y2="100" />
      <line x1="0" y1="200" x2="600" y2="200" />
      <line x1="0" y1="300" x2="600" y2="300" />
      <line x1="150" y1="0" x2="150" y2="400" />
      <line x1="300" y1="0" x2="300" y2="400" />
      <line x1="450" y1="0" x2="450" y2="400" />
    </g>

    <!-- Main Visual Container -->
    <g transform="translate(300, 160)" filter="url(#shadow)">
      ${iconSvg}
    </g>

    <!-- Badge & Typography -->
    <g transform="translate(300, 320)" text-anchor="middle">
      <rect x="-130" y="-22" width="260" height="32" rx="16" fill="#ffffff" fill-opacity="0.95" />
      <text x="0" y="-1" font-family="-apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif" font-size="13" font-weight="800" fill="#0f172a" letter-spacing="0.8">${title.toUpperCase()}</text>
      <text x="0" y="38" font-family="-apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif" font-size="13" font-weight="600" fill="#ffffff" fill-opacity="0.95">${subtitle}</text>
    </g>
  </svg>`;

  return 'data:image/svg+xml;utf8,' + encodeURIComponent(svg);
}

export function getProductImageUrl(product?: { name?: string; categoryName?: string; categoryId?: number; description?: string } | null): string {
  if (!product) {
    return createSvgDataUri(['#1e293b', '#0f172a'], 'Enterprise Tech', 'Commercial Hardware', `
      <rect x="-60" y="-45" width="120" height="90" rx="10" fill="#3b82f6" />
      <circle cx="0" cy="0" r="24" fill="#ffffff" fill-opacity="0.8" />
    `);
  }

  const name = (product.name || '').toLowerCase();
  const desc = (product.description || '').toLowerCase();
  const cat = (product.categoryName || '').toLowerCase();
  const combined = `${name} ${desc} ${cat}`;
  const catId = product.categoryId;

  // 1. LAPTOPS / NOTEBOOKS / GAMING LAPTOP / MACBOOK
  // Check LAPTOP FIRST so "Gaming Laptop" or "Business Laptop" is never mistaken for desktop
  if (combined.includes('laptop') || combined.includes('notebook') || combined.includes('macbook') || catId === 2) {
    return createSvgDataUri(['#065f46', '#10b981'], 'Laptop Series', 'Portable High Performance', `
      <!-- Laptop Screen Open -->
      <rect x="-90" y="-80" width="180" height="112" rx="8" fill="#ffffff" />
      <rect x="-82" y="-72" width="164" height="96" rx="4" fill="#0f172a" />
      <!-- Screen Graphic -->
      <circle cx="0" cy="-24" r="24" fill="#10b981" fill-opacity="0.25" />
      <polygon points="0,-42 20,-18 -20,-18" fill="#34d399" />
      <!-- Base & Keyboard -->
      <path d="M-120 32 L120 32 L100 52 L-100 52 Z" fill="#e2e8f0" />
      <rect x="-60" y="35" width="120" height="9" rx="2" fill="#94a3b8" />
      <rect x="-28" y="45" width="56" height="4" rx="1" fill="#64748b" />
    `);
  }

  // 2. SERVERS / DATA CENTER / RACK / ENTERPRISE SERVER
  if (combined.includes('server') || combined.includes('datacenter') || combined.includes('rack') || catId === 1 || catId === 4) {
    return createSvgDataUri(['#4c1d95', '#8b5cf6'], 'Enterprise Server', 'Mission-Critical Architecture', `
      <!-- Server Rack Chassis -->
      <rect x="-95" y="-75" width="190" height="40" rx="6" fill="#1e293b" stroke="#a78bfa" stroke-width="2" />
      <rect x="-95" y="-25" width="190" height="40" rx="6" fill="#1e293b" stroke="#a78bfa" stroke-width="2" />
      <rect x="-95" y="25" width="190" height="40" rx="6" fill="#1e293b" stroke="#a78bfa" stroke-width="2" />
      <!-- Drive Bays & LEDs -->
      <circle cx="-75" cy="-55" r="4.5" fill="#34d399" />
      <circle cx="-58" cy="-55" r="4.5" fill="#38bdf8" />
      <circle cx="-75" cy="-5" r="4.5" fill="#34d399" />
      <circle cx="-58" cy="-5" r="4.5" fill="#38bdf8" />
      <circle cx="-75" cy="45" r="4.5" fill="#34d399" />
      <circle cx="-58" cy="45" r="4.5" fill="#38bdf8" />
      <!-- Ventilation Grilles -->
      <line x1="-25" y1="-55" x2="75" y2="-55" stroke="#64748b" stroke-width="6" stroke-dasharray="8,4" />
      <line x1="-25" y1="-5" x2="75" y2="-5" stroke="#64748b" stroke-width="6" stroke-dasharray="8,4" />
      <line x1="-25" y1="45" x2="75" y2="45" stroke="#64748b" stroke-width="6" stroke-dasharray="8,4" />
    `);
  }

  // 3. DESKTOPS / WORKSTATIONS / PC / ALL-IN-ONE
  if (combined.includes('desktop') || combined.includes('workstation') || combined.includes('pc') || catId === 3) {
    return createSvgDataUri(['#1e3a8a', '#3b82f6'], 'Desktop Workstation', 'Productivity & Office Computing', `
      <!-- Monitor Screen -->
      <rect x="-95" y="-80" width="190" height="120" rx="8" fill="#ffffff" />
      <rect x="-87" y="-72" width="174" height="100" rx="4" fill="#0f172a" />
      <path d="M-60 -20 L-20 -40 L20 -15 L60 -45" stroke="#38bdf8" stroke-width="3.5" fill="none" />
      <!-- Stand -->
      <path d="M-15 40 L-25 65 L25 65 L15 40 Z" fill="#cbd5e1" />
      <!-- PC Tower Beside -->
      <rect x="110" y="-65" width="48" height="130" rx="6" fill="#334155" />
      <circle cx="134" cy="-45" r="4.5" fill="#38bdf8" />
      <rect x="120" y="-25" width="28" height="4" rx="2" fill="#64748b" />
      <rect x="120" y="-15" width="28" height="4" rx="2" fill="#64748b" />
    `);
  }

  // 4. NETWORKING / ROUTER / SWITCH / GATEWAY
  if (combined.includes('router') || combined.includes('network') || combined.includes('switch') || combined.includes('gateway') || catId === 5) {
    return createSvgDataUri(['#9a3412', '#f97316'], 'Network Router', 'High-Speed Infrastructure', `
      <!-- Antennas -->
      <line x1="-65" y1="-75" x2="-65" y2="-10" stroke="#cbd5e1" stroke-width="4" stroke-linecap="round" />
      <line x1="-22" y1="-90" x2="-22" y2="-10" stroke="#cbd5e1" stroke-width="4" stroke-linecap="round" />
      <line x1="22" y1="-90" x2="22" y2="-10" stroke="#cbd5e1" stroke-width="4" stroke-linecap="round" />
      <line x1="65" y1="-75" x2="65" y2="-10" stroke="#cbd5e1" stroke-width="4" stroke-linecap="round" />
      <!-- Router Body -->
      <rect x="-100" y="-10" width="200" height="62" rx="10" fill="#1e293b" />
      <!-- Ethernet Ports / Lights -->
      <circle cx="-75" cy="21" r="5" fill="#22c55e" />
      <circle cx="-55" cy="21" r="5" fill="#22c55e" />
      <circle cx="-35" cy="21" r="5" fill="#22c55e" />
      <circle cx="-15" cy="21" r="5" fill="#38bdf8" />
      <rect x="15" y="10" width="70" height="22" rx="4" fill="#0f172a" />
      <text x="50" y="26" font-family="monospace" font-size="11" font-weight="700" fill="#38bdf8" text-anchor="middle">10 Gbps</text>
    `);
  }

  // 5. CLOUD / CLUSTER / STORAGE
  if (combined.includes('cloud') || combined.includes('cluster') || combined.includes('storage') || catId === 6 || catId === 10 || catId === 12 || catId === 16 || catId === 18) {
    return createSvgDataUri(['#0369a1', '#0284c7'], 'Cloud Architecture', 'Elastic Cluster Computing', `
      <!-- Cloud Shape -->
      <path d="M-55 20 A38 38 0 0 1 -32 -38 A48 48 0 0 1 38 -38 A38 38 0 0 1 60 20 Z" fill="#ffffff" fill-opacity="0.95" />
      <!-- Synchronized Nodes -->
      <circle cx="-25" cy="0" r="9" fill="#0284c7" />
      <circle cx="25" cy="0" r="9" fill="#0284c7" />
      <line x1="-25" y1="0" x2="25" y2="0" stroke="#0284c7" stroke-width="3" stroke-dasharray="4,4" />
    `);
  }

  // 6. SECURITY / THREAT DETECTION / ZERO TRUST / IDENTITY
  if (combined.includes('security') || combined.includes('threat') || combined.includes('zerotrust') || combined.includes('identity') || combined.includes('cyber') || catId === 7 || catId === 9 || catId === 11 || catId === 13 || catId === 17 || catId === 19) {
    return createSvgDataUri(['#831843', '#db2777'], 'ZeroTrust Security', 'Threat Detection & Defense', `
      <!-- Shield -->
      <path d="M0 -80 L70 -48 L70 18 C70 60 0 85 0 85 C0 85 -70 60 -70 18 L-70 -48 Z" fill="#ffffff" fill-opacity="0.95" />
      <!-- Lock Inside -->
      <rect x="-22" y="-10" width="44" height="34" rx="5" fill="#db2777" />
      <path d="M-13 -10 L-13 -24 C-13 -32 13 -32 13 -24 L13 -10" fill="none" stroke="#db2777" stroke-width="5.5" stroke-linecap="round" />
      <circle cx="0" cy="7" r="4.5" fill="#ffffff" />
    `);
  }

  // 7. SOFTWARE / ERP / CRM
  if (combined.includes('software') || combined.includes('erp') || combined.includes('crm') || combined.includes('suite')) {
    return createSvgDataUri(['#115e59', '#14b8a6'], 'Enterprise Suite', 'Integrated Cloud Workflow', `
      <!-- App Window -->
      <rect x="-90" y="-75" width="180" height="125" rx="10" fill="#ffffff" />
      <rect x="-90" y="-75" width="180" height="24" rx="10" fill="#0f172a" />
      <circle cx="-72" cy="-63" r="3.5" fill="#ef4444" />
      <circle cx="-60" cy="-63" r="3.5" fill="#f59e0b" />
      <circle cx="-48" cy="-63" r="3.5" fill="#10b981" />
      <!-- Bar Charts -->
      <rect x="-65" y="-10" width="24" height="48" rx="3" fill="#14b8a6" />
      <rect x="-28" y="-32" width="24" height="70" rx="3" fill="#0d9488" />
      <rect x="9" y="-20" width="24" height="58" rx="3" fill="#14b8a6" />
      <rect x="46" y="-42" width="24" height="80" rx="3" fill="#0f766e" />
    `);
  }

  // Default Technology Solution
  return createSvgDataUri(['#1e293b', '#334155'], 'LeadFlow Enterprise', 'Commercial Hardware', `
    <rect x="-70" y="-55" width="140" height="105" rx="12" fill="#ffffff" fill-opacity="0.95" />
    <circle cx="0" cy="-5" r="30" fill="#3b82f6" />
    <polygon points="0,-22 16,6 -16,6" fill="#ffffff" />
  `);
}
