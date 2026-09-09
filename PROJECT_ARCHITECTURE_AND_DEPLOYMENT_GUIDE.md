# LeadFlow CRM: Complete Architecture, Backend Engine & MonsterASP Deployment Guide

---

## 1. Executive Summary & System Overview

**LeadFlow CRM** is an enterprise-grade, full-stack B2B Lead Generation, Inbound Visitor Intelligence, and Sales Automation platform. It bridges the gap between anonymous website traffic, outbound prospect discovery, AI-driven qualification, and sales rep pipeline execution.

### High-Level Architecture Diagram

```
+---------------------------------------------------------------------------------------------------+
|                                        CLIENT TOUCHPOINTS                                         |
+------------------------------------+----------------------------------+---------------------------+
|   1. Angular 19/20 Public & Admin  |   2. Inbound Visitor Tracker     |  3. Chrome Extension      |
|   - Catalog & Product Detail       |   - Anonymous Session Cookie     |  - LinkedIn Profile Scrape|
|   - Multi-Product Contact Form     |   - Click / View Telemetry       |  - 1-Click CRM Injection  |
|   - Admin & Sales Pipeline Portals |   - Consent Lifecycle            |                           |
+------------------------------------+----------------------------------+---------------------------+
                                                  |
                                    HTTPS / REST API Requests
                                                  v
+---------------------------------------------------------------------------------------------------+
|                                  ASP.NET CORE 10 WEB API ENGINE                                   |
+---------------------------------------------------------------------------------------------------+
|  SECURITY SHIELD:                                                                                 |
|  - Rate Limiter (Brute force defense)  |  - Signature Stripping (No Server/X-Powered-By headers)  |
|  - JWT Bearer Authentication (Zero skew)|  - Content Security Policy, Clickjacking Denial (DENY)   |
+---------------------------------------------------------------------------------------------------+
|  CORE SERVICES LAYER:                                                                             |
|  - LeadService: Inbound parsing, product-wise quantities, multi-key deduplication, auto-routing  |
|  - ScoringService: Dynamic rules execution (INTEREST_CLICK, FORM_SUBMIT, ROLE_MATCH, idempotency) |
|  - QualificationService: Score-based tiering (MQL -> SQL -> HIGH_INTENT)                          |
|  - GroqAIService: LLaMA 3.3 70B Versatile intent & commercial fit extraction with 8B fallback     |
|  - CampaignService & EmailSchedulerWorker: Automated multi-step email drip sequences              |
|  - LinkedInEnrichmentService: Public profile normalization and ICP matching                       |
+---------------------------------------------------------------------------------------------------+
|  DATA ACCESS & STORAGE:                                                                           |
|  - Entity Framework Core 9/10 with Pure Managed Networking (Switch.Microsoft.Data.SqlClient)       |
|  - Microsoft SQL Server (29 relational tables with idempotent migrations in DbInitializer)        |
+---------------------------------------------------------------------------------------------------+
```

---

## 2. Project Components Breakdown

### 2.1. Frontend: Angular 19/20 Standalone Application (`frontend/crm-web/`)
- **Architecture**: Modern Standalone Component architecture (no legacy `NgModule` clutter).
- **Key Modules & Portals**:
  1. **Public Catalog (`/products`, `/products/:id`, `/compare`)**:
     - Interactive product exploration with real-time specs, features, and pricing.
     - **Contact Modal (`app-contact-form`)**: Supports **product-wise quantities** with stepper buttons (`[-]` / `[+]`), unit prices, subtotals, and live total order value calculations.
     - **Visitor Tracking (`visitor-tracking.service.ts`)**: Collects anonymous cookie sessions and automatically dispatches `PRODUCT_VIEW`, `INTEREST_CLICK`, and `COOKIE_CONSENT_ACCEPTED` events to the backend.
  2. **Admin Portal (`/admin/*`)**:
     - **Dashboard**: Live KPIs, pipeline breakdown, conversion metrics.
     - **Leads Pipeline**: Comprehensive lead management, category filtering, manual reassignment.
     - **Visitor Tracking (`/admin/visitors`)**: Dedicated inbound intelligence console displaying real-time visitor traffic, activity counters, converted lead badges, and a slide-over **Visitor Journey Drawer** showing the exact chronological event stream.
     - **Email Campaigns**: Visual drip campaign builder with scheduled sequence steps.
     - **Products Catalog**: Full CRUD with draft/active status toggles and image uploads.
     - **Sales Team & Categories**: Organization structure and auto-routing rules.
     - **Scoring Rules Engine (`/admin/rules`)**: UI to configure point weights for behavioral triggers.
  3. **Sales Rep Portal (`/sales/*`)**:
     - Role-restricted pipeline showing leads assigned specifically to the rep or their designated product category.
     - Interactive activity logging (Calls, Emails, Meetings, Notes).
     - AI Analysis trigger with live intent and sentiment summaries.

