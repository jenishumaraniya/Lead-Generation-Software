// popup.js - Full Deep-Data LinkedIn Scraper (Experience, Education, Skills, Company Metrics) & CRM Sync
document.addEventListener('DOMContentLoaded', async () => {
  let currentProfileUrl = '';
  let currentPageType = 'PERSONAL'; // 'PERSONAL' or 'COMPANY'
  let apiUrl = 'http://localhost:5234';
  let extractedDeepData = null;

  // Elements
  const connectionBadge = document.getElementById('connectionBadge');
  const refreshTabBtn = document.getElementById('refreshTabBtn');
  const checkPageBtn = document.getElementById('checkPageBtn');
  const settingsBtn = document.getElementById('settingsBtn');
  const settingsPanel = document.getElementById('settingsPanel');
  const apiUrlInput = document.getElementById('apiUrlInput');
  const saveSettingsBtn = document.getElementById('saveSettingsBtn');
  
  const noProfileState = document.getElementById('noProfileState');
  const loadingState = document.getElementById('loadingState');
  const loadingMessage = document.getElementById('loadingMessage');
  const profileCard = document.getElementById('profileCard');
  const modeTag = document.getElementById('modeTag');

  const nameLabel = document.getElementById('nameLabel');
  const headlineLabel = document.getElementById('headlineLabel');
  const companyLabel = document.getElementById('companyLabel');
  const locationLabel = document.getElementById('locationLabel');
  const summaryLabel = document.getElementById('summaryLabel');
  const skillsGroup = document.getElementById('skillsGroup');

  const editName = document.getElementById('editName');
  const editHeadline = document.getElementById('editHeadline');
  const editCompany = document.getElementById('editCompany');
  const editLocation = document.getElementById('editLocation');
  const editSummary = document.getElementById('editSummary');
  const editSkills = document.getElementById('editSkills');

  const importBtn = document.getElementById('importBtn');
  const importAndAnalyzeBtn = document.getElementById('importAndAnalyzeBtn');
  const aiInsightsDrawer = document.getElementById('aiInsightsDrawer');
  const aiIntentBadge = document.getElementById('aiIntentBadge');
  const aiConfidence = document.getElementById('aiConfidence');
  const aiPriority = document.getElementById('aiPriority');
  const aiNextAction = document.getElementById('aiNextAction');
  const icebreakerList = document.getElementById('icebreakerList');
  const talkingPointsList = document.getElementById('talkingPointsList');
  const statusToast = document.getElementById('statusToast');

  // Load configured API URL
  const stored = await chrome.storage.local.get('crmApiUrl');
  if (stored.crmApiUrl && !stored.crmApiUrl.includes('5070')) {
    apiUrl = stored.crmApiUrl;
    apiUrlInput.value = apiUrl;
  } else {
    apiUrl = 'http://localhost:5234';
    apiUrlInput.value = apiUrl;
    await chrome.storage.local.set({ crmApiUrl: apiUrl });
  }

  // Toggle settings
  settingsBtn.addEventListener('click', () => {
    settingsPanel.classList.toggle('hidden');
  });

  saveSettingsBtn.addEventListener('click', async () => {
    apiUrl = apiUrlInput.value.trim().replace(/\/+$/, '');
    await chrome.storage.local.set({ crmApiUrl: apiUrl });
    settingsPanel.classList.add('hidden');
    showToast('API URL saved!', 'success');
    checkBackendHealth();
  });

  refreshTabBtn.addEventListener('click', () => inspectActiveTab());
  if (checkPageBtn) checkPageBtn.addEventListener('click', () => inspectActiveTab());

  async function checkBackendHealth() {
    try {
      const res = await fetch(`${apiUrl}/api/prospects`, { method: 'GET' });
      if (res.ok || res.status === 401 || res.status === 403) {
        connectionBadge.textContent = 'CRM Connected';
        connectionBadge.className = 'badge badge-online';
      } else {
        connectionBadge.textContent = 'API Error';
        connectionBadge.className = 'badge badge-offline';
      }
    } catch {
      connectionBadge.textContent = 'CRM Offline';
      connectionBadge.className = 'badge badge-offline';
    }
  }

  function showToast(message, type = 'success') {
    statusToast.textContent = message;
    statusToast.className = `toast toast-${type}`;
    statusToast.classList.remove('hidden');
    setTimeout(() => {
      statusToast.classList.add('hidden');
    }, 4000);
  }

  // =========================================================================
  // DEEP DOM SCRAPER ENGINE (Extracts Full Career, Education, Company Metrics)
  // =========================================================================
  function extractFullDeepLinkedInData() {
    function clean(str) {
      if (!str) return '';
      return str.replace(/\s+/g, ' ').trim();
    }

    const url = window.location.href;
    const isCompany = url.includes('/company/') || url.includes('/school/');

    // =========================================================================
    // 1. LINKEDIN COMPANY PAGE DEEP SCRAPER
    // =========================================================================
    if (isCompany) {
      // Company Name
      const compH1 = document.querySelector('h1.org-top-card-summary__title') ||
                     document.querySelector('.org-top-card__title') ||
                     document.querySelector('h1[title]') ||
                     document.querySelector('main h1') ||
                     document.querySelector('h1');
      let companyName = compH1 ? clean(compH1.innerText || compH1.textContent) : '';

      if (!companyName) {
        const rawTitle = (document.title || '').replace(/\s*\|\s*LinkedIn\s*$/i, '').trim();
        companyName = clean(rawTitle.split(/\s*[:|]\s*/)[0]);
      }

      companyName = companyName
        .replace(/:\s*(Overview|About|Jobs|Life|Posts|People|Insights|Videos).*$/i, '')
        .replace(/\s+·\s+.*$/g, '')
        .trim();

      // Parse Subline Elements
      let industry = '';
      let location = '';
      let companySize = '';

      const infoItems = Array.from(document.querySelectorAll('.org-top-card-summary-info-list__info-item, .org-top-card-summary-info-list > div, .org-top-card-summary-info-list > span, .org-top-card-summary__info-item'));
      
      if (infoItems.length > 0) {
        infoItems.forEach((item, idx) => {
          const t = clean(item.innerText || item.textContent);
          if (!t) return;
          if (t.toLowerCase().includes('employee') || t.toLowerCase().includes('associate') || t.match(/\d+[\d,]*\s*on LinkedIn/i)) {
            companySize = t.replace(/\s*view all.*$/i, '').trim();
          } else if (t.toLowerCase().includes('follower') || t.toLowerCase().includes('member')) {
            // follower count
          } else if (!industry && idx === 0) {
            industry = t;
          } else if (!location) {
            location = t;
          }
        });
      }

      if (!companySize || !industry || !location) {
        const topCardContainer = document.querySelector('.org-top-card-summary-info-list') ||
                                 document.querySelector('.org-top-card-summary__info') ||
                                 document.querySelector('.org-top-card');
        if (topCardContainer) {
          const rawLines = (topCardContainer.innerText || topCardContainer.textContent || '').split(/[\n\r·•|]+/);
          const cleanLines = rawLines.map(l => clean(l)).filter(l => l.length > 1);

          cleanLines.forEach((l, idx) => {
            if ((l.toLowerCase().includes('employee') || l.toLowerCase().includes('associate') || l.match(/\d+[\d,]*\s*on LinkedIn/i)) && !companySize) {
              const sizeMatch = l.match(/(\d+[\d,]*\+?(?:\s*-\s*\d+[\d,]*\+?)?\s*(?:employees|associates|people))/i) || l.match(/(\d+[\d,]*\+?\s*on LinkedIn)/i);
              companySize = sizeMatch ? sizeMatch[1].trim() : l;
            } else if (!l.toLowerCase().includes('follower') && !l.toLowerCase().includes('member') && !l.toLowerCase().includes('employee')) {
              if (!industry && idx === 0) {
                industry = l;
              } else if (!location && (l.includes(',') || l.includes('Cedex') || l.length > 2)) {
                location = l;
              }
            }
          });
        }
      }

      if (!location) {
        const locItem = document.querySelector('.org-location-card, .org-page-details__definition-text, dd[data-test-id="about-us__headquarters"]');
        if (locItem) location = clean(locItem.innerText || locItem.textContent);
      }

      if (!companySize) {
        const sizeItem = document.querySelector('dd[data-test-id="about-us__size"], .org-about-company-module__company-size-definition-text');
        if (sizeItem) companySize = clean(sizeItem.innerText || sizeItem.textContent);
      }

      // Website URL
      let website = '';
      const webBtn = Array.from(document.querySelectorAll('a')).find(a => {
        const t = clean(a.innerText || a.textContent).toLowerCase();
        return t.includes('visit website') || t.includes('website');
      }) || document.querySelector('a[data-field="website_url"], a.org-page-details__website-link');

      if (webBtn) {
        website = clean(webBtn.innerText || webBtn.href).replace(/^visit website\s*/i, '').trim();
      }

      // Overview / Description
      let description = '';
      const allHeadings = Array.from(document.querySelectorAll('h2, h3, h4, h5, p, strong'));
      const overviewHeading = allHeadings.find(el => clean(el.innerText || el.textContent).toLowerCase() === 'overview');

      if (overviewHeading && overviewHeading.parentElement) {
        const descP = overviewHeading.parentElement.querySelector('p, span') || overviewHeading.nextElementSibling;
        if (descP) description = clean(descP.innerText || descP.textContent);
      }

      if (!description) {
        const aboutEl = document.querySelector('.org-about-us-organization-description__text') ||
                        document.querySelector('.org-about-module__description') ||
                        document.querySelector('section.artdeco-card p');
        if (aboutEl) description = clean(aboutEl.innerText || aboutEl.textContent);
      }

      return {
        pageType: 'COMPANY',
        fullName: companyName || 'Company Account',
        headline: industry || 'Enterprise Sector',
        companySize: companySize || 'Enterprise Scale',
        location: location || 'Headquarters',
        summary: description,
        skills: industry || 'Corporate Infrastructure, B2B Operations',
        website: website,
        url: url
      };
    }

    // =========================================================================
    // 2. LINKEDIN PERSONAL PROFILE DEEP SCRAPER
    // =========================================================================
    let fullName = '';
    let headline = '';
    let jobTitle = '';
    let companyName = '';
    let location = '';
    let summary = '';
    let skills = [];
    let experiences = [];
    let education = [];

    // 2.1 FULL NAME
    const nameEl = document.querySelector('.pv-text-details__left-panel h1') ||
                   document.querySelector('h1.text-heading-xlarge') ||
                   document.querySelector('main section h1') ||
                   document.querySelector('h1.top-card-layout__title') ||
                   document.querySelector('h1');
    if (nameEl) {
      fullName = clean(nameEl.innerText || nameEl.textContent);
    }

    if (!fullName) {
      const docTitle = (document.title || '').replace(/\s*\|\s*LinkedIn\s*$/i, '').trim();
      if (docTitle && !docTitle.toLowerCase().includes('sign in') && !docTitle.toLowerCase().includes('feed')) {
        fullName = clean(docTitle.split(/\s+[-–—]\s+/)[0]);
      }
    }

    const rawNameClean = fullName.split(/\s*\(|\s+·|\s+She\/|\s+He\/|\s+They\//i)[0]
      .replace(/\s+(1st|2nd|3rd|\+)\s*$/gi, '')
      .trim();
    if (rawNameClean) fullName = rawNameClean;

    const topSection = nameEl ? (nameEl.closest('section') || nameEl.closest('.artdeco-card') || nameEl.closest('.ph5') || document.querySelector('main section') || document.body) : document.body;

    // 2.2 HEADLINE
    const headlineEl = document.querySelector('.pv-text-details__left-panel .text-body-medium.break-words') ||
                       document.querySelector('.pv-text-details__left-panel .text-body-medium') ||
                       document.querySelector('.text-body-medium.break-words') ||
                       topSection.querySelector('.text-body-medium') ||
                       document.querySelector('.top-card-layout__headline');

    if (headlineEl) {
      const t = clean(headlineEl.innerText || headlineEl.textContent);
      if (t && !t.startsWith(fullName)) headline = t;
    }

    if (!headline) {
      const candidates = Array.from(topSection.querySelectorAll('div, p, h2, span'));
      const found = candidates.find(el => {
        const t = clean(el.innerText || el.textContent);
        return t && !t.includes(fullName) && !t.includes('Contact info') && !t.includes('connection') && !t.includes('follower') && !t.startsWith('http') && t.length > 5;
      });
      if (found) headline = clean(found.innerText || found.textContent);
    }

    // 2.3 LOCATION
    const locSpan = document.querySelector('.pv-text-details__left-panel span.text-body-small.inline') ||
                    document.querySelector('span.text-body-small.inline.t-black--light') ||
                    document.querySelector('.top-card-layout__first-subline span') ||
                    document.querySelector('.top-card__subline-item');
    if (locSpan) {
      const t = clean(locSpan.innerText || locSpan.textContent);
      if (t && !t.includes('|') && !t.includes(fullName)) {
        location = t.replace(/·?\s*Contact info/gi, '').trim();
      }
    }

    if (!location) {
      const contactLinks = Array.from(topSection.querySelectorAll('a, button, span, div')).filter(el => {
        const t = clean(el.innerText || el.textContent).toLowerCase();
        return t.includes('contact info');
      });

      for (const link of contactLinks) {
        const parent = link.closest('.pv-text-details__left-panel') || link.parentElement;
        if (parent) {
          const fullText = clean(parent.innerText || parent.textContent);
          const locPart = fullText.split(/·?\s*Contact info/i)[0].trim();
          const locClean = locPart.replace(headline, '').replace(fullName, '').replace(/^[·,\s-]+|[·,\s-]+$/g, '').trim();
          if (locClean && !locClean.includes('|') && locClean.length < 60) {
            location = locClean;
            break;
          }
        }
      }
    }

    // 2.4 COMPANY NAME (Multi-Tier Selector Engine)
    const rightPanelButtons = Array.from(document.querySelectorAll('.pv-text-details__right-panel button, .pv-text-details__right-panel a, ul.pv-text-details__right-panel li, .pv-top-card--list-bullet li'));
    for (const btn of rightPanelButtons) {
      const aria = btn.getAttribute('aria-label') || '';
      if (aria.includes('Current company:')) {
        const m = aria.match(/Current company:\s*([^.]+)/i);
        if (m && m[1]) {
          companyName = clean(m[1]);
          break;
        }
      }

      const t = clean(btn.innerText || btn.textContent);
      if (t && t.length > 1 && t.length < 60) {
        companyName = t;
        break;
      }
    }

    if (!companyName) {
      const logoImg = document.querySelector('.pv-text-details__right-panel img[alt], ul.pv-text-details__right-panel img[alt]');
      if (logoImg && logoImg.alt) {
        companyName = clean(logoImg.alt).replace(/\s*logo\s*$/i, '').trim();
      }
    }

    if (!companyName && headline) {
      const atMatch = headline.match(/@\s*([A-Za-z0-9\s&.-]+)/i) ||
                      headline.match(/\bat\s+([A-Za-z0-9\s&.-]+)/i);
      if (atMatch && atMatch[1]) {
        companyName = atMatch[1].split('|')[0].split('•')[0].trim();
      }
    }

    // 2.5 DEEP SCRAPE WORK EXPERIENCE HISTORY
    const expSection = document.querySelector('#experience')?.closest('section') || 
                       document.querySelector('section:has(#experience)') ||
                       Array.from(document.querySelectorAll('section')).find(s => clean(s.innerText || '').toLowerCase().includes('experience'));

    if (expSection) {
      const expItems = expSection.querySelectorAll('li.artdeco-list__item, li.pvs-list__pvs-item, div.pvs-entity');
      expItems.forEach(item => {
        const titleEl = item.querySelector('.t-bold span[aria-hidden="true"], .mr1 span[aria-hidden="true"], span.t-bold');
        const compEl = item.querySelector('.t-normal span[aria-hidden="true"], .t-14.t-normal');
        const dateEl = item.querySelector('.t-black--light span[aria-hidden="true"], .pvs-entity__caption-wrapper');

        if (titleEl) {
          const titleText = clean(titleEl.innerText || titleEl.textContent);
          const compText = compEl ? clean(compEl.innerText || compEl.textContent) : '';
          const dateText = dateEl ? clean(dateEl.innerText || dateEl.textContent) : '';

          if (titleText && !titleText.includes('Full-time') && !titleText.includes('Part-time') && titleText.length > 2) {
            experiences.push({
              title: titleText,
              company: compText.split(/·|•/)[0].trim(),
              duration: dateText
            });
          }
        }
      });
    }

    // Set primary companyName & jobTitle from Experience history if present
    if (experiences.length > 0) {
      if (!jobTitle) jobTitle = experiences[0].title;
      if (!companyName && experiences[0].company) companyName = experiences[0].company;
    }

    // 2.6 DEEP SCRAPE EDUCATION HISTORY
    const eduSection = document.querySelector('#education')?.closest('section') || 
                       document.querySelector('section:has(#education)') ||
                       Array.from(document.querySelectorAll('section')).find(s => clean(s.innerText || '').toLowerCase().includes('education'));

    if (eduSection) {
      const eduItems = eduSection.querySelectorAll('li.artdeco-list__item, li.pvs-list__pvs-item, div.pvs-entity');
      eduItems.forEach(item => {
        const schoolEl = item.querySelector('.t-bold span[aria-hidden="true"], .mr1 span[aria-hidden="true"], span.t-bold');
        const degreeEl = item.querySelector('.t-normal span[aria-hidden="true"], .t-14.t-normal');

        if (schoolEl) {
          const schoolText = clean(schoolEl.innerText || schoolEl.textContent);
          const degreeText = degreeEl ? clean(degreeEl.innerText || degreeEl.textContent) : '';
          if (schoolText && schoolText.length > 2) {
            education.push({
              school: schoolText,
              degree: degreeText
            });
          }
        }
      });
    }

    // 2.7 DEEP SCRAPE SKILLS ARRAY
    const skillsKeywords = [
      'Java', 'Python', 'React', 'Angular', 'Flutter', 'Next', 'Node', 'Node JS', 
      'JavaScript', 'TypeScript', 'C#', '.NET', 'AWS', 'Azure', 'SQL', 'Docker', 
      'Kubernetes', 'Firebase', 'Mobile', 'UI/UX', 'Cloud', 'AI', 'ML', 'Odoo', 
      'ERP', 'Problem Solving', 'Data Structures', 'Business Development', 'Sales', 
      'Marketing', 'Education', 'Spring Boot', 'PHP', 'HTML', 'CSS', 'DevOps',
      'System Architecture', 'Git', 'Agile', 'Scrum', 'Management'
    ];

    if (headline) {
      skillsKeywords.forEach(k => {
        if (new RegExp('\\b' + k.replace(/[.*+?^${}()|[\]\\]/g, '\\$&') + '\\b', 'i').test(headline) && !skills.includes(k)) {
          skills.push(k);
        }
      });
    }

    const skillElements = document.querySelectorAll('#skills ~ div span[aria-hidden="true"], .skills-section li span, section:has(#skills) li span');
    skillElements.forEach(s => {
      const t = clean(s.innerText || s.textContent);
      if (t && t.length < 35 && !skills.includes(t) && !t.toLowerCase().includes('skill') && !t.toLowerCase().includes('endorse')) {
        skills.push(t);
      }
    });

    // 2.8 SUMMARY / BIO
    const aboutSection = document.querySelector('#about ~ div') ||
                         document.querySelector('.summary p') ||
                         document.querySelector('.core-section-container[data-section="summary"] p') ||
                         document.querySelector('section.artdeco-card .inline-show-more-text');
    if (aboutSection) {
      summary = clean(aboutSection.innerText || aboutSection.textContent);
    }

    // Compile deep summary if about is empty
    if (!summary) {
      let bioParts = [`${fullName} is ${jobTitle || headline || 'a Professional'}${companyName ? ' at ' + companyName : ''}${location ? ' based in ' + location : ''}.`];
      if (experiences.length > 0) {
        bioParts.push(`Work history includes: ${experiences.map(e => `${e.title}${e.company ? ' at ' + e.company : ''}`).slice(0, 3).join('; ')}.`);
      }
      if (education.length > 0) {
        bioParts.push(`Education: ${education.map(e => `${e.degree || 'Degree'} from ${e.school}`).slice(0, 2).join('; ')}.`);
      }
      summary = bioParts.join(' ');
    }

    return {
      pageType: 'PERSONAL',
      fullName: fullName || 'Candidate',
      headline: headline || 'Professional',
      jobTitle: jobTitle || headline || 'Professional',
      companyName: companyName || '',
      location: location || '',
      summary: summary,
      skills: skills.length > 0 ? skills.slice(0, 10).join(', ') : 'Software Development, Problem Solving, Professional Services',
      experiences: experiences,
      education: education,
      url: url
    };
  }

  // Inspect active tab and extract data directly
  async function inspectActiveTab() {
    loadingState.classList.remove('hidden');
    loadingMessage.textContent = 'Performing deep DOM extraction (Experience, Education, Skills)...';
    noProfileState.classList.add('hidden');
    profileCard.classList.add('hidden');
    aiInsightsDrawer.classList.add('hidden');

    try {
      const [tab] = await chrome.tabs.query({ active: true, currentWindow: true });
      if (!tab || !tab.url || (!tab.url.includes('linkedin.com/in/') && !tab.url.includes('linkedin.com/company/') && !tab.url.includes('linkedin.com/school/'))) {
        loadingState.classList.add('hidden');
        noProfileState.classList.remove('hidden');
        return;
      }

      currentProfileUrl = tab.url;

      // Execute extraction function directly inside the tab
      const results = await chrome.scripting.executeScript({
        target: { tabId: tab.id },
        func: extractFullDeepLinkedInData
      });

      loadingState.classList.add('hidden');

      if (results && results[0] && results[0].result) {
        extractedDeepData = results[0].result;
        currentPageType = extractedDeepData.pageType || 'PERSONAL';
        console.log('Extracted Deep LinkedIn Data:', extractedDeepData);
        populateForm(extractedDeepData);
        profileCard.classList.remove('hidden');
      } else {
        noProfileState.classList.remove('hidden');
      }
    } catch (err) {
      console.error('Inspection error:', err);
      loadingState.classList.add('hidden');
      noProfileState.classList.remove('hidden');
    }
  }

  function populateForm(data) {
    if (data.pageType === 'COMPANY') {
      modeTag.textContent = '🏢 LinkedIn Company Account';
      nameLabel.textContent = 'Company Name';
      headlineLabel.textContent = 'Industry / Sector';
      companyLabel.textContent = 'Headcount / Size';
      locationLabel.textContent = 'Headquarters';
      summaryLabel.textContent = 'Company Overview / Description';
      skillsGroup.classList.add('hidden');

      importBtn.textContent = '🏢 Import Company Account to CRM';
      importAndAnalyzeBtn.textContent = '🧠 Import & Run Account AI Analysis';

      editName.value = data.fullName || '';
      editHeadline.value = data.headline || '';
      editCompany.value = data.companySize || '';
      editLocation.value = data.location || '';
      editSummary.value = data.summary || '';
      editSkills.value = '';
    } else {
      modeTag.textContent = '👤 LinkedIn Prospect Profile (Deep Signals)';
      nameLabel.textContent = 'Full Name';
      headlineLabel.textContent = 'Headline / Role';
      companyLabel.textContent = 'Company';
      locationLabel.textContent = 'Location';
      summaryLabel.textContent = 'Professional Summary / Deep Bio';
      skillsGroup.classList.remove('hidden');

      importBtn.textContent = '📥 Import Prospect to CRM';
      importAndAnalyzeBtn.textContent = '🧠 Import & Run Groq AI Analysis';

      editName.value = data.fullName || '';
      editHeadline.value = data.headline || '';
      editCompany.value = data.companyName || '';
      editLocation.value = data.location || '';
      editSummary.value = data.summary || '';
      editSkills.value = data.skills || '';
    }
  }

  // Handle Import
  importBtn.addEventListener('click', async () => {
    await submitToCrm(false);
  });

  // Handle Import + Instant AI Analysis
  importAndAnalyzeBtn.addEventListener('click', async () => {
    await submitToCrm(true);
  });

  async function submitToCrm(withAi = false) {
    const name = editName.value.trim();
    if (!name) {
      showToast('Please enter a name.', 'error');
      return;
    }

    loadingState.classList.remove('hidden');
    loadingMessage.textContent = withAi ? 'Syncing deep profile to CRM & running Groq AI...' : 'Syncing data to CRM...';

    if (currentPageType === 'COMPANY') {
      // Submit Company Payload
      const payload = {
        companyName: name,
        tagline: editHeadline.value.trim(),
        industry: editHeadline.value.trim(),
        companySize: editCompany.value.trim() || '1,000+ employees',
        location: editLocation.value.trim() || 'Global',
        linkedInUrl: currentProfileUrl.split('?')[0],
        description: editSummary.value.trim(),
        autoCreateLead: true,
        autoAnalyzeAi: withAi
      };

      try {
        const res = await fetch(`${apiUrl}/api/prospects/import-linkedin-company`, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify(payload)
        });

        const json = await res.json();
        loadingState.classList.add('hidden');

        if (res.ok && json.success) {
          showToast(`✓ Company "${json.companyName}" imported to CRM! (Score: ${json.score})`, 'success');
          if (json.aiAnalysis) {
            renderAiInsights(json.aiAnalysis);
          }
        } else {
          showToast(json.error || 'Failed to import company to CRM.', 'error');
        }
      } catch (err) {
        loadingState.classList.add('hidden');
        showToast(`Network error: ${err.message}. Check backend connection.`, 'error');
      }
    } else {
      // Submit Personal Profile Payload
      const headline = editHeadline.value.trim();
      let jobTitle = headline;
      if (headline.includes(' at ')) {
        jobTitle = headline.split(' at ')[0].trim();
      } else if (headline.includes(' @ ')) {
        jobTitle = headline.split(' @ ')[0].trim();
      } else if (headline.includes(' | ')) {
        jobTitle = headline.split(' | ')[0].trim();
      }

      const payload = {
        fullName: name,
        headline: headline,
        jobTitle: jobTitle || headline || 'Lead',
        companyName: editCompany.value.trim() || 'Organization',
        location: editLocation.value.trim() || 'India',
        linkedInUrl: currentProfileUrl.split('?')[0],
        summary: editSummary.value.trim(),
        skills: editSkills.value.trim(),
        autoCreateLead: true,
        autoAnalyzeAi: withAi
      };

      try {
        const res = await fetch(`${apiUrl}/api/prospects/import-linkedin-profile`, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify(payload)
        });

        const json = await res.json();
        loadingState.classList.add('hidden');

        if (res.ok && json.success) {
          showToast(`✓ Prospect "${json.fullName}" imported to CRM! (Score: ${json.score})`, 'success');
          if (json.aiAnalysis) {
            renderAiInsights(json.aiAnalysis);
          }
        } else {
          showToast(json.error || 'Failed to import to CRM.', 'error');
        }
      } catch (err) {
        loadingState.classList.add('hidden');
        showToast(`Network error: ${err.message}. Check backend connection.`, 'error');
      }
    }
  }

  function renderAiInsights(analysis) {
    aiIntentBadge.textContent = analysis.intent || 'BUYING';
    aiConfidence.textContent = `${analysis.confidenceScore || 85}%`;
    aiPriority.textContent = analysis.priorityRecommendation || 'HIGH';
    aiNextAction.textContent = analysis.recommendedNextAction || 'Initiate outreach';

    icebreakerList.innerHTML = '';
    talkingPointsList.innerHTML = '';

    const insights = analysis.insights || [];
    const icebreakers = insights.filter(i => i.insightType === 'ICEBREAKER');
    const talkingPoints = insights.filter(i => i.insightType === 'TALKING_POINT' || i.insightType === 'PAIN_POINT');

    icebreakers.forEach(item => {
      const div = createInsightItem(item.insightText);
      icebreakerList.appendChild(div);
    });

    talkingPoints.forEach(item => {
      const div = createInsightItem(item.insightText);
      talkingPointsList.appendChild(div);
    });

    aiInsightsDrawer.classList.remove('hidden');
  }

  function createInsightItem(text) {
    const item = document.createElement('div');
    item.className = 'insight-item';

    const span = document.createElement('span');
    span.textContent = text;

    const copyBtn = document.createElement('button');
    copyBtn.className = 'copy-btn';
    copyBtn.title = 'Copy to clipboard';
    copyBtn.textContent = '📋';
    copyBtn.addEventListener('click', async () => {
      await navigator.clipboard.writeText(text);
      copyBtn.textContent = '✓';
      setTimeout(() => copyBtn.textContent = '📋', 2000);
    });

    item.appendChild(span);
    item.appendChild(copyBtn);
    return item;
  }

  // Initial Run
  await checkBackendHealth();
  await inspectActiveTab();
});
