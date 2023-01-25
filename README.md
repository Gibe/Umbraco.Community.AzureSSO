# Our.Umbraco.AzureSSO
Add Azure AD SSO to Umbraco v11+ sites. This will allow you to automatically create Umbraco user accounts for users in your AD. This will then associate the Umbraco users with groups based on their AD group, and the configuration below.

To install
`dotnet add package Our.Umbraco.AzureSSO`

To configure add the following section to the root of your appsettings.json file and customise as appropriate
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
    "GroupBindings": {
        "<AD group>": "<umbraco group>",
        "<another AD group>": "<umbraco group>"
    }
},
```
TODO Instructions on configuring App Registration in Azure and populating the credentials section

DisplayName - The display name for use on the login button
DenyLocalLogin - Allow users to login via Umbraco's standard login
GroupBindings - The bindings for AD group to Umbraco group




