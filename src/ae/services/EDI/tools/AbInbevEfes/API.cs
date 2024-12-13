using System;
using System.IO;
using ae.lib;
using ae.services.EDI.structure;
using ae.services.EDI.tools.AbInbevEfes.structure;



namespace ae.services.EDI.tools.AbInbevEfes
{
    internal class API
    {
        private static API Instance = null;
        private RestApiClient Authorization_RAC;
        private RestApiClient RAC;
        private string WorkDir;

        private string      Authorization_BaseUrl;
        private string      Authorization_ContentType;
        private string      Authorization_Username;
        private string      Authorization_Password;
        private string      Authorization_Answer_AccessToken;
        private string      Authorization_Answer_TokenType;
        private int         Authorization_Answer_Expires_in;
        private DateTime    Authorization_Answer_StartDate;

        private string      Data_BaseUrl;
        private string      Data_ContentType;


        private bool Init(ConfigClass config, string workDir)
        {
            bool result = false;
            int HttpClientTimeout = 1*60*1000;
            try
            {
                this.WorkDir = workDir;

                this.Authorization_BaseUrl = "";
                if (!config.AbInbevEfes_ApiSetting.TryGetValue("authorization_url", out this.Authorization_BaseUrl))
                    return false;

                if (!config.AbInbevEfes_ApiSetting.TryGetValue("authorization_content_type", out this.Authorization_ContentType))
                    this.Authorization_ContentType = "application/json";

                this.Authorization_Username = "";
                if (!config.AbInbevEfes_ApiSetting.TryGetValue("authorization_username", out this.Authorization_Username))
                    return false;

                this.Authorization_Password = "";
                if (!config.AbInbevEfes_ApiSetting.TryGetValue("authorization_password", out this.Authorization_Password))
                    return false;
                this.Authorization_Answer_Expires_in = 0;
                this.Authorization_Answer_StartDate = default(DateTime);

                this.Authorization_RAC = new RestApiClient();
                this.Authorization_RAC.Init(this.Authorization_BaseUrl, "", this.Authorization_ContentType, HttpClientTimeout);


                this.Data_BaseUrl = "";
                if (!config.AbInbevEfes_ApiSetting.TryGetValue("data_base_url", out this.Data_BaseUrl))
                    return false;

                if (!config.AbInbevEfes_ApiSetting.TryGetValue("data_content_type", out this.Data_ContentType))
                    this.Data_ContentType = "application/json";

                this.RAC = null;
                if (this.getAccessToken()) {
                    this.RAC = new RestApiClient();
                    this.RAC.Init(
                        this.Data_BaseUrl,
                        this.Authorization_Answer_TokenType+" "+this.Authorization_Answer_AccessToken,
                        this.Data_ContentType,
                        HttpClientTimeout
                    );
                    result = true;
                    Base.Log("lib.classes.AbInbevEfes.Init() is complete!");
                }
            }
            catch (Exception ex)
            {
                Base.Log("lib.classes.AbInbevEfes.Init() is complete with error!");
                Base.LogError("Error in " + this.GetType().Name + ".Init(): " + ex.Message);
            }
            return result;
        }

        public void DeInit()
        {
            this.Authorization_RAC = null;
            this.RAC = null;
        }

        public static API getInstance(ConfigClass config, string workDir)
        {
            if (API.Instance == null) {
                API.Instance = new API();
                if (API.Instance.Init(config, workDir) != true) API.Instance.DeInit();
            }
            return API.Instance;
        }

