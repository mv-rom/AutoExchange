using System;
using System.Collections;
using System.IO;
//using System.Text;
//using System.Threading.Tasks;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
//using System.Net.Http;
using System.Text;
//using System.Security.Cryptography;
//using System.Security.Policy;

//http s://aws.amazon.com/ru/what-is/restful-api/



namespace ae.lib
{
    internal class RestApiClient
    {
        private string BaseUrl;
        private string Authorization;
        private string ContentType;
        private int Timeout;

        public void Init(string BaseUrl, string Authorization, string ContentType="application/json", int Timeout=10000)
        {
            this.BaseUrl = BaseUrl;
            this.Authorization = Authorization;
            this.ContentType = ContentType;
            this.Timeout = Timeout;
        }

        private bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        private string HTTPClient(string Method, string SubUrl, string Arguments, string rawData="")
        {
            var final_url = this.BaseUrl;
            if (SubUrl.Length > 0) {
                if (final_url.EndsWith("/"))
                    final_url += (SubUrl.StartsWith("/")) ? SubUrl.Substring(1) : SubUrl;
                else
                    final_url += (SubUrl.StartsWith("/")) ? SubUrl : "/" + SubUrl;
            }
            if (Arguments.Length > 0)
                final_url += "?" + Arguments;
            string rawResponse = string.Empty;
            int rescode = 0;

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Ssl3;
            //ServicePointManager.ServerCertificateValidationCallback += 
            //    new System.Net.Security.RemoteCertificateValidationCallback(this.ValidateServerCertificate);


            HttpWebRequest webrequest = (HttpWebRequest)WebRequest.Create(Uri.EscapeUriString(final_url));
            webrequest.Method = Method;
            webrequest.Timeout = this.Timeout;
            webrequest.Accept = @"*/*";
            webrequest.Headers["UserAgent"] = "AutoExchange";
            webrequest.Headers["Cache-Control"] = "no-cache";
            webrequest.CachePolicy = new System.Net.Cache.RequestCachePolicy(
                System.Net.Cache.RequestCacheLevel.NoCacheNoStore
            );
            webrequest.Proxy = null;
            webrequest.Credentials = CredentialCache.DefaultCredentials;
            webrequest.ContentType = this.ContentType + "; charset=utf-8";

            // Set some reasonable limits on resources used by this request
            webrequest.MaximumAutomaticRedirections = 3;
            //webrequest.MaximumResponseHeadersLength = 3;

            if (this.Authorization.Length > 0)
                webrequest.Headers["Authorization"] = this.Authorization;

            if (rawData.Length > 0) {
                byte[] data = Encoding.UTF8.GetBytes(rawData);
                webrequest.ContentLength = data.Length;

                //add Data to Request
                using (var dataStream = webrequest.GetRequestStream())
                {
                    dataStream.Write(data, 0, data.Length);
                }
            }


            //receive response
            HttpWebResponse webresponse = null;
            try
            {
                webresponse = (HttpWebResponse)webrequest.GetResponse();
                var responseStream = webresponse.GetResponseStream();
                if (responseStream != null) {
                    using (var reader = new StreamReader(responseStream))
                    {
                        rawResponse = reader.ReadToEnd();
                    }
                }
                rescode = (int)((HttpWebResponse)webresponse).StatusCode;
            }
            catch (WebException ex)
            {
                if (ex.Status == WebExceptionStatus.ProtocolError) {
                    Base.LogError("ResponseError in " + this.GetType().Name + ".HTTPClient(): " + ex.Message);
                    Base.LogError("ResponseError Status: " + (int)((HttpWebResponse)ex.Response).StatusCode);
                    var s = ((HttpWebResponse)ex.Response).GetResponseStream();
                    using (var reader = new StreamReader(s))
                    {
                        Base.LogError("ResponseError Data: " + reader.ReadToEnd());
                        reader.Close();
                    }
                } else {
                    Base.LogError("ResponseError in " + this.GetType().Name + ".HTTPClient(): " + ex.Message, ex);
                }
            }
            catch (Exception ex)
            {
                Base.LogError("Error in " + this.GetType().Name + ".HTTPClient(): " + ex.Message);
            }
            finally
            {
                if (webresponse != null) webresponse.Close();
            }
            return rawResponse;
        }

        public string GET(string url, string args)
        {
            return this.HTTPClient(WebRequestMethods.Http.Get, url, args);
        }

        public string POST(string url, string args, string data)
        {
            return this.HTTPClient(WebRequestMethods.Http.Post, url, args, data);
        }

        public string PUT(string url, string args, string data)
        {
            return this.HTTPClient(WebRequestMethods.Http.Put, url, args, data);
        }

        /*
            protected static string Sha256(string signString)
            {
                var sha2 = new SHA256Managed();
                byte[] hash = sha2.ComputeHash(System.Text.Encoding.UTF8.GetBytes(signString));

                StringBuilder sb = new StringBuilder(40);
                foreach (byte b in hash)
                {
                    sb.AppendFormat("{0:x2}", b);
                }
                return sb.ToString();
            }
        */
    }
}
