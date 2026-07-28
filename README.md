# Umbraco.Community.AzureSSO

Add Azure Entra ID SSO to Umbraco v10+ sites. This will allow you to automatically create Umbraco user accounts for users in your Microsoft Entra ID tenant. This will then associate the Umbraco users with groups based on their group, and the configuration below.

First you, or an Entra ID administrator will need to create an App Registration in the Azure Portal which will be used to authenticate the site against Azure AD. Follow [these instructions to setup the new App Registration](EntraIDSetup.md)

To install:

`dotnet add package Umbraco.Community.AzureSSO`

### Setup

Select the instructions for your Umbraco version

[v10-12](README-uptov12.md)
[v13](README-v13.md)
[v15+](README-v15plus.md)

## Local development

To spin up a local Umbraco site for manually testing changes to this package, run one of the demo site scripts from the repo root:

```
./scripts/install-demo-site.ps1   # Windows/PowerShell
./scripts/install-demo-site.sh    # macOS/Linux/bash
```

This scaffolds a new Umbraco site under `demo/`, references it against the `Umbraco.Community.AzureSSO` project in `src/` (so your local changes are picked up directly, no need to pack/publish), and creates `Umbraco.Community.AzureSSO.local.slnx` combining both projects for convenience. The `AzureSSO` configuration section is added to the demo site's `appsettings.Development.json` disabled by default, with placeholder `REPLACE_WITH_*` values.

To use it:

1. Follow [EntraIDSetup.md](EntraIDSetup.md) to create an App Registration in Azure
2. Fill in the `AzureSSO.Credentials` values in `demo/Umbraco.Community.AzureSSO.DemoSite/appsettings.Development.json`
3. Set `AzureSSO.Enabled` to `true`
4. Open `Umbraco.Community.AzureSSO.local.slnx`, build, and run the `Umbraco.Community.AzureSSO.DemoSite` project

Both scripts accept `-Force`/`--force` to recreate the demo site from scratch, and `-SkipTemplateInstall`/`--skip-template-install` to skip reinstalling the Umbraco templates on repeat runs. The demo site and local solution file are gitignored.

## Advanced usage

### Manually composing

If you'd like to disable the composer and configure it yourself you can add DisableComposer to the settings and set it's value to true

i.e.
```
"AzureSSO": {
	/// All the other configuration
	"DisableComposer": true
}
```

In which case you'll need to add AddMicrosoftAccountAuthentication() to your ConfigureServices function

### Debugging

LogUnmappedRolesAsWarning
When SetGroupsOnLogin is set to true, if LogUnmappedRolesAsWarning is also set to true this will log as warning for unmapped Entra ID groups, where the Entra ID name has a slash \ in it. By design it does not log everything to prevent logging of email addresses and so on.