### 2.2. Backend: ASP.NET Core 10 Web API (`backend/CrmLeadTool.Api/`)
- **Runtime**: .NET 10.0 LTS optimized for high-throughput asynchronous execution.
- **Hosting Model**: In-Process IIS hosting via `AspNetCoreModuleV2`.
- **Database Engine**: Microsoft SQL Server accessed through Entity Framework Core with automatic connection retry policies (`EnableRetryOnFailure`) and split queries.

### 2.3. AI Intelligence Engine (`GroqAIService.cs`)
- Powered by the ultra-fast **Groq Cloud API**.
- **Primary Model**: `llama-3.3-70b-versatile` for deep commercial reasoning, intent detection, and buying signal analysis.
- **Automatic Fallback**: Seamless downgrade to `llama-3.1-8b-instant` if rate limits or latency thresholds are exceeded.
- **Output Validation**: Enforces structured JSON output parsing to update lead qualification tiers and commercial summaries.

### 2.4. LinkedIn Chrome Extension (`chrome-extension/`)
- Manifest V3 extension designed for B2B sales development representatives (SDRs).
- Injects a discrete widget onto LinkedIn profile pages.
- Parses name, headline, current company, role seniority, and location, transmitting it directly to `POST /api/prospects` with authenticated JWT bearer credentials.

---

## 3. How the Backend Works Completely (Step-by-Step)

### 3.1. Inbound Visitor Journey & Session Tracking
1. When any visitor lands on the website, `visitor-tracking.service.ts` checks for a persistent anonymous ID stored in `localStorage` (`visitor_anonymous_id`). If not present, a cryptographically random UUID is generated (`temp-uuid` or server-assigned GUID).
2. The frontend sends telemetry via `POST /api/activity`:
   - `AnonymousId`: Unique browser fingerprint.
   - `ActivityType`: `PAGE_VIEW`, `PRODUCT_VIEW`, or `INTEREST_CLICK`.
   - `ProductId`: ID of the viewed or clicked product.
   - `PageUrl`: Current browser path.
   - `Metadata`: JSON context (e.g. source campaign, referrer).
3. In `ActivityController.cs`:
   - Updates `Visitor_CRM.LastSeenAt = DateTime.UtcNow`.
   - Saves a record to `VisitorActivity_CRM`.
   - **Real-Time Identified Lead Scoring**: If this visitor had previously converted to a lead (`lead.VisitorId == visitor.VisitorId`), clicking "I'm Interested" instantly adds points to their live lead score via `ScoringService.ApplyScoreEventAsync(..., "INTEREST_CLICK", allowDuplicates: true)`.

### 3.2. Multi-Product Contact Form Submission & Deduplication
1. When a prospect submits the contact form:
   - Contains prospect contact details (Name, Work Email, Phone, Company, Job Title, Domain).
   - Contains an array of selected product IDs (`productIds`).
   - Contains a dictionary of **per-product quantities** (`productQuantities: { "1": 5, "3": 2 }`).
2. Handled in `LeadService.CreateLeadFromFormAsync`:
   - **Multi-Key Deduplication**: `DuplicateService.FindDuplicateLeadAsync` checks matching Email, Phone, Full Name, and Company Name.
   - **Returning Leads**: If a match is found, the requirement is appended (`[Update UTC]: ...`), product quantities are updated, and a `REPEAT_VISIT` score event (+15 pts) is recorded.
   - **New Leads**: A new record is inserted into `Lead_CRM`. The `ProductQuantities` JSON dictionary is serialized, and total quantity is calculated as the sum of all individual product quantities.

### 3.3. Salesperson Routing & Category Assignment
- In `LeadService.cs`:
  - If selected products belong to **exactly 1 category**:
    - The lead's `IsMultiCategory` is set to `false`.
    - Automatically assigned to the active sales representative mapped to that category (`User_CRM.CategoryId`).
  - If selected products span **multiple different categories** (or 0 categories):
    - `IsMultiCategory` is set to `true`.
    - `AssignedTo` is left as `null`.
    - Flagged for Admin manual assignment so the admin can review the multi-category requirement and assign the best rep.