        public bool getAccessToken()
        {
            bool result = false;
            try
            {
                string rawData = "grant_type=password&scope=integrationApi&client_id=integrationApiClient&client_secret=secret"+
                    "&username="+this.Authorization_Username+
                    "&password="+this.Authorization_Password;

                string data = this.Authorization_RAC.POST("", "", rawData);
                if (!String.IsNullOrEmpty(data)) {
                    var res1 = JSON.fromJSON<AuthorizationErrorAnswer>(data);
                    if ((res1 == null) || (res1.error != "invalid_request")) {
                        var res2 = JSON.fromJSON<AuthorizationAnswer>(data);
                        if (res2 != null) {
                            this.Authorization_Answer_AccessToken = res2.access_token;
                            this.Authorization_Answer_TokenType = res2.token_type;
                            this.Authorization_Answer_Expires_in = res2.expires_in;
                            this.Authorization_Answer_StartDate = DateTime.Now;
                            result = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Base.Log("Error in " + this.GetType().Name + ".getAccessToken(): " + ex.Message);
            }
            return result;
        }


        public string PUTfromDump(string requestString) {
            var dirPath = this.WorkDir;
            //long OrderNumIndex = 611128000000000;

            if (!string.IsNullOrEmpty(requestString) && Directory.Exists(dirPath)) {
                var preSaleNo = JSON.fromJSON<PreSalesRequest>(requestString).preSaleNo;
                string[] search = Directory.GetFiles(dirPath, "dump(response-packetPreSale)_*.json");

                foreach (string filePath in search)
                {
                    using (StreamReader stream = new StreamReader(filePath))
                    {
                        string responseData = stream.ReadToEnd();
                        var response = JSON.fromJSON<PreSalesResponse>(responseData);
                        if (response.result != null &&
                            !string.IsNullOrEmpty(preSaleNo) &&
                            !string.IsNullOrEmpty(response.result.preSaleNo) &&
                            string.Equals(preSaleNo, response.result.preSaleNo))
                        {

                            //OrderNumIndex++;
                            //response.result.orderNo = OrderNumIndex;
                            return JSON.toJSON(response);
                        }
                    }
                }
            }
            return null;
        }

        public PreSalesResponse getPreSales(Object packetPreSale)
        {
            var errorDescription = "Error in " + this.GetType().Name + ".getPreSales(): ";
            try
            {
                var requestString = JSON.toJSON(packetPreSale);
                Base.DumpToFile(this.WorkDir, "(request-getPreSales).json", requestString);

                //string responseString = this.PUTfromDump(requestString);
                string responseString = this.RAC.PUT("/api/PreSales", "", requestString);
                Base.DumpToFile(this.WorkDir, "(response-getPreSales).json", responseString);

                if (!String.IsNullOrEmpty(responseString)) {
                    var res1 = JSON.fromJSON<PreSalesErrorAnswer>(responseString);
                    if ((res1 == null) || (res1.error == null) || (res1.error.Length > 0)) {
                        var response = JSON.fromJSON<PreSalesResponse>(responseString);
                        if (response.result == null) {
                            var responseLogs = this.getLogs(response.traceIdentifier);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Base.LogError(errorDescription + ex.Message, ex);
            }
            return null;
        }

        public LogsResponse getLogs(string traceIdentifier)
        {
            var errorDescription = "Error in " + this.GetType().Name + ".getLogs(): ";
            if (!String.IsNullOrEmpty(traceIdentifier)) {
                try
                {
                    //string data = this.RAC.GET("/api/Logs/" + traceIdentifier, "Filter=logLevel=\"error\"|logLevel=\"warning\"");
                    string data = this.RAC.GET("/api/Logs/" + traceIdentifier, "", "application/vnd.salesworks.logs.traceidentifier+json");
                    Base.DumpToFile(this.WorkDir, "(response-getLogs).json", data);

                    if (!String.IsNullOrEmpty(data)) {
                        var res1 = JSON.fromJSON<LogsErrorAnswer>(data);
                        if ((res1 == null) || (res1.error == null) || (res1.error.Length <= 0)) {
                            Base.Log(errorDescription + "have data in dumpLogs file ...(response-getLogs).json");
                            return JSON.fromJSON<LogsResponse>(data);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Base.LogError(errorDescription + ex.Message, ex);
                }
            }
            return null;
        }
    }
}
