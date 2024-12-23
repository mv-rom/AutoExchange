using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ae.lib;
using ae.services.EDI.tools.VchasnoEDI.structure;



namespace ae.services.EDI
{
    public class EDI : Service
    {
        private string WorkDir = "";
        public structure.ConfigClass config;
        private int __N_AddDayToExecuteDay = 0;
        private structure.AlterProductClass AlterProductList;
        private structure.AgentNumberListClass agentNumberList;

        public EDI(string theServiceName) : base(theServiceName)
        {
            this.config = Base.Config.ConfigSettings.Services.EDI;
        }

        public void log(string msg)
        {
            Base.Log("Service [" + this.GetType().Name + "]> " + msg);
        }


        private string CalcOrderExecuteDate(DateTime orderExecuteDate, string PlanningListDaysOfWeeek)
        {
            string[] plDoW = PlanningListDaysOfWeeek.Split(',');
            Array.Sort(plDoW);
            int N_DisplacementNowDay = 0; //-1

            if (orderExecuteDate.Day > DateTime.Now.AddDays(N_DisplacementNowDay).Day) {
                int execDow = (int)orderExecuteDate.DayOfWeek;
                int firstPlanningDayOfWeek = 0;
                int daysDifference = -1;
                foreach (var p in plDoW)
                {
                    int res_p = 0;
                    if (int.TryParse(p, out res_p) && 0 < res_p && res_p <= 7) {
                        firstPlanningDayOfWeek = int.Parse(plDoW[0]);
                        if (res_p >= execDow) {
                            daysDifference = res_p - execDow;
                            break;
                        }
                    }
                }

                if (daysDifference == -1) {
                    daysDifference = 7 - execDow + firstPlanningDayOfWeek;
                }
                return orderExecuteDate.AddDays(daysDifference).ToString();
            }
            return "";
        }

        private List<Order> getOrdersFromEDI(tools.VchasnoEDI.API api, int n_day_before=0)
        {
            //getting needed documents
            var yesterdayDT = DateTime.Now.AddDays(n_day_before).ToString("yyyy-MM-dd");
            var nowDT = DateTime.Now.ToString("yyyy-MM-dd");

            //var obj1 = VchasnoAPI.getDocument("0faac24e-1960-3b29-94a1-1384badb60b7");

            var ordersList = api.getListDocuments(yesterdayDT, nowDT, 1);
            if (ordersList == null || ordersList.Count() <= 0)
                return null;

            //filter only Orders
            var result = ordersList.Where(x => x.type == 1).ToList();

            int count = result.Count();
            if (count <= 0) return null;

            //Filter by deal_id (like order_number) - leave only new orders
            int i = 0;
            while (i < count) {
                var item = result[i];
                if (ordersList.FirstOrDefault(t => t.type > 1 && t.deal_id.Equals(item.deal_id)) != null) {
                    result.Remove(item);
                    count = result.Count();
                } else {
                    i++;
                }
            }
            if (count <= 0) return null;

            //check our EDRPOU
            string ourEdrpou = "";
            if (!Base.Config.ConfigSettings.BaseSetting.TryGetValue("edrpou", out ourEdrpou)) {
                this.log("There is not EDRPOU of this company!");
                return null;
            }

            //Filter by edrpou - leave only from the list of Companies
            var companyList = this.config.Companies;
            i = 0;
            while (i < count) {
                var item = result[i];
                if (companyList.FirstOrDefault(t =>
                    item.company_to_edrpou.Equals(ourEdrpou) && 
                    t.edrpou.Equals(item.company_from_edrpou) &&
                    t.gln.Equals(item.as_json.buyer_gln)
                ) != null) {
                    i++;
                } else {
                    result.Remove(item);
                    count = result.Count();
                }
            }
            if (count <= 0) return null;

            return result;
        }

