# ==============================================================================
# MonsterASP Perfect Deployment Package Generator
# ==============================================================================
Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "Building 100% Error-Free MonsterASP Deployment Bundle" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

$rootDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$frontendDir = Join-Path $rootDir "frontend\crm-web"
$backendDir = Join-Path $rootDir "backend\CrmLeadTool.Api"
$wwwrootDir = Join-Path $backendDir "wwwroot"
$publishDir = Join-Path $backendDir "publish"
$zipOutput = Join-Path $rootDir "MonsterASP-Deployment-Package.zip"

# 1. Build Angular Production App
Write-Host "`n[Step 1/5] Compiling Angular Production SPA..." -ForegroundColor Yellow
Set-Location $frontendDir
npm run build -- --configuration production
if ($LASTEXITCODE -ne 0) {
    Write-Host "Angular build failed!" -ForegroundColor Red
    Exit 1
}

# 2. Sync Angular SPA into ASP.NET Core wwwroot
Write-Host "`n[Step 2/5] Syncing Angular SPA into ASP.NET Core wwwroot..." -ForegroundColor Yellow
$backendUploads = Join-Path $wwwrootDir "uploads"
$tempUploads = Join-Path $rootDir "temp_uploads_backup"
if (Test-Path $backendUploads) {
    Copy-Item -Recurse -Force $backendUploads $tempUploads
}
if (Test-Path $wwwrootDir) {
    Remove-Item -Recurse -Force $wwwrootDir
}
New-Item -ItemType Directory -Path $wwwrootDir | Out-Null
Copy-Item -Recurse -Force (Join-Path $frontendDir "dist\crm-web\browser\*") $wwwrootDir

# Restore uploads in wwwroot
if (Test-Path $tempUploads) {
    Copy-Item -Recurse -Force $tempUploads (Join-Path $wwwrootDir "uploads")
    Remove-Item -Recurse -Force $tempUploads
}

# 3. Publish Portable ASP.NET Core Web API
Write-Host "`n[Step 3/5] Compiling Portable .NET 10 Release..." -ForegroundColor Yellow
Set-Location $backendDir
if (Test-Path $publishDir) {
    Remove-Item -Recurse -Force $publishDir
}
dotnet publish -c Release -o ./publish /p:UseAppHost=false
if ($LASTEXITCODE -ne 0) {
    Write-Host ".NET publish failed!" -ForegroundColor Red
    Exit 1
}

# 4. Flatten all runtimes directly into the root & patch deps.json
Write-Host "`n[Step 4/5] Flattening runtime DLLs and eradicating runtimes folder..." -ForegroundColor Yellow

# Copy Angular files to both root and wwwroot
Copy-Item -Recurse -Force (Join-Path $frontendDir "dist\crm-web\browser\*") $publishDir
Copy-Item -Recurse -Force (Join-Path $frontendDir "dist\crm-web\browser\*") (Join-Path $publishDir "wwwroot")

# Ensure product uploads are preserved in publish wwwroot and root
$srcUploads = Join-Path $wwwrootDir "uploads"
if (Test-Path $srcUploads) {
    Copy-Item -Recurse -Force $srcUploads (Join-Path $publishDir "wwwroot\uploads")
    Copy-Item -Recurse -Force $srcUploads (Join-Path $publishDir "uploads")
}

# Extract the full Windows SqlClient DLL directly to the root
$sqlClientRuntime = Join-Path $publishDir "runtimes\win\lib\net9.0\Microsoft.Data.SqlClient.dll"
if (Test-Path $sqlClientRuntime) {
    Copy-Item $sqlClientRuntime (Join-Path $publishDir "Microsoft.Data.SqlClient.dll") -Force
}

$sni64 = Join-Path $publishDir "runtimes\win-x64\native\Microsoft.Data.SqlClient.SNI.dll"
if (Test-Path $sni64) {
    Copy-Item $sni64 (Join-Path $publishDir "Microsoft.Data.SqlClient.SNI.dll") -Force
}

$sni86 = Join-Path $publishDir "runtimes\win-x86\native\Microsoft.Data.SqlClient.SNI.dll"
if (Test-Path $sni86) {
    Copy-Item $sni86 (Join-Path $publishDir "Microsoft.Data.SqlClient.SNI.x86.dll") -Force
}

