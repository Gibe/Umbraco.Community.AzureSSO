# Our.Umbraco.AzureSSO
Add Azure AD SSO to Umbraco

Uses autolinking to create user accounts in Umbraco and then associate them with groups based on their AD group.

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

DisplayName - The display name for use on the login button
DenyLocalLogin - Allow users to login via Umbraco's standard login
GroupBindings - The bindings for AD group to Umbraco group

TODO Instructions on configuring App Registration in Azure