        private List<structure._1C.TTbyGLN_Item> getTTbyGLNfrom1C(List<tools.VchasnoEDI.structure.Order> source)
        {
            string gln = this.config.gln;
            if (gln.Length <= 0) {
                this.log("Warning in getTTbyGLNfrom1C(): hasn't parameter [gln] in config!");
                return null;
            }

            var listTT = new List<structure._1C.TTbyGLN_Item>();
            foreach (var s in source)
            {
                if (gln.Equals(s.as_json.seller_gln))
                {
                    bool found = false;
                    var glnTT =      long.Parse(s.as_json.buyer_gln);
                    var glnTT_gruz = long.Parse(s.as_json.delivery_gln);

                    foreach (var l in listTT)
                    {
                        if (glnTT == l.glnTT && glnTT_gruz == l.glnTT_gruz)
                        {
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        listTT.Add(new structure._1C.TTbyGLN_Item()
                        {
                            glnTT = glnTT,
                            glnTT_gruz = glnTT_gruz,
                            codeTT_part1 = 0,
                            codeTT_part2 = 0,
                            codeTT_part3 = 0
                        });
                    }
                }
            }

            if (listTT.Count() > 0)
            {
                string report1cName = "tt_by_gln";
                var input = new structure._1C.TTbyGLN() { list = listTT };
                var output = _1C.runReportProcessingData<structure._1C.TTbyGLN>(WorkDir, this.Reports1CDir, report1cName, input);
                if (output == null) {
                    this.log("Warning in getTTbyGLNfrom1C(): after do report [" + report1cName + "]!");
                    return null;
                }

                var output_listTT = output.list;
                int i = 0;
                while (i < listTT.Count())
                {
                    var glnTT = listTT[i].glnTT;
                    var glnTT_gruz = listTT[i].glnTT_gruz;

                    var output_item = output_listTT.
                        Where(x => x.glnTT == glnTT).
                        FirstOrDefault(y => y.glnTT_gruz == glnTT_gruz);
                    if (output_item != null) {
                        listTT[i].codeTT_part1 = output_item.codeTT_part1;
                        listTT[i].codeTT_part2 = output_item.codeTT_part2;
                        listTT[i].codeTT_part3 = output_item.codeTT_part3;
                        i++;
                    } else {
                        listTT.RemoveAt(i);
                    }
                }
                return listTT;
            }
            return null;
        }

        private List<structure._1C.ProductProfiles_Group> getProductProfilesOfTTfrom1C(
            List<tools.VchasnoEDI.structure.Order> source,
            List<structure._1C.TTbyGLN_Item> listTT
        )
        {
            var groupPP = new List<structure._1C.ProductProfiles_Group>();
            foreach (var s in source)
            {
                var id = s.id;
                var glnTT = long.Parse(s.as_json.buyer_gln);
                var glnTT_gruz = long.Parse(s.as_json.delivery_gln);
                var delivery_address = s.as_json.delivery_address;

                var execD = DateTime.Parse(s.as_json.date_expected_delivery).AddDays(this.__N_AddDayToExecuteDay);
                string date_expected_delivery = "";
                foreach (var item in this.config.Companies)
                {
                    if (long.Parse(item.gln) == glnTT && item.gruzs != null) {
                        var gruz = item.gruzs.FirstOrDefault(t => (t.gln.Length > 0 && long.Parse(t.gln) == glnTT_gruz));
                        if (gruz != null) {
                            date_expected_delivery = this.CalcOrderExecuteDate(execD, gruz.executionDayOfWeek);
                            break;
                        }
                    }
                }
                if (date_expected_delivery.Length <= 0) {
                    this.log(
                        "Warning in getProductProfilesOfTTfrom1C(): " + 
                        "date_expected_delivery is empty or not right in order ["+ id + "]!"
                    );
                    continue;
                }


                //search in listTT
                bool found = false;
                int found_i = 0;
                foreach (var tt in listTT)
                {
                    if (tt.glnTT == glnTT && tt.glnTT_gruz == glnTT_gruz) {
                        found = true;
                        break;
                    }
                    found_i++;
                }

                if (!found) {
                    this.log(
                        "Warning in getProductProfilesOfTTfrom1C(): " +
                        "not found TT with GLN [" + glnTT + ", " + glnTT_gruz + "(" + delivery_address + ")]!"
                    );
                    continue;
                }
                var foundTT = listTT[found_i];


                //search group in groupPP
                bool tt_found_in_PP = false;
                int tt_found_in_PP_i = 0;
                foreach (var g in groupPP)
                {
                    if (g.codeTT_part1 == foundTT.codeTT_part1 &&
                        g.codeTT_part2 == foundTT.codeTT_part2 &&
                        g.codeTT_part3 == foundTT.codeTT_part3 &&
                        g.id == id
                    ) {
                        tt_found_in_PP = true;
                        break;
                    }
                    tt_found_in_PP_i++;
                }

                structure._1C.ProductProfiles_Group pp_g = null;
                if (!tt_found_in_PP) {
                    pp_g = new structure._1C.ProductProfiles_Group()
                    {
                        id = id,
                        ExecutionDate = date_expected_delivery,
                        codeTT_part1 = foundTT.codeTT_part1,
                        codeTT_part2 = foundTT.codeTT_part2,
                        codeTT_part3 = foundTT.codeTT_part3,
                        list = new List<structure._1C.ProductProfiles_Item>()
                    };
                    groupPP.Add(pp_g);
                } else {
                    pp_g = groupPP[tt_found_in_PP_i];
                }

                //search items in listPP.group.items
                var listItems = s.as_json.items;
                int num = 1;
                foreach (var it in listItems)
                {
                    var product_code = this.selectionAlternativeProduct(long.Parse(it.product_code));
                    var title = it.title;
                    //var position = int.Parse(it.position);
                    //var buyer_code = long.Parse(it.buyer_code);
                    //var measure = it.measure; //PCE
                    //var supplier_code = it.supplier_code;
                    //var tax_rate = int.Parse(it.tax_rate); //20
                    //var quantity = float.Parse(it.quantity);

                    //search in listPP
                    bool item_found = false;
                    foreach (var l in groupPP[tt_found_in_PP_i].list) {
                        if (l.EAN == product_code) item_found = true;
                    }

                    if (!item_found) {
                        pp_g.list.Add(new structure._1C.ProductProfiles_Item() {
                            EAN = product_code,
                            Title = title,
                            Number = num
                        });
                        num++;
                    }
                }
            }

            if (groupPP.Count() > 0)
            {
                string report1cName = "product_profiles";
                var input = new structure._1C.ProductProfiles() { group = groupPP };
                var output = ae.lib._1C.runReportProcessingData<structure._1C.ProductProfiles>(WorkDir, this.Reports1CDir, report1cName, input);
                if (output == null) {
                    this.log("Warning in getProductProfilesOfTTfrom1C(): after do report [" + report1cName + "]!");
                    return null;
                }
                
                var output_groupPP = output.group;
                int i = 0;
                while (i < groupPP.Count())
                {
                    var g = groupPP[i];
                    var output_item = output_groupPP.Where(x => (x.id == g.id)).FirstOrDefault();
                    if (output_item != null)
                    {
                        int j = 0;
                        while (j < g.list.Count())
                        {
                            var g_el = g.list[j];
                            var output_item_el = output_item.list.Where(x => (x.EAN == g_el.EAN)).FirstOrDefault();
                            if (output_item_el != null)
                            {
                                g_el.ProductCode = output_item_el.ProductCode;
                                g_el.ProductType = output_item_el.ProductType;
                                g_el.BasePrice = output_item_el.BasePrice;
                                j++;
                            }
                            else
                            {
                                g.list.RemoveAt(j);
                            }
                        }
                        i++;
                    } else {
                        groupPP.RemoveAt(i);
                    }
                }
                output_groupPP = null;
                return groupPP;
            }
            return null;
        }

        private Dictionary<string, structure.SplittedOrdersClass> doSplittingUpOrders(
            List<tools.VchasnoEDI.structure.Order> Orders,
            List<structure._1C.ProductProfiles_Group> groupPP,
            Dictionary<string, structure.SplittedOrdersClass> splittedOrders
        )
        {
            var dictSO = new Dictionary<string, structure.SplittedOrdersClass>();
            foreach (var o in Orders)
            {
                var id = o.id;
                var deal_status = o.deal_status;

                //enumeration by type
                for (int type_of_product = 0; type_of_product < 4; type_of_product++)
                {
                    var found_key = id + "@" + type_of_product;
                    structure.SplittedOrdersClass so = null;
                    if (splittedOrders != null && splittedOrders.TryGetValue(found_key, out so) &&
                        so.resut_orderNo != null && so.resut_orderNo.Length > 0)
                    {
                        //TODO: if (!s.deal_status.Equals("new")) { }
                        so.deal_status = deal_status;
                        dictSO.Add(found_key, so);
                    }
                    else
                    {
                        var found_item = groupPP.Where(x => (x.id == id)).FirstOrDefault();
                        if (found_item == null) {
                            this.log(
                                "Warning in doSplittingUpOrders(): not found order with id " +
                                "[" + id + "] in ProductProfiles_Group!"
                            );
                            break;
                        }

                        var newItems = new List<structure.SplittedOrdersClass_Order>();
                        try
                        {
                            var listItems = o.as_json.items;
                            foreach (var it in listItems)
                            {
                                var ean13 = this.selectionAlternativeProduct(long.Parse(it.product_code));
                                var found_list_item = found_item.list.
                                    Where(x => (x.EAN == ean13 && x.ProductType == type_of_product)).FirstOrDefault();
                                if (found_list_item != null)
                                {
                                    var s_qty = (it.quantity.Contains(".")) ? it.quantity.Replace(".", ",") : it.quantity;
                                    newItems.Add(new structure.SplittedOrdersClass_Order()
                                    {
                                        ean13 = ean13,
                                        codeKPK = found_list_item.ProductCode,
                                        basePrice = found_list_item.BasePrice,
                                        qty = float.Parse(s_qty), //Math.Round((decimal)floatValue, 2);
                                        promoType = 0,
                                        totalDiscount = 0
                                    });
                                }
                                var title = it.title;
                            }
                        }
                        catch (Exception ex)
                        {
                            Base.LogError(ex.Message, ex);
                        }

                        //add new
                        if (newItems.Count > 0)
                        {
                            var glnTT = long.Parse(o.as_json.buyer_gln);
                            var glnTT_gruz = long.Parse(o.as_json.delivery_gln);
                            var execD = DateTime.Parse(o.as_json.date_expected_delivery).AddDays(this.__N_AddDayToExecuteDay);
                            string date_expected_delivery = "";
                            foreach (var item in this.config.Companies)
                            {
                                if (long.Parse(item.gln) == glnTT && item.gruzs != null) {
                                    var gruz = item.gruzs.FirstOrDefault(t => long.Parse(t.gln) == glnTT_gruz);
                                    if (gruz != null) {
                                        date_expected_delivery = this.CalcOrderExecuteDate(execD, gruz.executionDayOfWeek);
                                        break;
                                    }
                                }
                            }

                            if (date_expected_delivery.Length > 0)
                            {
                                dictSO.Add(found_key, new structure.SplittedOrdersClass()
                                {
                                    id = id,
                                    ae_id = Base.genarateKey(),
                                    orderNumber = o.number,
                                    OrderDate = DateTime.Parse(o.as_json.date),
                                    OrderExecutionDate = DateTime.Parse(date_expected_delivery),
                                    codeTT_part1 = found_item.codeTT_part1,
                                    codeTT_part2 = found_item.codeTT_part2,
                                    codeTT_part3 = found_item.codeTT_part3,
                                    Items = newItems,
                                    status1c = 0, //current state in 1C
                                    deal_status = deal_status //current state in EDI
                                });
                            } else {
                                this.log(
                                    "Warning in doSplittingUpOrders(): " +
                                    "date_expected_delivery is empty or not right in splitted order [" + found_key + "]!"
                                );
                            }
                        }
                    }
                }
            }
            return (dictSO.Count > 0) ? dictSO : null;
        }

        private bool CombineAbiePreSalesAndOrders(ref Dictionary<string, structure.SplittedOrdersClass> source)
        {
            string warehouse_code = "";
            if (!this.config.AbInbevEfes_ApiSetting.TryGetValue("warehouse_code", out warehouse_code)) {
                this.log("Warning in CombineAbiePreSalesAndOrders(): not found [warehouse_code]!");
                return false;
            }

            var AbInbevEfesAPI = tools.AbInbevEfes.API.getInstance(this.config, WorkDir);
            if (AbInbevEfesAPI == null) {
                this.log("Warning in CombineAbiePreSalesAndOrders(): AbInbevEfesAPI is null!");
                return false;
            }

            int nCount = 0;
            foreach (var so in source)
            {
                //Verification for the previous satisfactory request
                if (so.Value.resut_orderNo != null && so.Value.resut_orderNo.Length > 0) continue;

                var preSalesDetails = new List<tools.AbInbevEfes.structure.preSalesDetails>();
                foreach (var it in so.Value.Items)
                {
                    preSalesDetails.Add(new tools.AbInbevEfes.structure.preSalesDetails()
                    {
                        productCode = it.codeKPK.ToString(),
                        basePrice = it.basePrice.ToString("F4", CultureInfo.InvariantCulture),
                        qty = it.qty.ToString("F4", CultureInfo.InvariantCulture),
                        lotId = "-",
                        promoType = it.promoType.ToString(), //1 - vstugnu kyputu, 0 - general (default)
                        vat = "20.0" // 20.0% - PDV ??? get from 1C
                    });
                }

                if (preSalesDetails.Count > 0)
                {
                    var request = new tools.AbInbevEfes.structure.PreSalesRequest()
                    {
                        preSaleNo = so.Key,
                        custOrderNo = so.Value.id,
                        outletCode =
                            so.Value.codeTT_part1 + @"\" +
                            so.Value.codeTT_part2 + @"\" +
                            so.Value.codeTT_part3,
                        preSaleType = "6", //EDI order
                        dateFrom = so.Value.OrderDate.ToString("yyyy-MM-ddTHH:mm:ss"),
                        dateTo =   so.Value.OrderExecutionDate.ToString("yyyy-MM-ddTHH:mm:ss"),
                        warehouseCode = warehouse_code,
                        vatCalcMod = "0", // 0 - price without PDV, 1 - with PDV
                        custId = int.Parse(Base.torg_sklad).ToString(),
                        preSalesDetails = preSalesDetails
                    };

                    var PreSaleResult = AbInbevEfesAPI.getPreSales(request);
                    if (PreSaleResult != null)
                    {
                        if (PreSaleResult.result != null)
                        {
                            //updating SplittedOrders
                            source[so.Key].resut_orderNo = PreSaleResult.result.orderNo.ToString();
                            source[so.Key].result_outletId = PreSaleResult.result.outletId.ToString();
                            source[so.Key].resut_owner_id = this.selectionAgent(PreSaleResult.result.outletCode);

                            bool foundAtleastOne = false;
                            var listItems = PreSaleResult.result.details;
                            foreach (var its in listItems)
                            {
                                var compareCodeKPK = int.Parse(its.productCode);
                                var qty = its.qty;
                                for (int i = 0; i < so.Value.Items.Count; i++)
                                {
                                    var vit = so.Value.Items[i];
                                    if ((vit.codeKPK == compareCodeKPK) && (vit.qty == qty))
                                    {
                                        source[so.Key].Items[i].totalDiscount = its.totalDiscount;
                                        foundAtleastOne = true;
                                    }
                                }
                            }
                            if (foundAtleastOne) nCount++;
                        }
                        else {
                            //???
                            var ErrorResult = AbInbevEfesAPI.getLogs(PreSaleResult.traceIdentifier);
                            if (ErrorResult != null) {
                                this.log(ErrorResult.message);
                            }
                        }
                    }
                }
            }
            return nCount > 0 ? true : false;
        }


        private bool CheckAndAddOrdersIn1C(
            ref Dictionary<string, structure.SplittedOrdersClass> source
        )
        {
            var newOrders = new List<structure._1C.NewOrders_Order>();
            foreach (var so in source)
            {
                if (so.Value.status1c == 0)
                {
                    int num = 1;
                    var newItems = new List<structure._1C.NewOrders_Item>();
                    foreach (var it in so.Value.Items)
                    {
                        newItems.Add(new structure._1C.NewOrders_Item()
                        {
                            Number = num,
                            codeKPK = it.codeKPK,
                            BasePrice = it.basePrice,
                            qty = it.qty,
                            Akcya = it.totalDiscount
                        });
                        num++;
                    }

                    if (newItems.Count > 0)
                    {
                        if (string.IsNullOrEmpty(so.Value.resut_orderNo))
                        {
                            this.log("Warning in CheckAndAddOrdersIn1C(): order with id [" + so.Key + "] have empty [resut_orderNo]!");
                            newItems = null;
                            continue;
                        }
                        if (string.IsNullOrEmpty(so.Value.result_outletId))
                        {
                            this.log("Warning in CheckAndAddOrdersIn1C(): order with id [" + so.Key + "] have empty [result_outletId]!");
                            newItems = null;
                            continue;
                        }

                        newOrders.Add(new structure._1C.NewOrders_Order()
                        {
                            id = so.Key,
                            orderNumber = so.Value.resut_orderNo,
                            orderEDINumber = so.Value.orderNumber,
                            outletId = so.Value.result_outletId,
                            executionDate = so.Value.OrderExecutionDate.ToString("dd-MM-yyyy"),
                            codeTT_part1 = so.Value.codeTT_part1,
                            codeTT_part2 = so.Value.codeTT_part2,
                            codeTT_part3 = so.Value.codeTT_part3,
                            owner_id = so.Value.resut_owner_id,
                            items = newItems
                        });
                    }
                }
            }

            if (newOrders.Count() > 0)
            {
                string report1cName = "vkachka_zayavok";
                var input = new structure._1C.NewOrders() { orders = newOrders };
                var output = _1C.runReportProcessingData<structure._1C.NewOrders>(WorkDir, this.Reports1CDir, report1cName, input);
                if (output != null) {
                    var output_Orders = output.orders;
                    foreach (var oO in output_Orders)
                    {
                        if (source.ContainsKey(oO.id)) source[oO.id].status1c = oO.returnStatus;
                    }
                    return true;
                }
            }
            return false;
        }


        private bool loadAlternativeProdutList(string workDir)
        {
            var filePath = Path.Combine(workDir, "alterProductList.json");
            if (File.Exists(filePath)) {
                this.AlterProductList = JSON.fromJSON<structure.AlterProductClass>(File.ReadAllText(filePath));
                if (this.AlterProductList != null && 
                    this.AlterProductList.AlterProductList != null && 
                    this.AlterProductList.AlterProductList.Length > 0
                ) {
                    return true;
                }
            }
            return false;
        }

        private long selectionAlternativeProduct(long productEAN)
        {
            var alterP = this.AlterProductList.AlterProductList.FirstOrDefault(x => x.EAN == productEAN);
            if (alterP != null) {
                this.log("selectionAlternativeProduct contains a position "+
                    "[" + alterP.NameProduct + "] with an alternative product code EAN [" + alterP.alterEAN + "]!");
                return alterP.alterEAN;
            } else {
                return productEAN;
            }
        }

        private bool loadAgentNumberList(string workDir)
        {
            var filePath = Path.Combine(workDir, "Outlets.xml");
            if (File.Exists(filePath))
            {
                this.agentNumberList = XML.ConvertXMLTextToClass<structure.AgentNumberListClass>(File.ReadAllText(filePath));
                if (this.agentNumberList != null &&
                    this.agentNumberList.Outlets != null &&
                    this.agentNumberList.Outlets.Outlet.Count > 0
                ) {
                    return true;
                }
            }
            return false;
        }

        private int selectionAgent(string codeTT)
        {
            var outlet = this.agentNumberList.Outlets.Outlet.FirstOrDefault(x => x.OL_CODE.Equals(codeTT));
            return (outlet != null) ? outlet.OWNER_ID : 0;
        }



        //------------------------------------------------------------------
        public void actionInBox()
        {
            int ResCount = 0;
            var fileJSON = "orders.json";
            if (Directory.Exists(this.InboxDir))
            {
                this.WorkDir = this.InboxDir;

                //Clean Inbox dir first
                var arrExcludeFiles = new string[] { "orders.json", "splitted_orders.json" };
                Base.CleanDirectory(this.InboxDir, arrExcludeFiles);

                var ediDir = Path.GetFullPath(Path.Combine(this.WorkDir, @"..\"));

                if (!loadAlternativeProdutList(ediDir)) {
                    this.log("loadAlternativeProdutList has problem!");
                    return;
                }

                if (!loadAgentNumberList(ediDir)) {
                    this.log("loadAgentNumberList has problem!");
                    return;
                }


                try
                {
                    var instVchasnoAPI = tools.VchasnoEDI.API.getInstance(this.config);
                    var ordersListFiltered = getOrdersFromEDI(instVchasnoAPI);
                    if (ordersListFiltered != null && ordersListFiltered.Count > 0)
                    {
                        //var fp = Path.Combine(this.InboxDir, fileJSON);
                        //if (!File.Exists(fp)) {
                        //    throw new Exception("STOP");
                        //}
                        //var ordersListFiltered = JSON.fromJSON<List<tools.VchasnoEDI.structure.Order>>(File.ReadAllText(fp));

                        JSON.DumpToFile(this.InboxDir, fileJSON, ordersListFiltered);

                        var TTbyGLN_List = getTTbyGLNfrom1C(ordersListFiltered);
                        if (TTbyGLN_List == null)
                            throw new Exception("Result of getTTbyGLNfrom1C is null.");

                        var ProductProfiles = getProductProfilesOfTTfrom1C(ordersListFiltered, TTbyGLN_List);
                        if (ProductProfiles == null || ProductProfiles.Count <= 0)
                            throw new Exception("Result of getProductProfilesOfTTfrom1C is null.");

                        string jsonStr = "";
                        var filePathSO = Path.Combine(this.InboxDir, "splitted_" + fileJSON);
                        if (File.Exists(filePathSO)) {
                            jsonStr = File.ReadAllText(filePathSO);
                        }
                        var savedSplittedOrders = JSON.fromJSON<Dictionary<string, structure.SplittedOrdersClass>>(jsonStr);

                        var SplittedOrders = doSplittingUpOrders(ordersListFiltered, ProductProfiles, savedSplittedOrders);
                        if (SplittedOrders == null)
                            throw new Exception("Result of doSplittingUpOrders is null.");
                        
                        var combineStatus = CombineAbiePreSalesAndOrders(ref SplittedOrders);

                        var checkStatus1c = CheckAndAddOrdersIn1C(ref SplittedOrders);
                        if (checkStatus1c)
                            this.log("Processing orders in 1C is successful.");
                        else
                            this.log("Some problems with processing orders in 1c!");

                        if (combineStatus || checkStatus1c) {
                            //save Splited Order List
                            jsonStr = JSON.toJSON(SplittedOrders);
                            File.WriteAllText(filePathSO + "_temp", jsonStr);
                            if (File.Exists(filePathSO))
                                File.Delete(filePathSO);
                            File.Move(filePathSO + "_temp", filePathSO);
                        }
                    }
                }
                catch (Exception ex)
                {
                    this.log("Error in actionInBox(): " + ex.Message);
                }
                finally
                {
                    if (_1C.Instance != null)
                        _1C.Instance.runExit();
                    _1C.Instance = null;

                    Base.SaveDirectory(Base.ArchivesDir, this.InboxDir);
                }
            }
            this.log("ResCount: " + ResCount);
        }

        public void actionOutBox()
        {
            int ResCount = 0;
            if (Directory.Exists(this.OutboxDir))
            {
                this.WorkDir = this.OutboxDir;
                try
                {
/*
                    var inst1C = _1C.getInstance(this.Reports1CDir);
                    if (inst1C != null) {
                        throw new Exception("Error in actionOutBox of run 1c!");
                    }
                    string report1cName = "vugruzka_edi";
                    if (inst1C.runExternalReport(this.WorkDir, report1cName)) {
                        throw new Exception("Error in actionOutBox of run 1c report [" + report1cName + "]!");
                    }
*/
                    var instVchasnoAPI = tools.VchasnoEDI.API.getInstance(this.config);
                    var ordersListFiltered = this.getOrdersFromEDI(instVchasnoAPI, -7);

                    //Testing read data from dump
                    //--------------------------------------------------------
                    //var fp = Path.Combine(this.WorkDir, "orders.json");
                    //if (!File.Exists(fp))
                    //{
                    //    throw new Exception("STOP");
                    //}
                    //ordersListFiltered = JSON.fromJSON<List<tools.VchasnoEDI.structure.Order>>(File.ReadAllText(fp));

                    if (ordersListFiltered == null || ordersListFiltered.Count <= 0) {
                        this.log("There is no one order of EDI to the processing.");
                        goto __exit;
                    }


                    //load _DESADV_file from directory in OutboxDir
                    var listCompanyFilesDESADV = new Dictionary<string, Dictionary<string, tools.VchasnoEDI.structure.DESADV>>();
                    string pattern1 = @"^.+_DESADV_.+\.xml$";
                    var dirsList = Directory.GetDirectories(this.OutboxDir);
                    foreach (var dPath in dirsList)
                    {
                        if (Directory.Exists(dPath))
                        {
                            var dirName = new DirectoryInfo(dPath).Name;
                            var listFilesDESADV = new Dictionary<string, tools.VchasnoEDI.structure.DESADV>();
                            //filter by company gln
                            //  if (this.config.Companies.FirstOrDefault(x => x.erdpou == dirName) == null) continue;

                            foreach (var file in Directory.GetFiles(dPath))
                            {
                                var fileName = new FileInfo(file).Name;
                                //parse *_DESADV_*.xml:
                                if (Regex.IsMatch(fileName, pattern1) && File.Exists(file))
                                {
                                    var desadvClass = XML.ConvertXMLFileToClass<tools.VchasnoEDI.structure.DESADV>(file);
                                    if (desadvClass != null) {
                                        listFilesDESADV.Add(fileName, desadvClass);
                                    }
                                    else {
                                        File.Move(file, Path.Combine(dPath, "bad_" + fileName));
                                        this.log("File [" + fileName + "] in directory [" + dirName + "] have a bad structure! It was renamed!");
                                    }

                                }
                            }

                            listCompanyFilesDESADV.Add(dirName, listFilesDESADV);
                        }
                    }
                    if (listCompanyFilesDESADV.Count <= 0)
                        goto __exit;

                    //analize in Orders
                    var listOrdersFilesDESADV = new Dictionary<string, Dictionary<string, DESADV>>();
                    foreach (var oLF in ordersListFiltered) {
                        var listFilesDESADV = new Dictionary<string, DESADV>();
                        if (listCompanyFilesDESADV.TryGetValue(oLF.company_from_edrpou, out listFilesDESADV)) {
                            var lFilesDA = listFilesDESADV.Where(x => x.Value.ORDERNUMBER == oLF.number).ToList();
                            foreach (var fDA in lFilesDA)
                            {
                                var posList = fDA.Value.HEAD.PACKINGSEQUENCE.POSITION;
                                for (int i = 0; i < posList.Count(); i++)
                                {
                                    var item = oLF.as_json.items.FirstOrDefault<tools.VchasnoEDI.structure.OrderDataItem>(
                                        x => x.product_code == posList[i].PRODUCT
                                    );
                                    if (item != null) {
                                        posList[i].PRODUCTIDBUYER = "" + (string.IsNullOrEmpty(item.buyer_code) ? "0" : item.buyer_code);
                                    }
                                }
                            }
                            if (lFilesDA.Count > 0) {
                                var new_listFilesDESADV = new Dictionary<string, DESADV>();
                                foreach (var fDA in lFilesDA)
                                    new_listFilesDESADV.Add(fDA.Key, fDA.Value);
                                listOrdersFilesDESADV.Add(oLF.id, new_listFilesDESADV);
                            }
                        }
                    }

                    //Save XML files in ProcessingDir
                    string processingDir = Path.Combine(this.WorkDir, "processing");
                    Base.MakeFolder(processingDir);
                    foreach (var lOFDA in listOrdersFilesDESADV)
                    {
                        var k = lOFDA.Key;
                        var orderEDI = ordersListFiltered.FirstOrDefault(x => x.id == k);
                        if (orderEDI != null) {
                            var edrpouDir = Path.Combine(processingDir, orderEDI.company_from_edrpou);
                            Base.MakeFolder(edrpouDir);
                            foreach (var fDA in lOFDA.Value) {
                                var xmlFile = Path.Combine(edrpouDir, fDA.Key.ToString());
                                var desadvClass = fDA.Value;
                                //save xml file
                                if (XML.ConvertClassToXMLFile(xmlFile, desadvClass, null)) ResCount++;
                                /*
                                    var ordrspClass = new lib.classes.VchasnoEDI.ORDRSP();
                                    newFilepath = file+"_ordrsp";
                                    if (XML.ConvertClassToXMLFile(newFilepath, ordrspClass)) {}
                                */
                            }
                        }
                    }
                
                }
                catch (Exception ex)
                {
                    this.log("Error in actionOutBox(): " + ex.Message);
                }
                finally
                {
                    Base.SaveDirectory(Base.ArchivesDir, this.WorkDir);
                    //Base.CleanDirectory(this.WorkDir, null);
                }
            }

            __exit:
                this.log("ResCount: " + ResCount);
        }
    }
}