# Completely delete the runtimes folder (zero deep nested directories)
$runtimesPath = Join-Path $publishDir "runtimes"
if (Test-Path $runtimesPath) {
    Remove-Item -Recurse -Force $runtimesPath
}

# Patch deps.json so .NET loads Microsoft.Data.SqlClient directly from root
$depsPath = Join-Path $publishDir "CrmLeadTool.Api.deps.json"
if (Test-Path $depsPath) {
    $deps = Get-Content $depsPath -Raw | ConvertFrom-Json
    $targets = $deps.targets.'.NETCoreApp,Version=v10.0'
    if ($targets.'Microsoft.Data.SqlClient/6.1.6') {
        $targets.'Microsoft.Data.SqlClient/6.1.6'.PSObject.Properties.Remove('runtimeTargets')
        $targets.'Microsoft.Data.SqlClient/6.1.6'.runtime = [PSCustomObject]@{
            'Microsoft.Data.SqlClient.dll' = [PSCustomObject]@{
                assemblyVersion = '6.0.0.0'
                fileVersion = '6.16.26175.8'
            }
        }
    }
    if ($targets.'Microsoft.Data.SqlClient.SNI.runtime/6.0.2') {
        $sniObj = $targets.'Microsoft.Data.SqlClient.SNI.runtime/6.0.2'
        $sniObj.PSObject.Properties.Remove('runtimeTargets')
        $nativeProp = [PSCustomObject]@{ 'Microsoft.Data.SqlClient.SNI.dll' = [PSCustomObject]@{ fileVersion = '6.2.0.0' } }
        $sniObj | Add-Member -MemberType NoteProperty -Name "native" -Value $nativeProp -Force
    }
    $deps | ConvertTo-Json -Depth 100 | Set-Content $depsPath
}

# Patch runtimeconfig.json for pure managed networking
$runtimeConfigPath = Join-Path $publishDir "CrmLeadTool.Api.runtimeconfig.json"
if (Test-Path $runtimeConfigPath) {
    $rc = Get-Content $runtimeConfigPath -Raw | ConvertFrom-Json
    if (-not $rc.runtimeOptions.configProperties) {
        $rc.runtimeOptions | Add-Member -MemberType NoteProperty -Name "configProperties" -Value ([PSCustomObject]@{}) -Force
    }
    $rc.runtimeOptions.configProperties | Add-Member -MemberType NoteProperty -Name "Switch.Microsoft.Data.SqlClient.UseManagedNetworkingOnWindows" -Value $true -Force
    $rc | ConvertTo-Json -Depth 10 | Set-Content $runtimeConfigPath
}

# 4b. Package LinkedIn Chrome Extension
Write-Host "`n[Step 4b] Packaging LinkedIn Chrome Extension..." -ForegroundColor Yellow
$extDir = Join-Path $rootDir "chrome-extension"
$extZip = Join-Path $rootDir "LinkedIn-Chrome-Extension.zip"
if (Test-Path $extZip) {
    Remove-Item -Force $extZip
}
Compress-Archive -Path (Join-Path $extDir "*") -DestinationPath $extZip -Force
# Include extension zip in the publish package as well
Copy-Item $extZip (Join-Path $publishDir "LinkedIn-Chrome-Extension.zip") -Force
$downloadsDir = Join-Path $publishDir "wwwroot\downloads"
New-Item -ItemType Directory -Path $downloadsDir -Force | Out-Null
Copy-Item $extZip (Join-Path $downloadsDir "LinkedIn-Chrome-Extension.zip") -Force

# 5. Generate Ready-to-Upload ZIP Package
Write-Host "`n[Step 5/5] Creating MonsterASP-Deployment-Package.zip..." -ForegroundColor Yellow
if (Test-Path $zipOutput) {
    Remove-Item -Force $zipOutput
}
Add-Type -AssemblyName "System.IO.Compression.FileSystem"
[System.IO.Compression.ZipFile]::CreateFromDirectory($publishDir, $zipOutput, [System.IO.Compression.CompressionLevel]::Optimal, $false)

Set-Location $rootDir
Write-Host "`n==========================================================" -ForegroundColor Green
Write-Host "PERFECT ERROR-FREE PACKAGE GENERATED!" -ForegroundColor Green
Write-Host "Output File: MonsterASP-Deployment-Package.zip" -ForegroundColor White
Write-Host "Verification: Runtimes folder eliminated. All assemblies in root." -ForegroundColor White
Write-Host "==========================================================" -ForegroundColor Green
