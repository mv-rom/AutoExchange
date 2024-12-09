using System;
using System.Net;
using System.Net.Mail;


namespace ae.lib
{
    public class EmailInformer
    {
        public static EmailInformer Instance = null;
        private SmtpClient mySmtpClient;
        private MailAddress From;
        private MailAddress To;


        public static EmailInformer getInstance()
        {
            if (EmailInformer.Instance == null) {
                EmailInformer.Instance = new EmailInformer();
                if (EmailInformer.Instance.Init() != true) EmailInformer.Instance.deInit();
            }
            return EmailInformer.Instance;
        }

        public bool Init()
        {
            bool result = false;
            string hMsg = "Error in " + this.GetType().Name + ".Init(): ";
            try
            {
                var EI = Base.Config.ConfigSettings.EmailInformer;
                if (EI == null && EI.Count <= 0) {
                    Base.Log(hMsg + "EmailInformer isn't configured!");
                    return false;
                }

                string Enable = "false";
                if (!EI.TryGetValue("Enable", out Enable)) {
                    Enable = "false";
                }
                //Enable Informer only with true
                if (bool.Parse(Enable) != true) return false;

                string ServerHost = "";
                if (!EI.TryGetValue("ServerHost", out ServerHost)) {
                    Base.Log(hMsg + "Hasn't found ServerHost in settings of configuration!");
                    return false;
                }

                string ServerPort = "";
                if (!EI.TryGetValue("ServerPort", out ServerPort))
                {
                    Base.Log(hMsg + "Hasn't found ServerPort in settings of configuration!");
                    return false;
                }

                string ServerSSLEnable = "true";
                if (!EI.TryGetValue("ServerSSLEnable", out ServerSSLEnable))
                {
                    Base.Log(hMsg + "Hasn't found ServerSSLEnable in settings of configuration!");
                    return false;
                }

                string ServerUser = "";
                if (!EI.TryGetValue("ServerUser", out ServerUser))
                {
                    Base.Log(hMsg + "Hasn't found ServerUser in settings of configuration!");
                    return false;
                }

                string ServerPassword = "";
                if (!EI.TryGetValue("ServerPassword", out ServerPassword))
                {
                    Base.Log(hMsg + "Hasn't found ServerPassword in settings of configuration!");
                    return false;
                }

                string SourceEmailAddress = "";
                if (!EI.TryGetValue("SourceEmailAddress", out SourceEmailAddress))
                {
                    Base.Log(hMsg + "Hasn't found SourceEmailAddress in settings of configuration!");
                    return false;
                }

                string SourceAddressInfo = "";
                if (!EI.TryGetValue("SourceAddressInfo", out SourceAddressInfo))
                {
                    Base.Log(hMsg + "Hasn't found SourceAddressInfo in settings of configuration!");
                    return false;
                }
                this.From = new MailAddress(SourceEmailAddress, SourceAddressInfo);

                string DestinationEmailAddress = "";
                if (!EI.TryGetValue("DestinationEmailAddress", out DestinationEmailAddress))
                {
                    Base.Log(hMsg + "Hasn't found DestinationEmailAddress in settings of configuration!");
                    return false;
                }

                string DestinationAddressInfo = "";
                if (!EI.TryGetValue("DestinationAddressInfo", out DestinationAddressInfo))
                {
                    Base.Log(hMsg + "Hasn't found DestinationAddressInfo in settings of configuration!");
                    return false;
                }
                this.To = new MailAddress(DestinationEmailAddress, DestinationAddressInfo);

                this.mySmtpClient = new SmtpClient(ServerHost)
                {
                    EnableSsl = bool.Parse(ServerSSLEnable),
                    Port = int.Parse(ServerPort),
                    // set smtp-client with basicAuthentication
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(ServerUser, ServerPassword)
                };
                Base.Log(this.GetType().Name + ".Init() is done.");
                result = true;
            }
            catch (Exception ex) {
                Base.LogError(hMsg + ex.Message, ex);
            }
            return result;
        }

        public void deInit()
        {
            if (this.mySmtpClient != null) {
                this.mySmtpClient.Dispose();
            }
            EmailInformer.Instance = null;
        }

        public void SendMsg(string Subject, string Message, string AttachmentFilePath)
        {
            string hMsg = "Error in " + this.GetType().Name + ".SendMsg(): ";

            if (this.mySmtpClient == null) {
                Base.Log(hMsg + "Not initialized mySmtpClient()!");
                return;
            }

            try
            {
                using (var myMail = new MailMessage())
                {
                    myMail.From = this.From;
                    //add recipient
                    myMail.To.Add(this.To);

                    if (AttachmentFilePath.Length > 0)
                    {
                        string[] afp = AttachmentFilePath.Split(',');
                        foreach (var a in afp)
                        {
                            a.Trim();
                            if (a.Length > 0 && System.IO.File.Exists(a))
                            {
                                myMail.Attachments.Add(new Attachment(a));
                            }
                        }
                    }

                    // set subject and encoding
                    myMail.Subject = Subject; // "EDI Service Informer";
                    myMail.SubjectEncoding = System.Text.Encoding.UTF8;

                    // set body-message and encoding
                    myMail.Body = "<h2>AutoExchange Informer</h2><br>" + Message + "<b>HTML</b>.";
                    myMail.BodyEncoding = System.Text.Encoding.UTF8;
                    // text or html
                    myMail.IsBodyHtml = true;

                    this.mySmtpClient.SendAsync(myMail, null);
                }
            }
            catch (Exception ex)
            {
                Base.LogError(hMsg + ex.Message, ex);
            }
        }
    }
}
