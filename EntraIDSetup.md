# Setting up Entra ID to work with the module

In the Azure portal under your Microsoft Entra ID tenant, select App Registrations

![App Registrations Portal View](https://user-images.githubusercontent.com/113788/228666546-633e434a-4466-4f7c-9a6b-666751aae7bc.png)

Click create a new registration

Give that application a meaningful name and a Redirect URI with a type of web and the URL of your site followed by `/umbraco-microsoft-signin/`

![Add a new registration page](https://user-images.githubusercontent.com/113788/228667106-f52831f4-fff9-4ae0-a5ef-dc468a4c898c.png)

Within that application registration select Authentication and ensure Access Tokens is checked

![Authentication section within the App Registration](https://user-images.githubusercontent.com/113788/228667598-4f8243c6-5197-49c0-b909-ca61782e7d03.png)

Then under Certificates and Secrets select + New client secret and create a new secret. Copy the value of the new secret and save that for inserting into the appSettings.json later. This will go into the value for `ClientSecret`

Under Token configuration, select Add optional claimm then select ID and check email, when it asks agree to the permissions request

![image](https://user-images.githubusercontent.com/113788/228668304-5f0a7671-c1f7-4ac1-bfd0-a8157d6a843c.png)

Then select Add groups claim and check Security groups, select `NetBIOSDomain\sAMAccountName` under each of ID, Access and SAML

![image](https://user-images.githubusercontent.com/113788/228668899-0aad22a1-ca4e-48e0-8554-d44acf344dd5.png)

It should now look something like this:

![image](https://user-images.githubusercontent.com/113788/228669167-7d07e49a-65e2-439f-b4de-e39e7c6f903f.png)

The API permissions tab should look like this, no changes are required

![image](https://user-images.githubusercontent.com/113788/228669355-651e462a-b276-4743-904c-c0ad6876b4c1.png)



