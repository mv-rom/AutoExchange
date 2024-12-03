﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ae.lib;



namespace ae.services.EDI
{
    public class EDI : Service
    {
        private string WorkDir = "";
        public structure.ConfigClass config;

        public EDI(string theServiceName) : base(theServiceName)
        {
            this.config = Base.Config.ConfigSettings.Services.EDI;
        }

        public void log(string msg)
        {
            Base.Log("Service [" + this.GetType().Name + "]> " + msg);
        }


        private string CalcExecuteOrderDate(DateTime orderExecuteDate, string PlanningListDaysOfWeeek)
        {
            string[] plDoW = PlanningListDaysOfWeeek.Split(',');
            Array.Sort(plDoW);

            if (orderExecuteDate.Day > DateTime.Now.Day) {
                int execDow = (int)orderExecuteDate.DayOfWeek;
                int firstPlanningDayOfWeek = 0;
                int daysDifference = 0;
                foreach (var p in plDoW)
                {
                    int res_p = 0;
                    if (Int32.TryParse(p, out res_p) && 0 < res_p && res_p <= 7) {
                        firstPlanningDayOfWeek = Int32.Parse(plDoW[0]);
                        if (res_p >= execDow) {
                            daysDifference = res_p - execDow;
                            break;
                        }
                    }
                }
                daysDifference = (daysDifference == 0) ? (7 - execDow + firstPlanningDayOfWeek) : daysDifference;
                return orderExecuteDate.AddDays(daysDifference).ToString();
            }
            return "";
        }

