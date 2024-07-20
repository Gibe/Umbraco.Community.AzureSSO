# Umbraco.Community.AzureSSO

Add Azure Entra ID SSO to Umbraco v10-11 sites. This will allow you to automatically create Umbraco user accounts for users in your Microsoft Entra ID tenant. This will then associate the Umbraco users with groups based on their group, and the configuration below.

First you, or an Entra ID administrator will need to create an App Registration in the Azure Portal which will be used to authenticate the site against Azure AD. Follow [these instructions to setup the new App Registration](EntraIDSetup.md)

To install:

`dotnet add package Umbraco.Community.AzureSSO`

You'll need to configure the package by adding the following section to the root of your appsettings.json file and customise as appropriate

```
"AzureSSO": {
    "Credentials": {
        "Instance": "https://login.microsoftonline.com/",
        "Domain": "<domain>",
        "TenantId": "<tenantId>",
        "ClientId": "<clientId>",
        "CallbackPath": "/umbraco-microsoft-signin/",
        "SignedOutCallbackPath ": "/umbraco-microsoft-signout/",
        "ClientSecret": "<clientSecret>"
    },
    "DisplayName": "Azure AD",
    "DenyLocalLogin": true,
    "AutoRedirectLoginToExternalProvider": true,
    "TokenCacheType": "InMemory",
    "GroupBindings": {
        "<AD group>": "<umbraco group>",
        "<another AD group>": "<umbraco group>"
    },
    "SetGroupsOnLogin": true,
    "DefaultGroups": [
		"editor"
	],
    "Icon": "fa fa-lock",
    "ButtonStyle": "btn-microsoft",
},
```
On Umbraco v13+ change the `Icon` to `"icon-microsoft-fill"`, i.e. `"Icon": "icon-microsoft-fill",`

You'll need to configure these settings based on the values in Azure:

| Setting          | Description                                                              |
| ---------------- | ------------------------------------------------------------------------ |
| Domain           | The value in Primary domain in the Microsoft Entra ID Directory Overview |
| TenantId         | The value in Directory (tenant) ID on the App Registration Overview      |
| ClientId         | The value in Application (Client) ID on the App Registration Overview    |
| ClientSecret     | The client secret created for the App Registration                       |

You can also customise the configuration by setting these settings:

| Setting                               | Description                                                                                  |
| ------------------------------------- | -------------------------------------------------------------------------------------------- |
| DisplayName                           | The display name for use on the login button                                                 |
| Icon                                  | The class name for the icon to use                                                           |
| ButtonStyle                           | The class name for the button style                                                          |
| AutoRedirectLoginToExternalProvider   | Automatically redirect to the external login provider                                        |
| DenyLocalLogin                        | Allow users to login via Umbraco's standard login                                            |
| GroupBindings                         | The bindings for Entry ID group to Umbraco group                                             |
| SetGroupsOnLogin                      | Whether or not to reset the users assigned groups on each login                              |
| TokenCacheType                        | Token Cache Type to use: InMemory, Session, Distributed (defaults to InMemory)               |
| DefaultGroups                         | The groups to assign to users regardless of any Entry ID groups assigned (defaults to none)  |

## ButtonStyle

The standard set is shown at https://lipis.github.io/bootstrap-social/

## Group Bindings

To bind these you'll need to specify the Entra ID group and then the matching Umbraco group.

For example we use: `"GIBE\Producers" : "editors"` to bind everyone in the `GIBE\Producers` group to the Umbraco editors group using the Umbraco Alias for the group.  The Umbraco Alias for a group is displayed to the right of the group name in the User Group editor view.

Beware these will be reset on each login, so changing groups in umbraco will only take effect until the user next logs in. If a user is removed from an AD group they'll automatically be removed from the matching Umbraco group on next login.

## Alternate Group Bindings using Group Id

If you are having problems with NET BIOS group names, you can set the groups claim in the App Registration to use Group Id rather than NetBIOSDomain\sAMAccountName

![image](https://user-images.githubusercontent.com/1257752/235880241-4381fa6e-d72d-494e-bd3f-1ceaf8b4e71d.png)

You can now use the guid format for the Group Id like: `"xxxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx": "admin", "44a38651-xxxx-4c92-b1b6-51cf26ff9bab": "editor"`





