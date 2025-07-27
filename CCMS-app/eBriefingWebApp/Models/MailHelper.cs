using System;
using System.Net.Mail;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace eBriefingWebApp.Models
{
    public class MailHelper
    {
        public static bool SendMail(string SMTPServer, string EmailUsername, string EmailFrom, string EmailFromPass, string EmailTo, string Subject,
string Body)
        {
            try
            {
                SmtpClient SmtpServer = new SmtpClient();
                System.Net.Mail.MailMessage mail = new System.Net.Mail.MailMessage();
                //SmtpServer.Credentials = CredentialCache.DefaultNetworkCredentials;
                //SmtpServer.Credentials = new System.Net.NetworkCredential(EmailUsername, EmailFromPass);
                SmtpServer.Port = 25;
                SmtpServer.Host = SMTPServer;
                //SmtpServer.EnableSsl = true;
                mail = new System.Net.Mail.MailMessage();
                mail.From = new MailAddress(EmailFrom);
                mail.To.Add(EmailTo);
                mail.Subject = Subject;
                mail.IsBodyHtml = true;
                mail.Body = Body;

                System.Net.ServicePointManager.ServerCertificateValidationCallback = validateCert;
                SmtpServer.Send(mail);

                return true;
            }
            catch (Exception ex)
            {

            }

            return false;
        }

        public static bool validateCert(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }


    }
}