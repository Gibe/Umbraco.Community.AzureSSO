#!/bin/bash
# Demo Site Setup Script
# Creates a local Umbraco site referencing this repo's Umbraco.Community.AzureSSO project,
# for manually testing the SSO login flow during development.

set -e

# Determine repository root (parent of scripts folder)
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" &>/dev/null && pwd )"
REPO_ROOT="$( cd "$SCRIPT_DIR/.." &>/dev/null && pwd )"

# Change to repository root to ensure consistent behavior
cd "$REPO_ROOT" || exit 1

# Parse arguments
SKIP_TEMPLATE_INSTALL=false
FORCE=false

while [[ $# -gt 0 ]]; do
    case $1 in
        --skip-template-install|-s)
            SKIP_TEMPLATE_INSTALL=true
            shift
            ;;
        --force|-f)
            FORCE=true
            shift
            ;;
        --help|-h)
            echo "Usage: $0 [OPTIONS]"
            echo ""
            echo "Options:"
            echo "  -s, --skip-template-install  Skip reinstalling Umbraco.Templates"
            echo "  -f, --force                  Recreate demo if it already exists"
            echo "  -h, --help                   Show this help message"
            exit 0
            ;;
        *)
            echo "Unknown option: $1"
            echo "Use --help for usage information"
            exit 1
            ;;
    esac
done

echo "========================================="
echo "Umbraco.Community.AzureSSO Demo Site Setup"
echo "========================================="
echo "Working directory: $REPO_ROOT"
echo ""

# Detect the Umbraco template version to scaffold against. The project multi-targets several
# Umbraco majors at once (net6.0-net10.0); we always demo against the newest one (net10.0), reading
# its Umbraco.Cms.Web.Common version straight out of the csproj so this stays in lockstep with it.
CSPROJ_PATH="$REPO_ROOT/src/Umbraco.Community.AzureSSO/Umbraco.Community.AzureSSO.csproj"
if [ ! -f "$CSPROJ_PATH" ]; then
    echo "ERROR: Could not find $CSPROJ_PATH" >&2
    exit 1
fi
# Excludes the sibling ItemGroup keyed on "!= 'net10.0'" (for older TFMs), which also mentions net10.0.
NET10_BLOCK=$(awk '/ItemGroup/ && /net10\.0/ && !/!=/{flag=1} flag{print} flag && /<\/ItemGroup>/{exit}' "$CSPROJ_PATH")
if [ -z "$NET10_BLOCK" ]; then
    echo "ERROR: Could not find the net10.0 ItemGroup in $CSPROJ_PATH" >&2
    exit 1
fi
TEMPLATE_VERSION=$(echo "$NET10_BLOCK" | grep -A1 'Umbraco\.Cms\.Web\.Common' | grep -oE '[0-9]+\.[0-9]+\.[0-9]+(-[A-Za-z0-9.]+)?' | head -1)
if [ -z "$TEMPLATE_VERSION" ]; then
    echo "ERROR: Could not find the Umbraco.Cms.Web.Common version for net10.0 in $CSPROJ_PATH" >&2
    exit 1
fi
VERSION_MAJOR=$(echo "$TEMPLATE_VERSION" | cut -d. -f1)
IS_TEMPLATE_PRERELEASE=false
if echo "$TEMPLATE_VERSION" | grep -q '-'; then
    IS_TEMPLATE_PRERELEASE=true
fi
echo "Target Umbraco.Cms template version: $TEMPLATE_VERSION (v$VERSION_MAJOR)"
echo ""

DEMO_DIR="demo"
DEMO_SITE_NAME="Umbraco.Community.AzureSSO.DemoSite"
DEMO_SITE_DIR="${DEMO_DIR}/${DEMO_SITE_NAME}"
SOLUTION_NAME="Umbraco.Community.AzureSSO.local"
LIBRARY_PROJECT="src/Umbraco.Community.AzureSSO/Umbraco.Community.AzureSSO.csproj"

# Check if demo already exists
if [ -d "$DEMO_DIR" ] && [ "$FORCE" = false ]; then
    echo "Demo folder '$DEMO_DIR' already exists. Use --force to recreate."
    echo "Or open the existing ${SOLUTION_NAME}.slnx"
    exit 0
fi

# Clean up existing demo if Force
if [ "$FORCE" = true ] && [ -d "$DEMO_DIR" ]; then
    echo "Removing existing demo folder '$DEMO_DIR'..."
    rm -rf "$DEMO_DIR"
fi

if [ "$FORCE" = true ] && [ -f "${SOLUTION_NAME}.slnx" ]; then
    rm -f "${SOLUTION_NAME}.slnx"
fi

