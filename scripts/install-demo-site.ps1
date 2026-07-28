# Demo Site Setup Script
# Creates a local Umbraco site referencing this repo's Umbraco.Community.AzureSSO project,
# for manually testing the SSO login flow during development.

param(
    [switch]$SkipTemplateInstall,
    [switch]$Force
)

$ErrorActionPreference = "Stop"

# Determine repository root (parent of scripts folder)
$ScriptDir = $PSScriptRoot
$RepoRoot = (Resolve-Path (Split-Path -Parent $ScriptDir)).Path

# Change to repository root to ensure consistent behavior
Push-Location $RepoRoot

Write-Host "=== Umbraco.Community.AzureSSO Demo Site Setup ===" -ForegroundColor Cyan
Write-Host "Working directory: $RepoRoot" -ForegroundColor Gray
Write-Host ""

# Detect the Umbraco template version to scaffold against. The project multi-targets several
# Umbraco majors at once (net6.0-net10.0); we always demo against the newest one (net10.0), reading
# its Umbraco.Cms.Web.Common version straight out of the csproj so this stays in lockstep with it.
$csprojPath = Join-Path $RepoRoot "src\Umbraco.Community.AzureSSO\Umbraco.Community.AzureSSO.csproj"
if (-not (Test-Path $csprojPath)) {
    Write-Host "ERROR: Could not find $csprojPath" -ForegroundColor Red
    exit 1
}
$csprojContent = Get-Content $csprojPath -Raw
# Requires "==" before net10.0 so this doesn't match the sibling ItemGroup keyed on
# "!= 'net10.0'" (for older TFMs), which also mentions net10.0.
if ($csprojContent -match '(?s)Condition="[^"]*==[^"]*net10\.0[^"]*">(.*?)</ItemGroup>') {
    $net10Block = $matches[1]
} else {
    Write-Host "ERROR: Could not find the net10.0 ItemGroup in $csprojPath" -ForegroundColor Red
    exit 1
}
if ($net10Block -match 'Umbraco\.Cms\.Web\.Common"[^>]*>\s*<Version>([^<]+)</Version>') {
    $TemplateVersion = $matches[1]
} elseif ($net10Block -match 'Umbraco\.Cms\.Web\.Common"\s+Version="([^"]+)"') {
    $TemplateVersion = $matches[1]
} else {
    Write-Host "ERROR: Could not find the Umbraco.Cms.Web.Common version for net10.0 in $csprojPath" -ForegroundColor Red
    exit 1
}
$VersionMajor = [int]($TemplateVersion -split '\.')[0]
$IsTemplatePrerelease = $TemplateVersion -match '-'
Write-Host "Target Umbraco.Cms template version: $TemplateVersion (v$VersionMajor)" -ForegroundColor Gray
Write-Host ""

$DemoDir = "demo"
$DemoSiteName = "Umbraco.Community.AzureSSO.DemoSite"
$DemoSiteDir = "$DemoDir\$DemoSiteName"
$SolutionName = "Umbraco.Community.AzureSSO.local"
$LibraryProject = "src\Umbraco.Community.AzureSSO\Umbraco.Community.AzureSSO.csproj"

# Check if demo already exists
if ((Test-Path $DemoDir) -and -not $Force) {
    Write-Host "Demo folder '$DemoDir' already exists. Use -Force to recreate." -ForegroundColor Yellow
    Write-Host "Or open the existing $SolutionName.slnx" -ForegroundColor Yellow
    exit 0
}

# Clean up existing demo if Force
if ($Force -and (Test-Path $DemoDir)) {
    Write-Host "Removing existing demo folder '$DemoDir'..." -ForegroundColor Yellow
    Remove-Item -Recurse -Force $DemoDir
}

if ($Force -and (Test-Path "$SolutionName.slnx")) {
    Remove-Item -Force "$SolutionName.slnx"
}