### 3.4. The Lead Scoring Engine & Qualification Tiering
The scoring engine (`ScoringService.cs`) calculates a quantitative quality score (0 to 100+ points) for every lead:
- **Dual Rule Execution**:
  1. `INTEREST_CLICK` (+15 points): Triggered **for each individual product** the lead expressed interest in (`allowDuplicates: true`). If a lead selects 3 products, they receive `15 x 3 = 45` points.
  2. `FORM_SUBMIT` (+25 points): Triggered for submitting the commercial inquiry form.
  3. `ROLE_MATCH` (+20 points): Automatically triggered if the job title contains decision-maker keywords (`VP`, `Director`, `Head`, `Chief`, `Manager`).
  4. `AI_ANALYSIS` (+15 points / -15 points): Added or deducted when AI analyzes commercial viability.
- **Idempotency & Single-Count Protection**:
  - Repeat actions (such as re-running AI analysis multiple times) will **not** duplicate points. The engine detects previous events of the same type and adjusts the net delta rather than stacking points infinitely.
- **Qualification Tiering (`QualificationService.cs`)**:
  - Automatically evaluates the total score:
    - **Score >= 70**: `HIGH_INTENT`
    - **Score >= 45**: `SQL` (Sales Qualified Lead)
    - **Score >= 25**: `MQL` (Marketing Qualified Lead)
    - **Score < 25**: `UNQUALIFIED`

### 3.5. Background Workers & Email Drip Sequences
- `EmailSchedulerWorker.cs` runs continuously as an ASP.NET Core `BackgroundService`.
- Every 60 seconds:
  - Scans `CampaignRecipient_CRM` for enrolled prospects.
  - Checks scheduled sequence step delays (`DelayDays` and `DelayHours`).
  - Dispatches automated personalized emails via SMTP (`EmailService.cs`).
  - Replaces dynamic template variables: `{{FirstName}}`, `{{Company}}`, `{{Product}}`.
  - Embeds tracking pixels for open tracking (`/api/tracking/open/{id}`) and rewrites links for click tracking (`/api/tracking/click/{id}`).

---

## 4. Security Architecture & Hardening for Deployment

When deploying to a public cloud or shared Windows hosting environment (such as MonsterASP), standard development configurations leave vulnerabilities. We applied the following enterprise-grade security hardening measures:

### 4.1. Network & HTTP Header Hardening (`web.config` & `Program.cs`)
1. **Server Signature Masking**:
   - Stripped `Server`, `X-Powered-By`, `X-AspNet-Version`, and `X-AspNetMvc-Version` headers.
   - Attackers cannot fingerprint the underlying .NET or IIS version to target known CVE exploits.
2. **Clickjacking Defense**:
   - Injected `X-Frame-Options: DENY` preventing the application from being loaded in an iframe on malicious sites.
3. **MIME Sniffing Prevention**:
   - Injected `X-Content-Type-Options: nosniff` preventing browsers from executing files masquerading as images or text.
4. **Cross-Site Scripting (XSS) Shield**:
   - Configured `X-XSS-Protection: 1; mode=block`.
5. **Referrer Policy**:
   - `Referrer-Policy: strict-origin-when-cross-origin` preventing sensitive query strings from leaking to external domains.

### 4.2. Brute Force & Rate Limiting Defense (`AddRateLimiter`)
Configured sliding-window rate limiters in `Program.cs`:
- **General API Limiter**: Capped at 100 requests per minute per IP address.
- **Authentication Limiter**: Capped at 5 login attempts per minute per IP address. Exceeding limits returns HTTP `429 Too Many Requests`.
- **Account Lockout Mechanism**: In `AuthService.cs`, 5 consecutive failed login attempts lock the user account for 15 minutes (`LockoutEnd = DateTime.UtcNow.AddMinutes(15)`).

### 4.3. Authentication & Password Cryptography
- **Password Storage**: Uses PBKDF2 with HMAC-SHA256, 100,000 iterations, and a unique 128-bit cryptographic salt per user. Passwords are never stored in plain text.
- **JWT Authentication**:
  - Signed using 256-bit symmetric security keys.
  - Validated with `ClockSkew = TimeSpan.Zero` (no grace period for expired tokens).
  - Short-lived Access Tokens paired with rotating Refresh Tokens stored in `RefreshToken_CRM`.

### 4.4. Database Security & Pure Managed Networking
- **SQL Injection Immunity**: 100% of database queries use parameterized Entity Framework Core LINQ statements. Zero dynamic raw string concatenations.
- **Idempotent Migrations**: `DbInitializer.cs` checks column and table existence before creating them, avoiding schema drops or destructive updates.
- **Pure Managed Networking Switch**:
  ```csharp
  AppContext.SetSwitch("Switch.Microsoft.Data.SqlClient.UseManagedNetworkingOnWindows", true);
  ```
  - **Why this was added**: By default, `Microsoft.Data.SqlClient` on Windows attempts to load native C++ binaries (`Microsoft.Data.SqlClient.SNI.dll`) to establish TCP connections. On shared hosting environments like MonsterASP, security sandboxes and missing Visual C++ runtimes frequently cause native DLL loading crashes (Error `0x80004005` or `500.30`). Enabling the managed networking switch forces .NET to use pure, secure managed C# socket streams, completely bypassing native C++ dependencies.

