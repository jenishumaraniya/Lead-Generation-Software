import { Product } from '../models/product.model';

export const PRODUCTS: Product[] = [
  {
    productId: 1,
    name: 'Business Laptop Pro 14',
    category: 'Laptop',
    description:
      'Professional laptop designed for business users, office applications and everyday productivity.',
    pricing: 65000,
    features: [
      'Intel Core i5 Processor',
      '16GB RAM',
      '512GB SSD',
      '14-inch Full HD Display',
      'Wi-Fi 6',
      'Fingerprint Security'
    ],
    specifications: [
      'Processor: Intel Core i5 13th Gen',
      'RAM: 16GB DDR4',
      'Storage: 512GB NVMe SSD',
      'Display: 14-inch Full HD',
      'Operating System: Windows 11 Pro'
    ],
    status: 'ACTIVE'
  },

  {
    productId: 2,
    name: 'Business Laptop Ultra 15',
    category: 'Laptop',
    description:
      'High-performance business laptop for professionals handling demanding workloads.',
    pricing: 85000,
    features: [
      'Intel Core i7 Processor',
      '16GB RAM',
      '1TB SSD',
      '15.6-inch Full HD Display',
      'Wi-Fi 6E',
      'Backlit Keyboard'
    ],
    specifications: [
      'Processor: Intel Core i7 13th Gen',
      'RAM: 16GB DDR5',
      'Storage: 1TB NVMe SSD',
      'Display: 15.6-inch Full HD',
      'Operating System: Windows 11 Pro'
    ],
    status: 'ACTIVE'
  },

  {
    productId: 3,
    name: 'Business Laptop Elite',
    category: 'Laptop',
    description:
      'Premium business laptop for executives and professionals who need maximum performance.',
    pricing: 125000,
    features: [
      'Intel Core i7 Processor',
      '32GB RAM',
      '1TB SSD',
      '14-inch 2.5K Display',
      'Wi-Fi 6E',
      'Advanced Security'
    ],
    specifications: [
      'Processor: Intel Core i7 14th Gen',
      'RAM: 32GB DDR5',
      'Storage: 1TB NVMe SSD',
      'Display: 14-inch 2.5K',
      'Operating System: Windows 11 Pro'
    ],
    status: 'ACTIVE'
  },

  {
    productId: 4,
    name: 'Business Desktop Pro',
    category: 'Desktop',
    description:
      'Reliable desktop computer designed for office productivity and business applications.',
    pricing: 55000,
    features: [
      'Intel Core i5 Processor',
      '16GB RAM',
      '512GB SSD',
      'Multiple USB Ports',
      'Windows 11 Pro',
      'Business Security'
    ],
    specifications: [
      'Processor: Intel Core i5 13th Gen',
      'RAM: 16GB DDR4',
      'Storage: 512GB SSD',
      'Graphics: Integrated Graphics',
      'Operating System: Windows 11 Pro'
    ],
    status: 'ACTIVE'
  },

  {
    productId: 5,
    name: 'Business Desktop Performance',
    category: 'Desktop',
    description:
      'High-performance desktop for developers, designers and business professionals.',
    pricing: 80000,
    features: [
      'Intel Core i7 Processor',
      '32GB RAM',
      '1TB SSD',
      'Dedicated Graphics',
      'Multiple Display Support',
      'Windows 11 Pro'
    ],
    specifications: [
      'Processor: Intel Core i7 13th Gen',
      'RAM: 32GB DDR5',
      'Storage: 1TB NVMe SSD',
      'Graphics: 6GB Dedicated GPU',
      'Operating System: Windows 11 Pro'
    ],
    status: 'ACTIVE'
  },

  {
    productId: 6,
    name: 'Business Workstation X1',
    category: 'Desktop',
    description:
      'Professional workstation designed for engineering, design and data-intensive workloads.',
    pricing: 145000,
    features: [
      'Intel Core i9 Processor',
      '64GB RAM',
      '2TB SSD',
      'Professional Graphics',
      'Advanced Cooling',
      'Windows 11 Pro'
    ],
    specifications: [
      'Processor: Intel Core i9',
      'RAM: 64GB DDR5',
      'Storage: 2TB NVMe SSD',
      'Graphics: Professional GPU',
      'Operating System: Windows 11 Pro'
    ],
    status: 'ACTIVE'
  },

  {
    productId: 7,
    name: 'Business Server X1',
    category: 'Server',
    description:
      'Reliable entry-level server designed for small and medium-sized businesses.',
    pricing: 150000,
    features: [
      'Intel Xeon Processor',
      '64GB RAM',
      '2TB Storage',
      'RAID Support',
      'Remote Management',
      'Enterprise Security'
    ],
    specifications: [
      'Processor: Intel Xeon',
      'RAM: 64GB ECC',
      'Storage: 2TB SSD',
      'RAID: RAID 1',
      'Form Factor: Rack Mount'
    ],
    status: 'ACTIVE'
  },

  {
    productId: 8,
    name: 'Enterprise Server Pro',
    category: 'Server',
    description:
      'High-performance enterprise server for databases, applications and virtualization.',
    pricing: 280000,
    features: [
      'Dual Xeon Processor',
      '128GB RAM',
      '4TB SSD',
      'RAID 10',
      'Hot-Swap Drives',
      'Remote Management'
    ],
    specifications: [
      'Processor: Dual Intel Xeon',
      'RAM: 128GB ECC',
      'Storage: 4TB SSD',
      'RAID: RAID 10',
      'Form Factor: 2U Rack'
    ],
    status: 'ACTIVE'
  },

  {
    productId: 9,
    name: 'Enterprise Server Ultra',
    category: 'Server',
    description:
      'Enterprise-grade server designed for demanding workloads and high availability.',
    pricing: 450000,
    features: [
      'Dual High-Performance Xeon',
      '256GB RAM',
      '8TB Storage',
      'Advanced RAID',
      'High Availability',
      'Enterprise Management'
    ],
    specifications: [
      'Processor: Dual Intel Xeon',
      'RAM: 256GB ECC',
      'Storage: 8TB SSD',
      'RAID: RAID 10',
      'Form Factor: 2U Rack'
    ],
    status: 'ACTIVE'
  },

  {
    productId: 10,
    name: 'Business Network Router',
    category: 'Networking',
    description:
      'Secure business router designed for small and medium-sized office networks.',
    pricing: 35000,
    features: [
      '1Gbps Network Speed',
      'VPN Support',
      'Firewall',
      '8 Ethernet Ports',
      'Remote Management',
      'Traffic Monitoring'
    ],
    specifications: [
      'Speed: Up to 1Gbps',
      'Ports: 8 Ethernet',
      'VPN: IPSec / SSL',
      'Security: Enterprise Firewall',
      'Management: Web Dashboard'
    ],
    status: 'ACTIVE'
  },

  {
    productId: 11,
    name: 'Enterprise Network Switch',
    category: 'Networking',
    description:
      'Managed network switch designed for reliable enterprise connectivity.',
    pricing: 60000,
    features: [
      '24 Gigabit Ports',
      'Managed Switch',
      'VLAN Support',
      'QoS',
      'Network Monitoring',
      'Enterprise Security'
    ],
    specifications: [
      'Ports: 24 Gigabit Ethernet',
      'Uplink: 10Gb',
      'VLAN: Supported',
      'Management: Web / CLI',
      'PoE: Supported'
    ],
    status: 'ACTIVE'
  },

  {
    productId: 12,
    name: 'Enterprise Wi-Fi Access Point',
    category: 'Networking',
    description:
      'High-performance wireless access point for offices and enterprise environments.',
    pricing: 22000,
    features: [
      'Wi-Fi 6',
      'High-Speed Wireless',
      'Multiple SSIDs',
      'Guest Network',
      'Central Management',
      'Enterprise Security'
    ],
    specifications: [
      'Standard: Wi-Fi 6',
      'Speed: Up to 1.8Gbps',
      'Bands: Dual Band',
      'Security: WPA3',
      'Power: PoE'
    ],
    status: 'ACTIVE'
  },

  {
    productId: 13,
    name: 'Business Productivity Suite',
    category: 'Software',
    description:
      'Business productivity software designed to help teams collaborate and manage daily work.',
    pricing: 12000,
    features: [
      'Document Management',
      'Team Collaboration',
      'Task Management',
      'Cloud Storage',
      'User Management',
      'Reporting'
    ],
    specifications: [
      'Deployment: Cloud',
      'Users: Up to 100',
      'Storage: 1TB',
      'Authentication: Email / SSO',
      'Updates: Automatic'
    ],
    status: 'ACTIVE'
  },

  {
    productId: 14,
    name: 'Business Security Suite',
    category: 'Software',
    description:
      'Business security solution designed to protect devices and company data.',
    pricing: 18000,
    features: [
      'Endpoint Protection',
      'Threat Detection',
      'Firewall',
      'Security Monitoring',
      'Central Management',
      'Reports'
    ],
    specifications: [
      'Deployment: Cloud',
      'Devices: Up to 100',
      'Protection: Endpoint',
      'Management: Central Console',
      'Updates: Automatic'
    ],
    status: 'ACTIVE'
  },

  {
    productId: 15,
    name: 'Enterprise Business Platform',
    category: 'Software',
    description:
      'Enterprise software platform for managing business operations and workflows.',
    pricing: 45000,
    features: [
      'Workflow Management',
      'User Management',
      'Analytics',
      'API Integration',
      'Role-Based Access',
      'Advanced Reporting'
    ],
    specifications: [
      'Deployment: Cloud / On-Premise',
      'Users: Unlimited',
      'API: REST API',
      'Authentication: SSO',
      'Security: Role-Based Access'
    ],
    status: 'ACTIVE'
  }
];