# Step 1: Install Umbraco templates
if (-not $SkipTemplateInstall) {
    Write-Host "Installing Umbraco templates ($TemplateVersion)..." -ForegroundColor Green

    # Uninstall any existing version to avoid conflicts
    Write-Host "Removing any existing Umbraco.Templates installations..." -ForegroundColor Gray
    $installedTemplates = dotnet new uninstall 2>&1 | Out-String
    if ($installedTemplates -match "Umbraco\.Templates") {
        try {
            dotnet new uninstall Umbraco.Templates 2>&1 | Out-Null
        } catch {
            # Ignore errors during uninstall
        }
    }

    if ($IsTemplatePrerelease) {
        # Prerelease templates require the umbracoprereleases MyGet feed to be configured.
        # If not yet configured: dotnet nuget add source https://www.myget.org/F/umbracoprereleases/api/v3/index.json --name UmbracoPreReleases
        Write-Host "NOTE: Prerelease template ($TemplateVersion) requires the umbracoprereleases MyGet source." -ForegroundColor Yellow
    }
    dotnet new install "Umbraco.Templates::$TemplateVersion" --force
}

# Step 2: Create the Umbraco demo site
Write-Host "Creating demo folder '$DemoDir'..." -ForegroundColor Green
New-Item -ItemType Directory -Path $DemoDir -Force | Out-Null

Write-Host "Creating Umbraco demo site..." -ForegroundColor Green
Push-Location $DemoDir
dotnet new umbraco --force -n $DemoSiteName --friendly-name "Administrator" --email "admin@example.com" --password "password1234" --development-database-type SQLite
Pop-Location

# Step 3: Add project reference to Umbraco.Community.AzureSSO
Write-Host "Adding project reference to Umbraco.Community.AzureSSO..." -ForegroundColor Green
$demoProject = "$DemoSiteDir\$DemoSiteName.csproj"
dotnet add $demoProject reference $LibraryProject

# Step 4: Add a placeholder AzureSSO config section
# Disabled by default so the demo site boots cleanly without an Entra ID app registration.
# See EntraIDSetup.md and README-v15plus.md for what these values mean and how to fill them in.
Write-Host "Adding placeholder AzureSSO configuration..." -ForegroundColor Green
$devSettingsPath = "$DemoSiteDir\appsettings.Development.json"
$devSettings = Get-Content $devSettingsPath -Raw | ConvertFrom-Json
$azureSsoSettings = [PSCustomObject]@{
    Enabled     = $false
    DisplayName = "Azure AD"
    Credentials = [PSCustomObject]@{
        Instance              = "https://login.microsoftonline.com/"
        Domain                = "REPLACE_WITH_DOMAIN"
        TenantId              = "REPLACE_WITH_TENANT_ID"
        ClientId              = "REPLACE_WITH_CLIENT_ID"
        ClientSecret          = "REPLACE_WITH_CLIENT_SECRET"
        CallbackPath          = "/umbraco-microsoft-signin/"
        SignedOutCallbackPath = "/umbraco-microsoft-signout/"
    }
}
$devSettings | Add-Member -NotePropertyName "AzureSSO" -NotePropertyValue $azureSsoSettings -Force
$devSettings | ConvertTo-Json -Depth 10 | Out-File -FilePath $devSettingsPath -Encoding utf8 -Force

# Step 5: Create unified solution
Write-Host "Creating unified solution..." -ForegroundColor Green
dotnet new sln -n $SolutionName --force
dotnet sln "$SolutionName.slnx" add $LibraryProject --solution-folder "Library"
dotnet sln "$SolutionName.slnx" add $demoProject --solution-folder "Demo"

Write-Host ""
Write-Host "=== Setup Complete! ===" -ForegroundColor Green
Write-Host ""
Write-Host "Solution: $SolutionName.slnx" -ForegroundColor Cyan
Write-Host "Demo site: $DemoSiteDir" -ForegroundColor Cyan
Write-Host ""
Write-Host "Credentials:" -ForegroundColor Yellow
Write-Host "  Email: admin@example.com"
Write-Host "  Password: password1234"
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "  1. Follow EntraIDSetup.md to create an App Registration in Azure"
Write-Host "  2. Fill in the AzureSSO.Credentials values in $DemoSiteDir\appsettings.Development.json"
Write-Host "  3. Set AzureSSO.Enabled to true"
Write-Host "  4. Open $SolutionName.slnx in your IDE, build, and run the $DemoSiteName project"
Write-Host ""

# Restore original directory
Pop-Location