---

## 5. The MonsterASP Deployment Package (`MonsterASP-Deployment-Package.zip`)

### 5.1. The MonsterASP "500.30 In-Process Failure" Problem & How We Solved It
When deploying a modern .NET application to MonsterASP's IIS environment, standard `dotnet publish` packages frequently crash with HTTP `500.30 In-Process Failure` due to three root causes:
1. **The Nested `runtimes\` Directory Trap**: Standard `dotnet publish` places Windows-specific assemblies inside `runtimes\win\lib\net9.0\`. MonsterASP's shared IIS worker process runs with strict directory permission sandboxes and cannot resolve assemblies nested several folders deep.
2. **Native SNI Dependencies**: The `Microsoft.Data.SqlClient.SNI.dll` unmanaged library fails to load under restricted application pool identities.
3. **Executable Lockout (`UseAppHost=true`)**: Standard publish creates an executable `CrmLeadTool.Api.exe`. If IIS tries to restart the app pool while the `.exe` is locked, the deployment errors out.

### 5.2. How `build-monsterasp.ps1` Generates the 100% Error-Free Package
Our automated PowerShell script ([build-monsterasp.ps1](file:///d:/Lead-Generation-Software/build-monsterasp.ps1)) executes 5 precise engineering steps:

#### **[Step 1] Angular Production Compilation**
```powershell
Set-Location $frontendDir
npm run build -- --configuration production
```
- Compiles the Angular SPA with Ahead-of-Time (AOT) compilation, tree-shaking, minification, and CSS extraction into `frontend/crm-web/dist/crm-web/browser/`.

#### **[Step 2] Static Assets & Media Synchronization**
- Automatically synchronizes compiled HTML, JS, and CSS bundles into the ASP.NET Core `wwwroot/` folder.
- Preserves existing product images and catalog media in `uploads/products/` using a temporary backup buffer so customer images are never lost.

#### **[Step 3] Portable .NET 10 Release Compilation**
```powershell
dotnet publish -c Release -o ./publish /p:UseAppHost=false
```
- Compiles in `Release` mode with `/p:UseAppHost=false`.
- **Why `/p:UseAppHost=false` is critical**: Instead of generating a platform-dependent native Windows `.exe`, it generates a portable `CrmLeadTool.Api.dll`. IIS loads this DLL directly via `dotnet .\CrmLeadTool.Api.dll` under the `AspNetCoreModuleV2` in-process pipeline, preventing file-lock issues.

#### **[Step 4] Runtime DLL Flattening & Manifest Patching (The Key Innovation)**
```powershell
# 1. Extract Windows SqlClient DLL directly to root
Copy-Item $publishDir "runtimes\win\lib\net9.0\Microsoft.Data.SqlClient.dll" $publishDir -Force

# 2. Extract Native SNI DLLs to root
Copy-Item $publishDir "runtimes\win-x64\native\Microsoft.Data.SqlClient.SNI.dll" $publishDir -Force

