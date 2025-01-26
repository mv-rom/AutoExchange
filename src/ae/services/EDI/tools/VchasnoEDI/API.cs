using System;
using System.Collections.Generic;
using ae.lib;
using ae.services.EDI.structure;
using ae.services.EDI.tools.VchasnoEDI.structure;



namespace ae.services.EDI.tools.VchasnoEDI
{
    internal class API
    {
        private static API Instance = null;
        private RestApiClient RAC;

        private string BaseUrl;
        private string Authorization;
        private string ContentType;


        private bool Init(ConfigClass config)
        {
            bool result = false;
            try
            {
                BaseUrl = "";
                if (!config.VchasnoEDI_ApiSetting.TryGetValue("url", out BaseUrl))
                    return false;

                Authorization = "";
                if (!config.VchasnoEDI_ApiSetting.TryGetValue("authorization", out Authorization))
                    return false;

                if (!config.VchasnoEDI_ApiSetting.TryGetValue("content_type", out ContentType))
                    ContentType = "application/json";

                this.RAC = new RestApiClient();
                this.RAC.Init(BaseUrl, Authorization, ContentType);
                result = true;
            }
            catch (Exception ex) {
                Base.LogError("Error in "+this.ToString()+"->Init(): "+ex.Message);
            }
            Base.Log(this.ToString()+"->Init() is complete!");
            return result;
        }

        public void DeInit()
        {
            this.RAC = null;
        }

        public static API getInstance(ConfigClass config)
        {
            if (API.Instance == null) {
                API.Instance = new API();
                if (API.Instance.Init(config) != true) API.Instance.DeInit();
            }
            return API.Instance;
        }

        public List<Order> getNowListDocuments()
        {
            var nowDT = DateTime.Now.ToString("yyyy-MM-dd");
            return this.getListDocuments(nowDT, nowDT);
        }
        
        public List<Order> getNowYesterdayListDocuments()
        {
            var yesterdayDT = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
            var nowDT = DateTime.Now.ToString("yyyy-MM-dd");
            return this.getListDocuments(yesterdayDT, nowDT);
        }

        public List<Order> getYesterdayListDocuments()
        {
            var yesterdayDT = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
            return this.getListDocuments(yesterdayDT, yesterdayDT);
        }

        public List<Order> getListDocuments(string DateFrom, string DateTo, int with_json_data=0) {
            List<Order> result = null;
            try
            {
                string data = this.RAC.GET("documents", "date_from="+ DateFrom+ "&date_to=" + DateTo + "&with_json_data="+ with_json_data);
                if (!String.IsNullOrEmpty(data)) {
                    result = JSON.fromJSON<List<Order>>(data);
                }
            }
            catch (Exception ex) {
                Base.Log("Error in "+this.GetType().Name+"->getDocument(): "+ex.Message);
            }
            return result;
        }

        public Order getDocument(string ids)
        {
            Order result = null;
            try
            {
                string data = this.RAC.GET("documents/"+ids, "");
                if (!String.IsNullOrEmpty(data)) {
                    result = JSON.fromJSON<Order>(data);
                }
            }
            catch (Exception ex) {
                Base.Log("Error in "+this.ToString()+ "->getDocument(): "+ex.Message);
            }
            return result;
        }

        public PostAnswer rejectDocument(string ids, string deal_ids)
        {
            //example: https://edi.vchasno.ua/api/deals/0f91f8f6-ffa0-ef12-30d3-466243f2b2f7/rejections?deal_id=0f91f8f6-fd63-7e4d-ee8d-f9453f2c9a3a
            PostAnswer result = null;
            PostData raw_data = new PostData() { ids = ids };
            string data = String.Empty;
            try
            {
                data = this.RAC.POST("deals/" + ids + "/rejections", "deal_id="+ deal_ids, JSON.toJSON(raw_data));
                if (!String.IsNullOrEmpty(data)) {
                    PostErrorAnswer res = JSON.fromJSON<PostErrorAnswer>(data);
                    if (res == null) {
                        result = JSON.fromJSON<PostAnswer>(data);
                    }
                }
            }
            catch (Exception ex) {
                Base.Log("Error in "+this.ToString()+"->rejectDocument(): "+ex.Message);
            }
            return result;
        }

        public PostAnswer markDocumentProcessed(string ids)
        {
            PostAnswer result = null;
            PostData raw_data = new PostData() { ids = ids };

            try
            {
                string data = this.RAC.POST("additional-documents/mark-processed", "", JSON.toJSON(raw_data));
                if (!String.IsNullOrEmpty(data)) {
                    result = JSON.fromJSON<PostAnswer>(data);
                }
            }
            catch (Exception ex)
            {
                Base.Log("Error in "+this.ToString()+"->Mark_DocumentProcessed(): "+ex.Message);
            }
            return result;
        }


        //https://docs.google.com/document/d/1h2fsnTa3M70PYQxCmOTDIGtJKOKXNXweTHet2gtJFcY/edit?pli=1&tab=t.0
        //https://www.postman.com/planetary-eclipse-102992/edi-origin/request/vqudul0/copy?tab=overview
        public OrderResponse sendNewDocument(string dealId, string rawData)
        {
            OrderResponse result = null;
            var oldContentType = this.ContentType;
            try
            {
                this.ContentType = "multipart/form-data";
                string data = this.RAC.POST("api/documents", "deal_id="+dealId, JSON.toJSON(rawData));
                if (!String.IsNullOrEmpty(data)) {
                    result = JSON.fromJSON<OrderResponse>(data);
                }
            }
            catch (Exception ex)
            {
                Base.Log("Error in " + this.ToString() + "->sendNewDocument(): " + ex.Message);
            }
            this.ContentType = oldContentType;
            return result;
        }
    }
}
