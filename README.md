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

### Managed Identity / Workload Identity

By default the package authenticates against the App Registration using a client secret. Alternatively you can authenticate using an Azure Managed Identity or a Workload Identity (federated credentials, e.g. on AKS), which removes the need to store a client secret, by setting `CredentialType` in the `Credentials` section:

```
"AzureSSO": {
	"Credentials": {
		"Instance": "https://login.microsoftonline.com/",
		"Domain": "<domain>",
		"TenantId": "<tenantId>",
		"ClientId": "<clientId>",
		"CallbackPath": "/umbraco-microsoft-signin/",
		"SignedOutCallbackPath": "/umbraco-microsoft-signout/",
		"CredentialType": "ManagedIdentity"
	},
	/// All the other configuration
}
```

| Setting                 | Description                                                                                                                                                              |
| ----------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| CredentialType          | How the application authenticates to Entra ID: `Secret` (the default, uses ClientSecret), `ManagedIdentity` or `WorkloadIdentity`                                        |
| ManagedIdentityClientId | Only used when CredentialType is `ManagedIdentity`. Set to the client ID of a user-assigned managed identity, or leave empty to use the system-assigned managed identity |

When `CredentialType` is `ManagedIdentity` or `WorkloadIdentity`, `ClientSecret` is not required and is ignored.

#### Managed Identity

The managed identity is used as a federated identity credential for the App Registration, so the App Registration must be configured to trust it — see [Configure an application to trust a managed identity](https://learn.microsoft.com/en-us/entra/workload-id/workload-identity-federation-config-app-trust-managed-identity). For a user-assigned managed identity set `ManagedIdentityClientId` to the identity's client ID; for a system-assigned managed identity leave it empty.

#### Workload Identity

Workload identity authenticates using the federated token file issued by the Azure Workload Identity webhook. The `AZURE_FEDERATED_TOKEN_FILE` environment variable must be set — on AKS this means the workload identity webhook is enabled and the pod has the `azure.workload.identity/use: "true"` label. The App Registration needs a federated credential trusting the Kubernetes service account, and `TenantId` and `ClientId` must be set explicitly in the configuration. If the environment variable is missing the site will throw an error on startup.

### Debugging

LogUnmappedRolesAsWarning
When SetGroupsOnLogin is set to true, if LogUnmappedRolesAsWarning is also set to true this will log as warning for unmapped Entra ID groups, where the Entra ID name has a slash \ in it. By design it does not log everything to prevent logging of email addresses and so on.