        private List<tools.VchasnoEDI.structure.Order> getOrdersFromEDI(tools.VchasnoEDI.API api)
        {
            //getting needed documents
            var yesterdayDT = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd"); //DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
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
                if (companyList.FirstOrDefault(t => (
                    item.company_to_edrpou.Equals(ourEdrpou) && t.edrpou.Equals(item.company_from_edrpou))
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
            if (gln.Length > 0)
            {
                var listTT = new List<structure._1C.TTbyGLN_Item>();
                foreach (var s in source)
                {
                    var self_gln = s.as_json.seller_gln;
                    if (gln.Equals(self_gln))
                    {
                        bool found = false;
                        var glnTT = long.Parse(s.as_json.buyer_gln);
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
                                codeTT = new structure._1C.codeTT()
                                {
                                    part1 = 0,
                                    part2 = 0,
                                    part3 = 0
                                }
                            });
                        }
                    }
                }

                if (listTT.Count() > 0)
                {
                    string report1cName = "tt_by_gln";
                    var input = new structure._1C.TTbyGLN() {
                        list = listTT
                    };

                    var output = _1C.runReportProcessingData<structure._1C.TTbyGLN>(WorkDir, this.Reports1CDir, report1cName, input);
                    if (output != null)
                    {
                        var output_listTT = output.list;

                        int i = 0;
                        while (i < listTT.Count())
                        {
                            var glnTT = listTT[i].glnTT;
                            var glnTT_gruz = listTT[i].glnTT_gruz;

                            var output_item = output_listTT.
                                Where(x => x.glnTT == glnTT).                       //Where(x => x.glnTT.Equals(glnTT)).
                                FirstOrDefault(y => y.glnTT_gruz == glnTT_gruz);    //FirstOrDefault(y => y.glnTT_gruz.Equals(glnTT_gruz));
                            if (output_item != null) {
                                listTT[i].codeTT = output_item.codeTT;
                                i++;
                            } else {
                                listTT.RemoveAt(i);
                            }
                        }
                        return listTT;
                    }
                    else
                    {
                        this.log(
                            "Warning in getTTbyGLNfrom1C(): after do report [" + report1cName + "]!"
                        );
                    }
                }
            }
            else {
                this.log(
                    "Warning in getTTbyGLNfrom1C(): hasn't parameter [gln] in config!"
                );
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

                var execD = DateTime.Parse(s.as_json.date_expected_delivery).AddDays(1);
                string date_expected_delivery = "";
                foreach (var item in this.config.Companies)
                {
                    if (long.Parse(item.gln) == glnTT && item.gruzs != null) {
                        var gruz = item.gruzs.FirstOrDefault(t => (t.gln.Length > 0 && long.Parse(t.gln) == glnTT_gruz));
                        if (gruz != null) {
                            date_expected_delivery = this.CalcExecuteOrderDate(execD, gruz.executionDayOfWeek);
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

                if (found)
                {
                    var foundTT = listTT[found_i];

                    //search group in groupPP
                    bool tt_found_in_PP = false;
                    int tt_found_in_PP_i = 0;
                    foreach (var g in groupPP)
                    {
                        if (g.codeTT_part1 == foundTT.codeTT.part1 &&
                            g.codeTT_part2 == foundTT.codeTT.part2 &&
                            g.codeTT_part3 == foundTT.codeTT.part3 &&
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
                            codeTT_part1 = foundTT.codeTT.part1,
                            codeTT_part2 = foundTT.codeTT.part2,
                            codeTT_part3 = foundTT.codeTT.part3,
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
                        var product_code = long.Parse(it.product_code);
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
                } else {
                    this.log(
                        "Warning in getProductProfilesOfTTfrom1C(): " +
                        "not found TT with GLN [" + glnTT + ", " + glnTT_gruz + "(" + delivery_address + ")]!"
                    );
                }
            }

            if (groupPP.Count() > 0)
            {
                string report1cName = "product_profiles";
                var input = new structure._1C.ProductProfiles() {
                    group = groupPP
                };

                var output = ae.lib._1C.runReportProcessingData<structure._1C.ProductProfiles>(WorkDir, this.Reports1CDir, report1cName, input);
                if (output != null)
                {
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
                } else {
                    this.log(
                        "Warning in getProductProfilesOfTTfrom1C(): after do report [" + report1cName + "]!"
                    );
                }
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
                    if (splittedOrders != null && splittedOrders.ContainsKey(found_key))
                    {
                        //TODO: if (!s.deal_status.Equals("new")) { }
                        splittedOrders[found_key].deal_status = deal_status;
                        dictSO.Add(found_key, splittedOrders[found_key]);
                    }
                    else
                    {
                        var found_item = groupPP.Where(x => (x.id == id)).FirstOrDefault();
                        if (found_item != null)
                        {
                            var newItems = new List<structure.SplittedOrdersClass_Order>();
                            try
                            {
                                var listItems = o.as_json.items;
                                foreach (var it in listItems)
                                {
                                    var ean13 = long.Parse(it.product_code);
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
                                            qty = float.Parse(s_qty),
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
                                var execD = DateTime.Parse(o.as_json.date_expected_delivery).AddDays(1);
                                string date_expected_delivery = "";
                                foreach (var item in this.config.Companies)
                                {
                                    if (long.Parse(item.gln) == glnTT && item.gruzs != null) {
                                        var gruz = item.gruzs.FirstOrDefault(t => long.Parse(t.gln) == glnTT_gruz);
                                        if (gruz != null) {
                                            date_expected_delivery = this.CalcExecuteOrderDate(execD, gruz.executionDayOfWeek);
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
                        else
                        {
                            this.log("Warning in doSplittingUpOrders(): not found order with id [" + id + "] in ProductProfiles_Group!");
                            break;
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

            int nCount = 0;
            foreach (var so in source)
            {
                var preSalesDetails = new List<tools.AbInbevEfes.structure.preSalesDetails>();
                foreach (var it in so.Value.Items)
                {
                    preSalesDetails.Add(new tools.AbInbevEfes.structure.preSalesDetails()
                    {
                        productCode = it.codeKPK.ToString(),
                        basePrice = it.basePrice.ToString("F4", CultureInfo.InvariantCulture),
                        qty = it.qty.ToString("F4", CultureInfo.InvariantCulture),
                        lotId = "-",
                        promoType = it.promoType.ToString(), //1 - vstugnu kyputu, 0 - ni (default)
                        vat = "20.0" // 20.0% - PDV
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

                    var AbInbevEfesAPI = tools.AbInbevEfes.API.getInstance(this.config, WorkDir);
                    if (AbInbevEfesAPI != null)
                    {
                        var PreSaleResult = AbInbevEfesAPI.getPreSales(request);
                        if (PreSaleResult != null)
                        {
                            if (PreSaleResult.result != null)
                            {
                                //updating SplittedOrders
                                source[so.Key].resut_orderNo = PreSaleResult.result.orderNo.ToString();
                                source[so.Key].result_outletId = PreSaleResult.result.outletId.ToString();

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
                            else
                            {
                                var ErrorResult = AbInbevEfesAPI.getLogs(PreSaleResult.traceIdentifier);
                                if (ErrorResult != null)
                                {
                                    this.log(ErrorResult.message);
                                }
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
                            items = newItems
                        });
                    }
                }
            }

            if (newOrders.Count() > 0)
            {
                string report1cName = "vkachka_zayavok";
                var input = new structure._1C.NewOrders() {
                    orders = newOrders
                };

                var output = _1C.runReportProcessingData<structure._1C.NewOrders>(WorkDir, this.Reports1CDir, report1cName, input);
                if (output != null) {
                    var output_Orders = output.orders;
                    foreach (var oO in output_Orders)
                    {
                        if (source.ContainsKey(oO.id)) {
                            source[oO.id].status1c = oO.returnStatus;
                        }
                    }
                    return true;
                }
            }
            return false;
        }



        public void actionInBox()
        {
            int ResCount = 0;
            var fileJSON = "orders.json";
            //string dirName = "InBox";
            if (Directory.Exists(this.InboxDir))
            {
                this.WorkDir = this.InboxDir;
                try
                {
                    var instVchasnoAPI = tools.VchasnoEDI.API.getInstance(this.config);
                    var ordersListFiltered = getOrdersFromEDI(instVchasnoAPI);
                    if (ordersListFiltered != null && ordersListFiltered.Count > 0)
                    {
                        //var fp = Path.Combine(dirPath, fileJSON);
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
                }
            }
            this.log("ResCount: " + ResCount);
        }

        public void actionOutBox()
        {
            int ResCount = 0;
            string dirName = "OutBox";
            if (Directory.Exists(this.OutboxDir))
            {
                this.WorkDir = this.OutboxDir;
                try
                {
                    var yesterdayDT = DateTime.Now.AddDays(-3).ToString("yyyy-MM-dd");
                    //var nowDT = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
                    //var yesterdayDT = DateTime.Now.ToString("yyyy-MM-dd");
                    var nowDT = DateTime.Now.ToString("yyyy-MM-dd");

                    var VchasnoAPI = tools.VchasnoEDI.API.getInstance(this.config); //this.config.VchasnoEDI_ApiSetting
                    var ordersList = VchasnoAPI.getListDocuments(yesterdayDT, nowDT, 1);
                    if (ordersList == null || ordersList.Count() <= 0)
                        goto __exit;

                    //filter only Orders
                    var ordersListFiltered = new List<tools.VchasnoEDI.structure.Order>();
                    foreach (var item in ordersList.Where(x => x.type == 1))
                    {
                        ordersListFiltered.Add(item);
                    }
                    ordersList = null;
                    if (ordersListFiltered.Count() <= 0)
                        goto __exit;

                    //var ordersListFilteredMaped = ordersListFiltered.ConvertAll<lib.classes.VchasnoEDI.Order>(x => VchasnoAPI.getDocument(x.id));
                    //ordersListFiltered = null;

                    //processing in OutboxDir
                    string pattern1 = @"^.+_DESADV_.+\.xml$";
                    var dirsList = Directory.GetDirectories(this.OutboxDir);
                    foreach (var dPath in dirsList)
                    {
                        if (Directory.Exists(dPath))
                        {
                            var dName = new DirectoryInfo(dPath).Name;

                            //filter by company gln
/*                          var Company = this.config.Companies.FirstOrDefault(x => x.erdpou == dName);
                            if (Company != null) {
                                var gln = Company.gln;
                            }
 */                           //var ORDRSP = 1;

            var xmlFilesList = Directory.GetFiles(dPath);
                            foreach (var file in xmlFilesList)
                            {
                                //parse *_DESADV_*.xml:
                                if (Regex.IsMatch(file, pattern1) && File.Exists(file))
                                {
                                    var desadvClass = XML.ConvertXMLFileToClass<tools.VchasnoEDI.structure.DESADV>(file);
                                    if (desadvClass != null)
                                    {
                                        tools.VchasnoEDI.structure.Order _founded = null;
                                        foreach (var item in ordersListFiltered.Where(x => x.number == desadvClass.ORDERNUMBER))
                                        {
                                            _founded = item;
                                            break;
                                        }

                                        if (_founded != null)
                                        {
                                            var posList = desadvClass.HEAD.PACKINGSEQUENCE.POSITION;
                                            for (int i = 0; i < posList.Count(); i++)
                                            {
                                                var item = _founded.as_json.items.FirstOrDefault<tools.VchasnoEDI.structure.OrderDataItem>(
                                                    x => x.product_code == posList[i].PRODUCT
                                                );
                                                if (item != null)
                                                {
                                                    posList[i].PRODUCTIDBUYER = "" + (string.IsNullOrEmpty(item.buyer_code) ? "0" : item.buyer_code);
                                                }
                                            }
                                        }

                                        //this.log(desadvClass.HEAD.BUYER);
                                        string newFilepath = file; //file + "_"
                                        if (File.Exists(newFilepath)) File.Delete(newFilepath);

                                        if (XML.ConvertClassToXMLFile(newFilepath, desadvClass, null)) ResCount++;
                                        /*
                                            if (Company != null) {
                                                //add to orrsp array
                                                var ordrspClass = new lib.classes.VchasnoEDI.ORDRSP();
                                                newFilepath = file+"_ordrsp";
                                                if (XML.ConvertClassToXMLFile(newFilepath, ordrspClass)) { 
                                                }
                                            }
                                        */
                                    }
                                    else
                                    {
                                        if (File.Exists(file)) File.Delete(file);
                                    }
                                }
                            }

/*                            if (Company != null)
                            {
                                //ORDRSP
                            }
*/                        }
                    }
                }
                catch (Exception ex)
                {
                    this.log("Error in actionOutBox(): " + ex.Message);
                }
                finally
                {
                    Base.SaveDirectory(this.OutboxDir, dirName, Base.ArchivesDir);
                }
            }

__exit:
            this.log("ResCount: " + ResCount);
        }
    }
}
