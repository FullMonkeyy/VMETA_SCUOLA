using System.Collections.Generic;
using Azure;
using Azure.Communication.Email;
using HtmlAgilityPack;
namespace VMETA_1.Classes
{
    public class EmailServiceVMeta
    {
        string connectionString;
        EmailClient emailClient;

        public EmailServiceVMeta()
        {
            connectionString = "endpoint=https://vmetacommunication.europe.communication.azure.com/;accesskey=CWzHFkcklduZSmUH85VZFrUiw9IJcFiYWZosaCmVKBFthNS3FH97JQQJ99AJACULyCp2sVGvAAAAAZCSTWRR";
            emailClient = new EmailClient(connectionString);

        }
        public bool SendEmail(string ogg,string title,string body, string destinationEmail)
        {

            var emailMessage = new EmailMessage(
            senderAddress: "DoNotReply@c76b9799-09e0-4f4e-b5a5-b02536d69e03.azurecomm.net",
            content: new EmailContent(ogg)
            {
                PlainText = body,
                Html = $@"
		        <html>
			        <body>
				        <h1>{title}</h1>
                        <p>{body}<p>
			        </body>
		        </html>"
            },
            recipients: new EmailRecipients(new List<EmailAddress> { new EmailAddress(destinationEmail) }));


            EmailSendOperation emailSendOperation = emailClient.Send( WaitUntil.Started,emailMessage);



            return true;

         }
        
    }
}
