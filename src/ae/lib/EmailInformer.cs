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
            string hMsg = "Error in " + this.GetType().Name + ".Init(): ";
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
                    Base.Log(hMsg + "Hasn't found ServerHost in settings of configuration!");
                    return false;
                }

                string ServerPort = "";
                if (!Base.Config.ConfigSettings.EmailInformer.TryGetValue("ServerPort", out ServerPort))
                {
                    Base.Log(hMsg + "Hasn't found ServerPort in settings of configuration!");
                    return false;
                }

                string ServerSSLEnable = "true";
                if (!Base.Config.ConfigSettings.EmailInformer.TryGetValue("ServerSSLEnable", out ServerSSLEnable))
                {
                    Base.Log(hMsg + "Hasn't found ServerSSLEnable in settings of configuration!");
                    return false;
                }

                string ServerUser = "";
                if (!Base.Config.ConfigSettings.EmailInformer.TryGetValue("ServerUser", out ServerUser))
                {
                    Base.Log(hMsg + "Hasn't found ServerUser in settings of configuration!");
                    return false;
                }

                string ServerPassword = "";
                if (!Base.Config.ConfigSettings.EmailInformer.TryGetValue("ServerPassword", out ServerPassword))
                {
                    Base.Log(hMsg + "Hasn't found ServerPassword in settings of configuration!");
                    return false;
                }

                string SourceEmailAddress = "";
                if (!Base.Config.ConfigSettings.EmailInformer.TryGetValue("SourceEmailAddress", out SourceEmailAddress))
                {
                    Base.Log(hMsg + "Hasn't found SourceEmailAddress in settings of configuration!");
                    return false;
                }

                string SourceAddressInfo = "";
                if (!Base.Config.ConfigSettings.EmailInformer.TryGetValue("SourceAddressInfo", out SourceAddressInfo))
                {
                    Base.Log(hMsg + "Hasn't found SourceAddressInfo in settings of configuration!");
                    return false;
                }

                string DestinationEmailAddress = "";
                if (!Base.Config.ConfigSettings.EmailInformer.TryGetValue("DestinationEmailAddress", out DestinationEmailAddress))
                {
                    Base.Log(hMsg + "Hasn't found DestinationEmailAddress in settings of configuration!");
                    return false;
                }

                string DestinationAddressInfo = "";
                if (!Base.Config.ConfigSettings.EmailInformer.TryGetValue("DestinationAddressInfo", out DestinationAddressInfo))
                {
                    Base.Log(hMsg + "Hasn't found DestinationAddressInfo in settings of configuration!");
                    return false;
                }


                this.mySmtpClient = new SmtpClient(ServerHost) {
                    EnableSsl = bool.Parse(ServerSSLEnable),
                    Port = int.Parse(ServerPort),
                    // set smtp-client with basicAuthentication
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(ServerUser, ServerPassword)
                };

                if (this.mySmtpClient != null) {
                    this.myMail = new MailMessage() {
                        From = new MailAddress(SourceEmailAddress, SourceAddressInfo)
                    };

                    //add recipient
                    this.myMail.To.Add(new MailAddress(DestinationEmailAddress, DestinationAddressInfo));
                    result = true;
                }
            }
            catch (Exception ex)
            {
                Base.LogError(hMsg + ex.Message, ex);
                result = false;
            }
            return result;
        }

        public void DeInit()
        {
            if (this.myMail != null) {
                this.myMail.Dispose();
            }

            if (this.mySmtpClient != null) {
                this.mySmtpClient.Dispose();
            }
            EmailInformer.Instance = null;
        }

        public void SendMsg(string Subject, string Message, string AttachmentFilePath)
        {
            try
            {
                if (AttachmentFilePath.Length > 0) {
                    string[] afp = AttachmentFilePath.Split(',');
                    foreach (var a in afp) {
                        a.Trim();
                        if (a.Length > 0 && System.IO.File.Exists(a)) {
                            this.myMail.Attachments.Add(new Attachment(a));
                        }
                    }
                }

                // set subject and encoding
                this.myMail.Subject = Subject; // "EDI Service Informer";
                this.myMail.SubjectEncoding = System.Text.Encoding.UTF8;

                // set body-message and encoding
                myMail.Body = "<h2>AutoExchange Informer</h2><br>" + Message + "<b>HTML</b>.";
                myMail.BodyEncoding = System.Text.Encoding.UTF8;
                // text or html
                myMail.IsBodyHtml = true;

                this.mySmtpClient.SendAsync(this.myMail, null);
            }
            catch (Exception ex)
            {
                Base.LogError(ex.Message, ex);
            }
        }
    }
}
