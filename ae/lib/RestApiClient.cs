using System;
using System.IO;
//using System.Text;
//using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
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


            HttpWebRequest webrequest = (HttpWebRequest)WebRequest.Create(final_url);
            webrequest.Method = Method;
            webrequest.Timeout = this.Timeout;
            webrequest.Accept = @"*/*";
            webrequest.ContentType = this.ContentType;
            webrequest.Headers["UserAgent"] = "SimpleHostClient";
            webrequest.Headers["Cache-Control"] = "no-cache";

            if (this.Authorization.Length > 0)
                webrequest.Headers["Authorization"] = this.Authorization;

            if (rawData.Length > 0) {
                byte[] data = Encoding.UTF8.GetBytes(rawData);
                webrequest.ContentLength = data.Length;

                //add Data to Request
                var req_s = webrequest.GetRequestStream();
                req_s.Write(data, 0, data.Length);
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
                Base.LogError("ResponseError in " + this.GetType().Name + ".HTTPClient(): " + ex.Message);
                if (ex.Status == WebExceptionStatus.ProtocolError) {
                    Base.LogError("ResponseError Status: " + (int)((HttpWebResponse)ex.Response).StatusCode);
                    var s = ((HttpWebResponse)ex.Response).GetResponseStream();
                    using (var reader = new StreamReader(s))
                    {
                        Base.LogError("ResponseError Data: " + reader.ReadToEnd());
                        reader.Close();
                    }
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
            public static TResponse SendAndReceive<TRequest, TResponse>(string service, string operation, TRequest request, int merchantID, string merchantSecret)
                where TRequest : Base, new()
                where TResponse : Base, new()
            {
                //make full URL
                var url = BaseUrl + service + "/" + operation;

                //serialize JSON without any whitespace
                var jsonSerializerSettings = new JsonSerializerSettings {
                    Formatting = Formatting.None,
                    NullValueHandling = NullValueHandling.Ignore,
                    Culture = System.Globalization.CultureInfo.InvariantCulture
                };
                var rawJson = JsonConvert.SerializeObject(request, Formatting.None, jsonSerializerSettings);

                //calculate checksum
                var signString = url + "POST" + merchantID.ToString() + merchantSecret + rawJson;
                var checksum = Sha256(signString);

                //initiate request
                var webrequest = HttpWebRequest.CreateHttp(url);
                webrequest.Method = "POST";
                webrequest.ContentType = "application/json";
                //add merchant ID and checksum to headers
                webrequest.Headers.Add("MerchantID", merchantID.ToString());
                webrequest.Headers.Add("Checksum", checksum);

                //send request
                var requestStream = webrequest.GetRequestStream();
                var writer = new StreamWriter(requestStream);
                writer.Write(rawJson);
                writer.Flush();

                //receive response
                TResponse response = null;
                string rawResponse = string.Empty;
                WebResponse webresponse = null;
                try
                {
                    webresponse = webrequest.GetResponse();
                }
                catch (WebException ex)
                {
                    webresponse = ex.Response;
                }
                var responseStream = webresponse.GetResponseStream();
                var reader = new StreamReader(responseStream);
                rawResponse = reader.ReadToEnd();
                response = JsonConvert.DeserializeObject<TResponse>(rawResponse);



                //verify response checksum
                //always require presence of a checksum header
                if (!string.IsNullOrWhiteSpace(webresponse.Headers["Checksum"]))
                {
                    var responseChecksum = webresponse.Headers["Checksum"];
                    var responseSignString = webresponse.ResponseUri.AbsoluteUri + "POST" + merchantID.ToString() + merchantSecret + rawResponse;
                    var responseVerificationChecksum = Sha256(responseSignString);
                    if (!responseChecksum.Equals(responseVerificationChecksum, System.StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (string.IsNullOrWhiteSpace(response.Message))
                        {
                            response = new TResponse { Message = "Authentication error: the checksum was incorrect. Verify your secret code." };
                        }
                        response = new TResponse { Message = response.Message };
                    }
                }
                else
                {
                    //if no checksum header was present in the response, the most likely cause is that the sender ID was invalid
                    //return only the response message and regard the response as failed
                    if (string.IsNullOrWhiteSpace(response.Message))
                    {
                        response = new TResponse { Message = "Authentication error: no checksum found. Verify your merchant ID." };
                    }
                    response = new TResponse { Message = response.Message };
                }

                //close streams
                writer.Dispose();
                reader.Dispose();
                webresponse.Dispose();

                return response;
            }

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
