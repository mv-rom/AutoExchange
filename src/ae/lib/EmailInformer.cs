using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Net;
using System.Net.Mail;
using System.Net.Mime;


namespace ae.lib
{
    internal class EmailInformer
    {
        private static EmailInformer Instance = null;

        private SmtpClient mySmtpClient;
        private MailMessage myMail;



        public static EmailInformer getInstance()
        {
            if (EmailInformer.Instance == null) {
                EmailInformer.Instance = new EmailInformer();
                if (EmailInformer.Instance.Init() != true) EmailInformer.Instance.DeInit();
            }
            return EmailInformer.Instance;
        }

        public bool Init()
        {
            bool result = false;
            try
            {
                string Enable = "false";
                if (!Base.Config.ConfigSettings.EmailInformer.TryGetValue("Enable", out Enable)) {
                    Enable = "false";
                }
                //Enable Informer only with true
                if (bool.Parse(Enable) != true) return false;

                string ServerHost = "";
                if (!Base.Config.ConfigSettings.EmailInformer.TryGetValue("ServerHost", out ServerHost)) {
                    string msg = "Error in EmailInformer.Init(): Hasn't found ServerHost in settings of configuration!";
                    throw new Exception(msg);
                }

                string ServerPort = "";
                string ServerSSLEnable = "";
                string ServerUser = "";
                string ServerPassword = "";
                string SourceEmailAddress = "";
                string SourceAddressInfo = "";
                string DestinationEmailAddress = "";
                string DestinationAddressInfo = "";


                this.mySmtpClient = new SmtpClient(ServerHost);
                if (this.mySmtpClient != null)
                {
                    this.mySmtpClient.EnableSsl = bool.Parse(ServerSSLEnable);
                    this.mySmtpClient.Port = int.Parse(ServerPort);

                    // set smtp-client with basicAuthentication
                    this.mySmtpClient.UseDefaultCredentials = false;
                    var basicAuthenticationInfo = new NetworkCredential(ServerUser, ServerPassword);
                    this.mySmtpClient.Credentials = basicAuthenticationInfo;

                    // add from,to mailaddresses
                    var from = new MailAddress(SourceEmailAddress, SourceAddressInfo);
                    var to =   new MailAddress(DestinationEmailAddress, DestinationAddressInfo);
                    this.myMail = new MailMessage(from, to);

                    // add ReplyTo
                    //var replyTo = new MailAddress("reply@example.com");
                    //myMail.ReplyToList.Add(replyTo);

                    result = true;
                }
            }
            catch (Exception ex)
            {
                Base.LogError("Error in " + this.GetType().Name + ".Init(): " + ex.Message, ex);
                result = false;
            }
            return result;
        }

        public void DeInit()
        {
            EmailInformer.Instance = null;
        }

        public void SendMsg(string Subject, string msg)
        {
            try
            {
                // set subject and encoding
                this.myMail.Subject = Subject; // "EDI Service Informer";
                this.myMail.SubjectEncoding = System.Text.Encoding.UTF8;

                // set body-message and encoding
                myMail.Body = "<b>AutoExchange Informer</b><br>"+ msg + "<b>HTML</b>.";
                myMail.BodyEncoding = System.Text.Encoding.UTF8;
                // text or html
                myMail.IsBodyHtml = true;

                this.mySmtpClient.Send(myMail);
            }
            catch (SmtpException ex)
            {
                throw new ApplicationException
                  ("SmtpException has occured: " + ex.Message);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
