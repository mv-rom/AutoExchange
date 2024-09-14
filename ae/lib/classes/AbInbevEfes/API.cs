using System;


namespace ae.lib.classes.AbInbevEfes
{
    internal class API
    {
        private static API Instance = null;
        private RestApiClient Authorization_RAC;
        private RestApiClient RAC;

        private string      Authorization_BaseUrl;
        private string      Authorization_ContentType;
        private string      Authorization_Username;
        private string      Authorization_Password;
        private string      Authorization_AccessToken;
        private int         Authorization_AccessToken_Expires_in;
        private DateTime    Authorization_AccessToken_StartDate;

        private string BaseUrl;
        private string ContentType;


        private bool Init()
        {
            bool result = false;
            int HttpClientTimeout = 30000;
            try
            {
                this.Authorization_BaseUrl = "";
                if (!Base.Config.ConfigSettings.AbInbevEfes_ApiSetting.TryGetValue("authorization_url", out this.Authorization_BaseUrl))
                    return false;

                if (!Base.Config.ConfigSettings.AbInbevEfes_ApiSetting.TryGetValue("authorization_content_type", out this.Authorization_ContentType))
                    this.Authorization_ContentType = "application/json"; // "application/json; charset=utf-8"

                this.Authorization_Username = "";
                if (!Base.Config.ConfigSettings.AbInbevEfes_ApiSetting.TryGetValue("authorization_username", out this.Authorization_Username))
                    return false;

                this.Authorization_Password = "";
                if (!Base.Config.ConfigSettings.AbInbevEfes_ApiSetting.TryGetValue("authorization_password", out this.Authorization_Password))
                    return false;
                this.Authorization_AccessToken_Expires_in = 0;
                this.Authorization_AccessToken_StartDate = default(DateTime);

                this.Authorization_RAC = new RestApiClient();
                this.Authorization_RAC.Init(this.Authorization_BaseUrl, "", this.Authorization_ContentType, HttpClientTimeout);


                this.BaseUrl = "";
                if (!Base.Config.ConfigSettings.AbInbevEfes_ApiSetting.TryGetValue("url", out this.BaseUrl))
                    return false;

                if (!Base.Config.ConfigSettings.AbInbevEfes_ApiSetting.TryGetValue("content_type", out this.ContentType))
                    this.ContentType = "application/json";

                this.RAC = new RestApiClient();
                if (this.getAccessToken()) {
                    this.RAC.Init(this.BaseUrl, "Bearer " + this.Authorization_AccessToken, this.ContentType, HttpClientTimeout);
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

        public static API getInstance()
        {
            if (API.Instance == null)
            {
                API.Instance = new API();
                if (API.Instance.Init() != true) API.Instance.DeInit();
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
                            this.Authorization_AccessToken = res2.access_token;
                            this.Authorization_AccessToken_Expires_in = res2.expires_in;
                            this.Authorization_AccessToken_StartDate = DateTime.Now;
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

        public PreSalesAnswer getPreSaleProfile(Object packetPreSale)
        {
            PreSalesAnswer result = default(PreSalesAnswer);
            try
            {
                var rawJsonString = JSON.toJSON(packetPreSale);

                string data = this.RAC.POST("", "", rawJsonString);
                if (!String.IsNullOrEmpty(data)) {
                    var res1 = JSON.fromJSON<PreSalesErrorAnswer>(data);
                    if ((res1 == null) || (res1.error != "invalid_request")) {
                        result = JSON.fromJSON<PreSalesAnswer>(data);
                    }
                }
            }
            catch (Exception ex)
            {
                Base.Log("Error in " + this.GetType().Name + ".getPreSale(): " + ex.Message);
            }
            return result;
        }
    }
}