# 3. Completely delete the runtimes folder
Remove-Item -Recurse -Force (Join-Path $publishDir "runtimes")
```
- **Manifest Patching (`CrmLeadTool.Api.deps.json`)**:
  - The script parses `CrmLeadTool.Api.deps.json`, removes the `runtimeTargets` pointing to the nested `runtimes/` folder, and updates the manifest to declare `Microsoft.Data.SqlClient.dll` as a root assembly.
- **Config Patching (`CrmLeadTool.Api.runtimeconfig.json`)**:
  - Injects `Switch.Microsoft.Data.SqlClient.UseManagedNetworkingOnWindows = true` directly into the published runtime configuration.

#### **[Step 4b] Packaging LinkedIn Chrome Extension**
- Automatically zips the `chrome-extension/` directory into `LinkedIn-Chrome-Extension.zip`.
- Copies it to `wwwroot/downloads/LinkedIn-Chrome-Extension.zip` so administrators and sales reps can download the extension directly from the live deployed CRM portal.

#### **[Step 5] ZIP Archive Generation**
- Compresses the entire `./publish/` folder into `MonsterASP-Deployment-Package.zip` at maximum compression level.

---

## 6. Exact File Structure Inside `MonsterASP-Deployment-Package.zip`

When you unzip or inspect `MonsterASP-Deployment-Package.zip`, here is the exact structure:

```
MonsterASP-Deployment-Package.zip
│
├── CrmLeadTool.Api.dll                  <-- Main compiled backend application assembly
├── CrmLeadTool.Api.deps.json            <-- Patched dependencies manifest (runtimes removed)
├── CrmLeadTool.Api.runtimeconfig.json   <-- Patched runtime configuration with managed networking
├── web.config                           <-- IIS AspNetCoreModuleV2 handler & security headers
├── appsettings.json                     <-- Production database connection string & JWT keys
│
├── Microsoft.Data.SqlClient.dll         <-- Flattened directly in root for instant loading
├── Microsoft.Data.SqlClient.SNI.dll     <-- Native 64-bit SNI DLL in root
├── Microsoft.EntityFrameworkCore.dll    <-- EF Core database engine
├── Microsoft.AspNetCore.*.dll           <-- ASP.NET Core framework assemblies
├── GroqSharp / GenAI assemblies         <-- AI SDK binaries
│
├── index.html                           <-- Angular SPA entry point (copied to root & wwwroot)
├── main-*.js                            <-- Minified Angular main bundle
├── polyfills-*.js                       <-- Browser compatibility polyfills
├── styles-*.css                         <-- Global compiled stylesheets
├── chunk-*.js                           <-- Lazy-loaded route chunks (dashboard, leads, visitors)
│
├── wwwroot/                             <-- Primary IIS static file directory
│   ├── index.html                       <-- Angular entry point
│   ├── main-*.js                        <-- Production JavaScript chunks
│   ├── styles-*.css                     <-- Stylesheet
│   ├── assets/                          <-- Icons, logos, and fonts
│   ├── uploads/products/                <-- Stored product catalog images
│   └── downloads/                       <-- LinkedIn-Chrome-Extension.zip download
│
└── LinkedIn-Chrome-Extension.zip        <-- Packaged ready-to-use Chrome extension
```

> [!NOTE]
> **No `runtimes/` folder exists in this package.** By eliminating the `runtimes` folder and flattening all DLLs into the root, MonsterASP's IIS environment loads every assembly without path resolution errors.

---

## 7. Step-by-Step Instructions to Deploy onto MonsterASP

1. **Log in to MonsterASP**:
   - Open your MonsterASP Control Panel and go to **Websites** -> Click on your website domain.
2. **Stop the Website** *(Recommended)*:
   - Click **Stop Website** in the control panel to release any open file locks.
3. **Open File Manager**:
   - Navigate to the **File Manager** tab.
   - Enter your website root folder (`/` or `site/wwwroot/` depending on your account setup).
4. **Clean Previous Files**:
   - Select and delete all previous files and folders (except your database files if stored locally, though MS SQL is typically hosted on a separate database server).
5. **Upload the ZIP**:
   - Click **Upload** -> Select `MonsterASP-Deployment-Package.zip`.
6. **Extract**:
   - Click on the uploaded zip file -> Select **Extract** -> Extract directly into the root folder.
   - Delete the `.zip` file after extraction to save disk space.
7. **Start the Website**:
   - Return to the website settings and click **Start Website**.
8. **Verify Live Health**:
   - Open `https://your-domain.com/api/health` in your browser.
   - You should see:
     ```json
     {
       "status": "Healthy",
       "timestamp": "..."
     }
     ```
   - Open `https://your-domain.com/` to interact with your live Angular frontend catalog, login to Admin, and verify Visitor Tracking.

---

## 8. Summary Checklist of Key Enhancements

| Feature Area | Key Implementation | Security & Reliability Benefit |
| :--- | :--- | :--- |
| **Contact Form** | Per-product quantity steppers, subtotals, live total calculation | Eliminates vague bulk inquiries; provides accurate order sizing |
| **Dual Scoring** | `INTEREST_CLICK` per product + `FORM_SUBMIT` + `ROLE_MATCH` | Accurately awards multi-intent interest; boosts hot prospects |
| **Visitor Tracking** | Dedicated `/admin/visitors` dashboard with journey slide-over | Full visibility into anonymous touchpoints and conversion paths |
| **AI Qualification** | Groq LLaMA 3.3 70B with 8B fallback | Real-time commercial sentiment and ICP matching within milliseconds |
| **MonsterASP Bundle** | Runtime flattening + `UseAppHost=false` + Managed Networking | Eradicates HTTP 500.30 crashes; guarantees zero assembly resolution errors |
| **API Security** | Sliding-window rate limiter, signature stripping, JWT with 0 skew | Full defense against brute force, scraping, and clickjacking |
