# Sitecore-Forms-Cloud-Upload

Use Azure Storage Queue and Azure Storage Blob to store Sitecore 9.3+ Forms uploads Encrypted, with a key from Azure Key Fault

What you can do is process the message on the queue with for example an Azure function, get the related uploaded files, process the message and files. And delete the sensitive files and remove the message from the queue. The message on the queue is now not encrypted. If the information is very sensitive you can also encrypted that.

# Quick Azure Setup (for local development)

## Create an Azure storage
For Storing the uploaded files and tempory storing the Firm values
- Goto Access keys
- Copy the Connection string use it for Stockpick.Forms.AzureQueue.Connectionstring ideally you set this also I key vault for now just as easy to quickly have a local dev environment.

No necessary to create queue or blob storage the code will create one one the first use.

## Create an Application
you use this to authenticate instead of a user.
- Go to App-registraties
- Create a new app,
- Create a new application secret
- Copy you secret value this value do you need for the setting Stockpick.Forms.KeyFault.ClientSecret
- Goto overview of the app, and copy the Applicaton (client-id) Use it for the setting Stockpick.Forms.KeyFault.ClientId

SEE https://docs.microsoft.com/en-us/azure/active-directory/develop/howto-create-service-principal-portal for more info about creating an Service principal,

##Create a Key vault
For storing the secret decryption key
- Create a Key with name sitecoreformupload, RSA is fine.
- Goto Access policies of your Key vault, and add your app give Key Permissions GET

# Sitecore setup, Sitecore 9.3+
- Compile
- Deploy
- Unicorn sync
- Config \App_Config\Include\Stockpick.Form.Cloud.config
- Create a Form
- Add the new save action to your Form

It also contains a Decrypt controller but that is more for the purpose to demo how to encrypt a file, Note in this example all files are encrypted with the same key. So still possible to easy get all uploaded files if you have the key. 