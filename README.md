# Umbraco.Community.AzureSSO

Add Azure Entra ID SSO to Umbraco v10+ sites. This will allow you to automatically create Umbraco user accounts for users in your Microsoft Entra ID tenant. This will then associate the Umbraco users with groups based on their group, and the configuration below.

First you, or an Entra ID administrator will need to create an App Registration in the Azure Portal which will be used to authenticate the site against Azure AD. Follow [these instructions to setup the new App Registration](EntraIDSetup.md)

To install:

`dotnet add package Umbraco.Community.AzureSSO`

## Advanced usage

### Setup

Select the instructions for your Umbraco version

[v10-12](README-uptov12.md)
[v13](README-v13.md)
[v14+](README-v14plus.md)

### Manually composing

If you'd like to disable the composer and configure it yourself you can add DisableComposer to the settings and set it's value to true

i.e.

"AzureSSO": {
	/// All the other configuration
	"DisableComposer": true
}

In which case you'll need to add AddMicrosoftAccountAuthentication() to your ConfigureServices function

##

LogUnmappedRolesAsWarning
When SetGroupsOnLogin is set to true, if LogUnmappedRolesAsWarning is also set to true this will log as warning for unmapped Entra ID groups, where the Entra ID name has a slash \ in it. Be design it does not log everything to prevent logging of email addresses and so on.