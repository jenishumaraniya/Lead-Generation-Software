// Content Script: Multi-Layer LinkedIn Profile Extractor
(function () {
  function getCleanText(el) {
    if (!el) return '';
    return el.innerText ? el.innerText.trim().replace(/\s+/g, ' ') : '';
  }

  function parseJsonLd() {
    const scripts = document.querySelectorAll('script[type="application/ld+json"]');
    for (const script of scripts) {
      try {
        const json = JSON.parse(script.textContent || '{}');
        if (json['@type'] === 'Person' || json['@type'] === 'ProfilePage') {
          const person = json['@type'] === 'Person' ? json : (json.mainEntity || {});
          if (person.name || person.jobTitle) {
            return {
              name: person.name || '',
              jobTitle: person.jobTitle || '',
              company: person.worksFor?.name || (typeof person.worksFor === 'string' ? person.worksFor : ''),
              location: person.address?.addressLocality || person.address?.addressRegion || person.address?.addressCountry || '',
              description: person.description || ''
            };
          }
        }
      } catch (e) {
        // Continue to next script
      }
    }
    return null;
  }

  function parseMetaTags() {
    const ogTitle = document.querySelector('meta[property="og:title"]')?.content ||
                    document.querySelector('meta[name="twitter:title"]')?.content || '';
    const description = document.querySelector('meta[property="og:description"]')?.content ||
                        document.querySelector('meta[name="description"]')?.content || '';
    const author = document.querySelector('meta[name="author"]')?.content || '';

    let parsedName = author;
    let parsedTitle = '';
    let parsedCompany = '';

    if (ogTitle) {
      // e.g. "Jensen Huang - Founder and CEO - NVIDIA | LinkedIn"
      // or "Jensen Huang - CEO @ NVIDIA"
      const cleanTitle = ogTitle.replace(/\s*\|\s*LinkedIn\s*$/i, '').trim();
      const parts = cleanTitle.split(/\s+[-–—]\s+/);
      if (parts.length >= 1 && !parsedName) parsedName = parts[0].trim();
      if (parts.length >= 2) parsedTitle = parts[1].trim();
      if (parts.length >= 3) parsedCompany = parts[2].trim();
    }

    return {
      name: parsedName,
      headline: parsedTitle,
      company: parsedCompany,
      description: description
    };
  }

  function parseDocTitle() {
    const rawTitle = document.title || '';
    // e.g. "Jensen Huang - Founder and CEO - NVIDIA | LinkedIn" or "Jensen Huang | LinkedIn"
    const cleaned = rawTitle.replace(/\s*\|\s*LinkedIn\s*$/i, '').trim();
    const parts = cleaned.split(/\s+[-–—]\s+/);
    return {
      name: parts[0] ? parts[0].trim() : '',
      headline: parts[1] ? parts[1].trim() : '',
      company: parts[2] ? parts[2].trim() : ''
    };
  }

  function parseFromUrl(url) {
    try {
      const match = url.match(/\/in\/([^/?#]+)/);
      if (match && match[1]) {
        const slug = decodeURIComponent(match[1])
          .replace(/[-_.]+/g, ' ')
          .replace(/\d+$/g, '')
          .trim();
        // Capitalize words
        const formatted = slug.split(' ')
          .filter(Boolean)
          .map(w => w.charAt(0).toUpperCase() + w.slice(1))
          .join(' ');
        return formatted;
      }
    } catch (e) {}
    return '';
  }

  function extractProfileData() {
    const url = window.location.href;
    const isProfile = url.includes('/in/');

    if (!isProfile) {
      return {
        isProfilePage: false,
        error: 'Please open an individual LinkedIn Profile page (linkedin.com/in/...)'
      };
    }

    const jsonLd = parseJsonLd();
    const meta = parseMetaTags();
    const titleParsed = parseDocTitle();
    const urlName = parseFromUrl(url);

    // 1. FULL NAME
    let fullName = '';
    const nameEl = document.querySelector('h1.inline.t-24.v-align-middle') ||
      document.querySelector('h1.text-heading-xlarge') ||
      document.querySelector('.top-card-layout__title') ||
      document.querySelector('.pv-top-card--list h1') ||
      document.querySelector('main section h1') ||
      document.querySelector('h1.top-card-layout__title') ||
      document.querySelector('h1');

    if (nameEl) {
      const txt = getCleanText(nameEl);
      if (txt && !txt.toLowerCase().includes('linkedin') && !txt.toLowerCase().includes('sign in')) {
        fullName = txt;
      }
    }

    if (!fullName) fullName = jsonLd?.name || meta?.name || titleParsed?.name || urlName || 'LinkedIn Contact';

    // Clean any badges / suffixes like "(He/Him)" or "1st" from name
    fullName = fullName
      .replace(/\s*\(.*?\)\s*/g, ' ')
      .replace(/\s+(1st|2nd|3rd|\+)\s*$/gi, '')
      .trim();

    // 2. HEADLINE / JOB TITLE
    let headline = '';
    const headlineEl = document.querySelector('.text-body-medium.break-words') ||
      document.querySelector('h2.top-card-layout__headline') ||
      document.querySelector('.top-card-layout__headline') ||
      document.querySelector('div.text-body-medium[data-generated-suggestion-target]') ||
      document.querySelector('.pv-top-card--list-bullet .pv-top-card--item') ||
      document.querySelector('.pv-text-details__left-panel div.text-body-medium') ||
      document.querySelector('main section .text-body-medium');

    if (headlineEl) headline = getCleanText(headlineEl);
    if (!headline) headline = jsonLd?.jobTitle || meta?.headline || titleParsed?.headline || 'Business Leader';

    // 3. COMPANY NAME
    let companyName = '';
    const compEl = document.querySelector('button[aria-label*="Current company"]') ||
      document.querySelector('.top-card-layout__first-subline .top-card__flavor') ||
      document.querySelector('.pv-text-details__right-panel .pv-item-component__title') ||
      document.querySelector('li.pv-text-details__right-panel-item button span') ||
      document.querySelector('[data-field="experience_company_name"]') ||
      document.querySelector('div[aria-label="Current company"]');

    if (compEl) companyName = getCleanText(compEl);
    if (!companyName) companyName = jsonLd?.company || meta?.company || titleParsed?.company || '';

    // Infer company from headline if not found yet
    if (!companyName && headline) {
      if (headline.includes(' at ')) {
        companyName = headline.split(' at ')[1].trim();
      } else if (headline.includes(' @ ')) {
        companyName = headline.split(' @ ')[1].trim();
      } else if (headline.includes(' | ')) {
        companyName = headline.split(' | ')[1].trim();
      }
    }

    if (!companyName) companyName = 'Organization';

    // 4. JOB TITLE (derived cleanly)
    let jobTitle = headline;
    if (headline.includes(' at ')) {
      jobTitle = headline.split(' at ')[0].trim();
    } else if (headline.includes(' @ ')) {
      jobTitle = headline.split(' @ ')[0].trim();
    } else if (headline.includes(' | ')) {
      jobTitle = headline.split(' | ')[0].trim();
    }

    // 5. LOCATION
    let location = '';
    const locEl = document.querySelector('span.text-body-small.inline.t-black--light.break-words') ||
      document.querySelector('.top-card__subline-item') ||
      document.querySelector('.top-card-layout__first-subline span') ||
      document.querySelector('.pv-top-card--list-bullet .pv-top-card--item-bullet') ||
      document.querySelector('.pv-text-details__left-panel + div span') ||
      document.querySelector('h3.top-card-layout__first-subline');

    if (locEl) location = getCleanText(locEl);
    if (!location) location = jsonLd?.location || 'United States';

    // 6. ABOUT / SUMMARY
    let summary = '';
    const aboutSection = document.querySelector('#about');
    if (aboutSection) {
      const parentCard = aboutSection.closest('section');
      if (parentCard) {
        const summaryTextEl = parentCard.querySelector('.display-flex.ph0.pv0') ||
          parentCard.querySelector('.inline-show-more-text') ||
          parentCard.querySelector('span[aria-hidden="true"]');
        if (summaryTextEl) summary = getCleanText(summaryTextEl);
      }
    }
    if (!summary) {
      const publicAbout = document.querySelector('.summary p') || document.querySelector('.core-section-container[data-section="summary"] p');
      if (publicAbout) summary = getCleanText(publicAbout);
    }
    if (!summary) summary = jsonLd?.description || meta?.description || `${fullName} is ${headline} at ${companyName}.`;

    // 7. KEY SKILLS
    let skills = '';
    const skillsSection = document.querySelector('#skills');
    if (skillsSection) {
      const parentCard = skillsSection.closest('section');
      if (parentCard) {
        const skillSpans = parentCard.querySelectorAll('span[aria-hidden="true"]');
        const skillList = [];
        skillSpans.forEach(s => {
          const t = getCleanText(s);
          if (t && t.length < 35 && !skillList.includes(t) && !t.toLowerCase().includes('skill')) {
            skillList.push(t);
          }
        });
        if (skillList.length > 0) skills = skillList.slice(0, 6).join(', ');
      }
    }
    if (!skills) skills = 'Executive Leadership, Strategic Operations, Business Development, Technology';

    return {
      isProfilePage: true,
      fullName: fullName,
      headline: headline,
      jobTitle: jobTitle,
      companyName: companyName,
      location: location,
      linkedInUrl: url.split('?')[0],
      summary: summary,
      skills: skills,
      experienceHistory: '',
      extractedAt: new Date().toISOString()
    };
  }

  // Listen for extraction requests from popup
  chrome.runtime.onMessage.addListener((request, sender, sendResponse) => {
    if (request.action === 'SCRAPE_PROFILE') {
      try {
        const data = extractProfileData();
        sendResponse({ success: true, data });
      } catch (err) {
        sendResponse({ success: false, error: err.message });
      }
    }
    return true;
  });
})();
