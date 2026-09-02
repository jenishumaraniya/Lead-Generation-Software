export interface CountryCodeItem {
  name: string;
  code: string;
  dialCode: string;
  flag: string;
  patternLength: number;
}

export const COUNTRY_DATA: CountryCodeItem[] = [
  { name: 'India', code: 'IN', dialCode: '+91', flag: '🇮🇳', patternLength: 10 },
  { name: 'United States', code: 'US', dialCode: '+1', flag: '🇺🇸', patternLength: 10 },
  { name: 'United Kingdom', code: 'GB', dialCode: '+44', flag: '🇬🇧', patternLength: 10 },
  { name: 'Canada', code: 'CA', dialCode: '+1', flag: '🇨🇦', patternLength: 10 },
  { name: 'Australia', code: 'AU', dialCode: '+61', flag: '🇦🇺', patternLength: 9 },
  { name: 'Germany', code: 'DE', dialCode: '+49', flag: '🇩🇪', patternLength: 10 },
  { name: 'France', code: 'FR', dialCode: '+33', flag: '🇫🇷', patternLength: 9 },
  { name: 'United Arab Emirates', code: 'AE', dialCode: '+971', flag: '🇦🇪', patternLength: 9 },
  { name: 'Singapore', code: 'SG', dialCode: '+65', flag: '🇸🇬', patternLength: 8 },
  { name: 'Japan', code: 'JP', dialCode: '+81', flag: '🇯🇵', patternLength: 10 },
  { name: 'Saudi Arabia', code: 'SA', dialCode: '+966', flag: '🇸🇦', patternLength: 9 },
  { name: 'South Africa', code: 'ZA', dialCode: '+27', flag: '🇿🇦', patternLength: 9 },
  { name: 'Brazil', code: 'BR', dialCode: '+55', flag: '🇧🇷', patternLength: 11 },
  { name: 'Netherlands', code: 'NL', dialCode: '+31', flag: '🇳🇱', patternLength: 9 },
  { name: 'Switzerland', code: 'CH', dialCode: '+41', flag: '🇨🇭', patternLength: 9 },
  { name: 'New Zealand', code: 'NZ', dialCode: '+64', flag: '🇳🇿', patternLength: 9 },
  { name: 'Ireland', code: 'IE', dialCode: '+353', flag: '🇮🇪', patternLength: 9 },
  { name: 'Italy', code: 'IT', dialCode: '+39', flag: '🇮🇹', patternLength: 10 },
  { name: 'Spain', code: 'ES', dialCode: '+34', flag: '🇪🇸', patternLength: 9 },
  { name: 'Sweden', code: 'SE', dialCode: '+46', flag: '🇸🇪', patternLength: 9 },
  { name: 'Mexico', code: 'MX', dialCode: '+52', flag: '🇲🇽', patternLength: 10 },
  { name: 'Other / International', code: 'OTHER', dialCode: '+', flag: '🌐', patternLength: 10 }
];