# Step 1: Install Umbraco templates
if [ "$SKIP_TEMPLATE_INSTALL" = false ]; then
    echo "Installing Umbraco templates ($TEMPLATE_VERSION)..."
    # Uninstall any existing version to avoid conflicts
    echo "Removing any existing Umbraco.Templates installations..."
    if dotnet new uninstall 2>&1 | grep -q "Umbraco\.Templates"; then
        dotnet new uninstall Umbraco.Templates 2>/dev/null || true
    fi
    if [ "$IS_TEMPLATE_PRERELEASE" = true ]; then
        # Prerelease templates require the umbracoprereleases MyGet feed to be configured.
        # If not yet configured: dotnet nuget add source https://www.myget.org/F/umbracoprereleases/api/v3/index.json --name UmbracoPreReleases
        echo "NOTE: Prerelease template ($TEMPLATE_VERSION) requires the umbracoprereleases MyGet source."
    fi
    dotnet new install "Umbraco.Templates::${TEMPLATE_VERSION}" --force
fi

# Step 2: Create the Umbraco demo site
echo "Creating demo folder '$DEMO_DIR'..."
mkdir -p "$DEMO_DIR"

echo "Creating Umbraco demo site..."
pushd "$DEMO_DIR" > /dev/null
dotnet new umbraco --force -n "$DEMO_SITE_NAME" --friendly-name "Administrator" --email "admin@example.com" --password "password1234" --development-database-type SQLite
popd > /dev/null

# Step 3: Add project reference to Umbraco.Community.AzureSSO
echo "Adding project reference to Umbraco.Community.AzureSSO..."
DEMO_PROJECT="${DEMO_SITE_DIR}/${DEMO_SITE_NAME}.csproj"
dotnet add "$DEMO_PROJECT" reference "$LIBRARY_PROJECT"

# Step 4: Add a placeholder AzureSSO config section
# Disabled by default so the demo site boots cleanly without an Entra ID app registration.
# See EntraIDSetup.md and README-v15plus.md for what these values mean and how to fill them in.
echo "Adding placeholder AzureSSO configuration..."
DEV_SETTINGS_PATH="${DEMO_SITE_DIR}/appsettings.Development.json"
if command -v jq >/dev/null 2>&1; then
    jq '.AzureSSO = {
        "Enabled": false,
        "DisplayName": "Azure AD",
        "Credentials": {
            "Instance": "https://login.microsoftonline.com/",
            "Domain": "REPLACE_WITH_DOMAIN",
            "TenantId": "REPLACE_WITH_TENANT_ID",
            "ClientId": "REPLACE_WITH_CLIENT_ID",
            "ClientSecret": "REPLACE_WITH_CLIENT_SECRET",
            "CallbackPath": "/umbraco-microsoft-signin/",
            "SignedOutCallbackPath": "/umbraco-microsoft-signout/"
        }
    }' "$DEV_SETTINGS_PATH" > "${DEV_SETTINGS_PATH}.tmp"
    mv "${DEV_SETTINGS_PATH}.tmp" "$DEV_SETTINGS_PATH"
else
    # jq isn't guaranteed to be installed; fall back to a plain text insert. This relies on the
    # scaffolded appsettings.Development.json being a normal single top-level JSON object, which is
    # what `dotnet new umbraco` produces.
    echo "  (jq not found, falling back to a plain text insert)"
    CONTENT=$(cat "$DEV_SETTINGS_PATH")
    TRIMMED="${CONTENT%\}}"
    TRIMMED="${TRIMMED%"${TRIMMED##*[![:space:]]}"}"
    {
        printf '%s' "$TRIMMED"
        cat <<'JSON_EOF'
,
  "AzureSSO": {
    "Enabled": false,
    "DisplayName": "Azure AD",
    "Credentials": {
      "Instance": "https://login.microsoftonline.com/",
      "Domain": "REPLACE_WITH_DOMAIN",
      "TenantId": "REPLACE_WITH_TENANT_ID",
      "ClientId": "REPLACE_WITH_CLIENT_ID",
      "ClientSecret": "REPLACE_WITH_CLIENT_SECRET",
      "CallbackPath": "/umbraco-microsoft-signin/",
      "SignedOutCallbackPath": "/umbraco-microsoft-signout/"
    }
  }
}
JSON_EOF
    } > "${DEV_SETTINGS_PATH}.tmp"
    mv "${DEV_SETTINGS_PATH}.tmp" "$DEV_SETTINGS_PATH"
fi

# Step 5: Create unified solution
echo "Creating unified solution..."
dotnet new sln -n "$SOLUTION_NAME" --force
dotnet sln "${SOLUTION_NAME}.slnx" add "$LIBRARY_PROJECT" --solution-folder "Library"
dotnet sln "${SOLUTION_NAME}.slnx" add "$DEMO_PROJECT" --solution-folder "Demo"

echo ""
echo "========================================="
echo "Setup Complete!"
echo "========================================="
echo ""
echo "Solution: ${SOLUTION_NAME}.slnx"
echo "Demo site: $DEMO_SITE_DIR"
echo ""
echo "Credentials:"
echo "  Email: admin@example.com"
echo "  Password: password1234"
echo ""
echo "Next steps:"
echo "  1. Follow EntraIDSetup.md to create an App Registration in Azure"
echo "  2. Fill in the AzureSSO.Credentials values in $DEV_SETTINGS_PATH"
echo "  3. Set AzureSSO.Enabled to true"
echo "  4. Open ${SOLUTION_NAME}.slnx in your IDE, build, and run the $DEMO_SITE_NAME project"
echo ""
