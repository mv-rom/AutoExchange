using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
//using System.Threading.Tasks;
using ae.lib;
//using System.Security.Cryptography;
//using static System.Net.Mime.MediaTypeNames;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using System.Xml.XPath;
using ae.lib.classes.Base1C;
using ae.lib.classes.AE;
using ae.lib.classes.AbInbevEfes;
//using Newtonsoft.Json.Linq;

namespace ae
{
    public static class dllProgram
    {
        //[STAThread]
        public static void Entry()
        {
            Base.Init();

            processInBox();
            //processOutBox();

            /*
                Base.Scheduler = Scheduler.getInstance();
                try
                {
                    Base.Scheduler.Run();
                }
                catch (Exception ex)
                {
                    string msg = "Error in Program.Main(): "+ex.Message;
                    Base.Log(msg);
                    throw new Exception(msg);
                }
                finally
                {
                    Base.Scheduler.DeInit();
                }
            */

            // ротация файлов архивов (удаление старых) 
            // в архивной папке через период (количество дней)
            int day = 14;
            // для архивов данных
            string pattern = "_[A-Za-z0-9_-]*.zip";
            Base.RotateArchives(Base.ArchivesDir, pattern, day);

            // для архивов логов
            pattern = "Log_[A-Za-z0-9_-]*.zip";
            Base.RotateArchives(Base.ArchivesDir, pattern, day);

            Base.Log("");
            Base.Log(">>> Работа скрипта завершена.");

            Base.deInit();

            Base.SaveLog(Base.ArchivesDir, "ae.log");
            File.Delete(Path.Combine(Base.RunDir, "ae.log"));
        }




/*
        private static void expandDictFromList(
            List<lib.classes.VchasnoEDI.Order> source, 
            ref Dictionary<string, lib.classes.VchasnoEDI.Order> destination
        )
        {
            foreach (var s in source) {
                if (!s.deal_status.Equals("new") && existKey.Length > 0) {
                    destination[existKey].deal_status = s.deal_status;
                    //TODO ?
                }
            }
        }
*/

        private static List<lib.classes.Base1C.TTbyGLN_Item> getTTbyGLNfrom1C(List<lib.classes.VchasnoEDI.Order> source)
        {
            string report1cName = "EDI_tt_by_gln";
            List<lib.classes.Base1C.TTbyGLN_Item> result = null;

            string gln = "";
            if (Base.Config.ConfigSettings.BaseSetting.TryGetValue("gln", out gln))
            {
                var listTT = new List<lib.classes.Base1C.TTbyGLN_Item>();
                foreach (var s in source)
                {
                    var self_gln = s.as_json.seller_gln;
                    if (gln.Equals(self_gln)) {
                        bool found = false;
                        var glnTT = long.Parse(s.as_json.buyer_gln);
                        var glnTT_gruz = long.Parse(s.as_json.delivery_gln);

                        foreach (var l in listTT)
                        {
                            if (glnTT == l.glnTT && glnTT_gruz == l.glnTT_gruz) {
                                found = true;
                                break;
                            }
                        }

                        if (!found) {
                            listTT.Add(
                                new lib.classes.Base1C.TTbyGLN_Item() {
                                    glnTT = glnTT,
                                    glnTT_gruz = glnTT_gruz,
                                    codeTT = new lib.classes.Base1C.codeTT() {
                                        part1 = 0,
                                        part2 = 0,
                                        part3 = 0
                                    }
                                }
                            );
                        }
                    }
                }

                if (listTT.Count() > 0) {
                    var input = new lib.classes.Base1C.TTbyGLN() {
                        list = listTT
                    };

                    var output = ae.lib._1C.runReportProcessingData<lib.classes.Base1C.TTbyGLN>(report1cName, input);
                    if (output != null) {
                        var output_listTT = output.list;

                        int i = 0;
                        while (i < listTT.Count()) {
                            var glnTT =      listTT[i].glnTT;
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

                        result = listTT;
                        output_listTT = null;
                    }
                }
            }
            else {
                Base.Log(
                    "Warning in getTTbyGLNfrom1C: before do report " + 
                    "[" + report1cName + "] " +
                    "hasn't parametr [gln] in config!"
                );
            }
            return result;
        }

        private static List<lib.classes.Base1C.ProductProfiles_Group> getProductProfilesOfTTfrom1C(
            List<lib.classes.VchasnoEDI.Order> source,
            List<lib.classes.Base1C.TTbyGLN_Item> listTT
        )
        {
            string report1cName = "EDI_product_profiles";
            List<lib.classes.Base1C.ProductProfiles_Group> result = null;

            var groupPP = new List<lib.classes.Base1C.ProductProfiles_Group>();
            foreach (var s in source)
            {
                var id = s.id;
                var glnTT = long.Parse(s.as_json.buyer_gln);
                var glnTT_gruz = long.Parse(s.as_json.delivery_gln);
                var date_expected_delivery = s.as_json.date_expected_delivery;

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

                if (found) {
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

                    ProductProfiles_Group pp_g = null;
                    if (!tt_found_in_PP) {
                        pp_g = new ProductProfiles_Group() {
                            id = id,
                            ExecutionDate = date_expected_delivery,
                            codeTT_part1 = foundTT.codeTT.part1,
                            codeTT_part2 = foundTT.codeTT.part2,
                            codeTT_part3 = foundTT.codeTT.part3,
                            list = new List<ProductProfiles_Item>()
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
                        foreach (var l in groupPP[tt_found_in_PP_i].list)
                        {
                            if (l.EAN == product_code) item_found = true;
                        }

                        if (!item_found) {
                            pp_g.list.Add(new ProductProfiles_Item() {
                                EAN = product_code,
                                Title = title,
                                Number = num
                            });
                            num++;
                        }
                    }
                } else {
                    Base.Log1(
                        "Warning in getProductProfilesOfTTfrom1C: "+
                        "not found TT with GLN ["+glnTT+","+glnTT_gruz+"]!"
                    );
                }
            }

            if (groupPP.Count() > 0)
            {
                var input = new lib.classes.Base1C.ProductProfiles() {
                    group = groupPP
                };

                var output = ae.lib._1C.runReportProcessingData<lib.classes.Base1C.ProductProfiles>(report1cName, input);
                if (output != null) {
                    var output_groupPP = output.group;
                    int i = 0;
                    while (i < groupPP.Count())
                    {
                        var g = groupPP[i];
                        var output_item = output_groupPP.Where(x => (x.id == g.id)).FirstOrDefault();
                        if (output_item != null) {
                            int j = 0;
                            while (j < g.list.Count())
                            {
                                var g_el = g.list[j];
                                var output_item_el = output_item.list.Where(x => (x.EAN == g_el.EAN)).FirstOrDefault();
                                if (output_item_el != null) {
                                    g_el.ProductCode = output_item_el.ProductCode;
                                    g_el.ProductType = output_item_el.ProductType;
                                    g_el.BasePrice = output_item_el.BasePrice;
                                    j++;
                                } else {
                                    g.list.RemoveAt(j);
                                }
                            }
                            i++;
                        } else {
                            groupPP.RemoveAt(i);
                        }
                    }
                    result = groupPP;
                    output_groupPP = null;
                }
            }
            return result;
        }

        private static Dictionary<string, lib.classes.AE.SplittedOrdersClass> doSplittingUpOrders(
            List<lib.classes.VchasnoEDI.Order> Orders,
            List<lib.classes.Base1C.ProductProfiles_Group> groupPP,
            Dictionary<string, lib.classes.AE.SplittedOrdersClass> source2
        )
        {
            Dictionary<string, lib.classes.AE.SplittedOrdersClass> result = null;
            var dictSO = new Dictionary<string, lib.classes.AE.SplittedOrdersClass>();
            foreach (var o in Orders)
            {
                var id = o.id;
                //enumeration by type
                for (int type_of_product = 0; type_of_product < 4; type_of_product++)
                {
                    var found_key = id + "@" + type_of_product;
                    if (source2 != null && source2.ContainsKey(found_key)) {
                        dictSO.Add(found_key, source2[found_key]);
                    } else {
                        var found_item = groupPP.Where(x => (x.id == id)).FirstOrDefault();
                        if (found_item != null) {
                            var newItems = new List<SplittedOrdersClass_Order>();

                            var listItems = o.as_json.items;
                            foreach (var it in listItems)
                            {
                                var ean13 = long.Parse(it.product_code);
                                var found_list_item = found_item.list.
                                    Where(x => (x.EAN == ean13 && x.ProductType == type_of_product)).FirstOrDefault();
                                if (found_list_item != null) {
                                    newItems.Add(new SplittedOrdersClass_Order() {
                                        ean13 = ean13,
                                        codeKPK = found_list_item.ProductCode,
                                        basePrice = found_list_item.BasePrice,
                                        qty = float.Parse(it.quantity),
                                        promoType = 0,
                                        totalDiscount = 0
                                    });
                                }
                                var title = it.title;
                            }

                            //add new
                            if (newItems.Count > 0) {
                                dictSO.Add(found_key, new SplittedOrdersClass()
                                {
                                    id = id,
                                    ae_id = Base.genarateKeyN(1),
                                    orderNumber = o.number,
                                    OrderExecutionDate = DateTime.Parse(found_item.ExecutionDate),
                                    codeTT_part1 = found_item.codeTT_part1,
                                    codeTT_part2 = found_item.codeTT_part2,
                                    codeTT_part3 = found_item.codeTT_part3,
                                    Items = newItems
                                });
                            }
                        }
                    }
                }
            }

            if (dictSO.Count > 0) {
                result = dictSO;
            }
            return result;
        }

        private static bool CheckAndAddOrdersIn1C(
            Dictionary<string, lib.classes.AE.SplittedOrdersClass> source
        )
        {
            bool result = false;
            string report1cName = "EDI_vkachka_zayavok";


            var newOrders = new List<NewOrders_Order>();
            foreach (var so in source)
            {
                int num = 1;
                var newItems = new List<NewOrders_Item>();
                foreach (var it in so.Value.Items)
                {
                    newItems.Add(new NewOrders_Item() {
                        Number = num,
                        codeKPK = it.codeKPK,
                        BasePrice = it.basePrice,
                        Akcya = it.totalDiscount
                    });
                    num++;
                }

                if (newItems.Count > 0) {
                    newOrders.Add(new NewOrders_Order()
                    {
                        orderNo = so.Value.resut_orderNo,
                        outletCode = so.Value.result_outletCode,
                        ExecutionDate = so.Value.OrderExecutionDate.ToString(),
                        codeTT_part1 = so.Value.codeTT_part1,
                        codeTT_part2 = so.Value.codeTT_part2,
                        codeTT_part3 = so.Value.codeTT_part3,
                        items = newItems
                    });
                }
            }

            if (newOrders.Count() > 0) {
                var input = new lib.classes.Base1C.NewOrders(){
                    orders = newOrders
                };

                var output = ae.lib._1C.runReportProcessingData<lib.classes.Base1C.NewOrders>(report1cName, input);
                if (output != null) {
                    var output_Orders = output.orders;
                    int i = 0;
                    while (i < output_Orders.Count())
                    {
                        var o = output_Orders[i];
                    }

                    result = true;
                }
            }
            return result;
        }



        public static void processInBox()
        {
            int ResCount = 0;
            FileStream fs;
            var dirPath = Base.InboxDir;
            var fileJSON_SplittedOrders = "ae.json";
            

            if (!Directory.Exists(dirPath))
                goto __exit;

            try
            {
                var VchasnoAPI = lib.classes.VchasnoEDI.API.getInstance();
                //getting needed documents
                var yesterdayDT = DateTime.Now.AddDays(-3).ToString("yyyy-MM-dd");
                //var nowDT = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
                var nowDT = DateTime.Now.ToString("yyyy-MM-dd");

                //var obj1 = VchasnoAPI.getDocument("0faac24e-1960-3b29-94a1-1384badb60b7");

                var ordersList = VchasnoAPI.getListDocuments(yesterdayDT, nowDT, 1);
                if (ordersList == null || ordersList.Count() <= 0)
                    throw new Exception("ordersList = null");

                //filter only Orders
                var ordersListFiltered = new List<lib.classes.VchasnoEDI.Order>();
                foreach (var item in ordersList.Where(x => x.type == 1))
                {
                    ordersListFiltered.Add(item);
                }

                if (ordersListFiltered.Count() <= 0)
                    goto __exit;

                //Filter by deal_id (like order_number)
                int i = 0;
                int count = ordersListFiltered.Count();
                while (i < ordersListFiltered.Count)
                {
                    var item = ordersListFiltered[i];
                    var temp1 = ordersList.FirstOrDefault(t => (t.type > 1 && t.deal_id.Equals(item.deal_id)));
                    if (temp1 != null) {
                        ordersListFiltered.Remove(item);
                        count = ordersListFiltered.Count();
                    } else {
                        i++;
                    }
                }
                ordersList = null;


                if (ordersListFiltered != null && ordersListFiltered.Count > 0) {
                    var TTbyGLN_List = getTTbyGLNfrom1C(ordersListFiltered);
                    if (TTbyGLN_List == null)
                        throw new Exception("Result of getTTbyGLNfrom1C is null.");

                    var ProductProfiles = getProductProfilesOfTTfrom1C(ordersListFiltered, TTbyGLN_List);
                    if (ProductProfiles == null)
                        throw new Exception("Result of getProductProfilesOfTTfrom1C is null.");

                    if (ProductProfiles.Count > 0) {
                        string jsonStr = "";
                        var filePathSO = Path.Combine(dirPath, fileJSON_SplittedOrders);
                        if (!File.Exists(filePathSO)) {
                            fs = File.Create(filePathSO);
                            fs.Close();
                        } else {
                            jsonStr = File.ReadAllText(filePathSO);
                        }

                        var savedSplittedOrders = JSON.fromJSON<Dictionary<string, lib.classes.AE.SplittedOrdersClass>>(jsonStr);
                        jsonStr = "";
                        var SplittedOrders = doSplittingUpOrders(ordersListFiltered, ProductProfiles, savedSplittedOrders);
                        if (SplittedOrders == null)
                            throw new Exception("Result of doSplittingUpOrders is null.");

                        var AbInbevEfesAPI = lib.classes.AbInbevEfes.API.getInstance();
                        if (AbInbevEfesAPI != null) {
                            foreach (var so in SplittedOrders)
                            {
                                var request = new PreSalesRequest() {
                                    preSaleNo = so.Value.ae_id, //or so.Key
                                    custOrderNo = so.Value.id,
                                    outletCode =
                                        so.Value.codeTT_part1 + "\\" +
                                        so.Value.codeTT_part1 + "\\" +
                                        so.Value.codeTT_part1,
                                    preSaleType = 6, //EDI order
                                    dateFrom = DateTime.Now.ToString(),
                                    dateTo = so.Value.OrderExecutionDate.ToString(),
                                    warehouseCode = Base.torg_sklad,
                                    vatCalcMod = 1, //price with PDV
                                    custId = int.Parse(Base.torg_sklad)
                                };

                                var preSalesDetails = new List<preSalesDetails>();
                                foreach (var it in so.Value.Items)
                                {
                                    preSalesDetails.Add(new preSalesDetails(){
                                        productCode = it.codeKPK,
                                        basePrice = it.basePrice,
                                        qty = it.qty,
                                        lotId = "-",
                                        promoType = it.promoType, //1 - vstugnu kuputu, 0 - ni (default)
                                        vat = 20.0F // 20.0% - PDV
                                    });
                                }

                                if (preSalesDetails.Count > 0) {
                                    request.preSalesDetails = preSalesDetails;

                                    throw new Exception("STOP");

                                    var PreSaleResult = AbInbevEfesAPI.getPreSaleProfile(request);
                                    if (PreSaleResult != null) {
                                        //update SplittedOrders
                                    }
                                }
                            }

                            if (CheckAndAddOrdersIn1C(SplittedOrders))
                                Base.Log("Processing orders in 1C is successful.");
                            else
                                Base.Log("Some problems with processing orders in 1c!");


                            //save Splited Order List
                            jsonStr = JSON.toJSON(SplittedOrders);
                            File.WriteAllText(filePathSO + "_temp", jsonStr);
                            if (File.Exists(filePathSO))
                                File.Delete(filePathSO);
                            File.Move(filePathSO + "_temp", filePathSO);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Base.Log("Error in processInBox(): " + ex.Message);
            }
            finally
            {
                if (_1C.Instance != null)
                    _1C.Instance.runExit();
            }

        __exit:
            {
                Base.Log("ResCount: " + ResCount);
            }
        }

        public static void processOutBox()
        {
            int ResCount = 0;

            try
            {
                if (!Directory.Exists(Base.OutboxDir))
                    goto __exit;
                var dirsList = Directory.GetDirectories(Base.OutboxDir);


                var yesterdayDT = DateTime.Now.AddDays(-7).ToString("yyyy-MM-dd");
                var nowDT = DateTime.Now.ToString("yyyy-MM-dd");

                var VchasnoAPI = lib.classes.VchasnoEDI.API.getInstance();
                var ordersList = VchasnoAPI.getListDocuments(yesterdayDT, nowDT, 1);
                if (ordersList == null || ordersList.Count() <= 0)
                    goto __exit;

                //filter only Orders
                var ordersListFiltered = new List<lib.classes.VchasnoEDI.Order>();
                foreach (var item in ordersList.Where(x => x.type == 1))
                {
                    ordersListFiltered.Add(item);
                }
                ordersList = null;
                if (ordersListFiltered.Count() <= 0)
                    goto __exit;
                
                //var ordersListFilteredMaped = ordersListFiltered.ConvertAll<lib.classes.VchasnoEDI.Order>(x => VchasnoAPI.getDocument(x.id));
                //ordersListFiltered = null;

                string pattern1 = @"^.+_DESADV_.+\.xml$";
                //processing in OutboxDir
                foreach (var dirPath in dirsList)
                {
                    if (Directory.Exists(dirPath)) {
                        var DirName = new DirectoryInfo(dirPath).Name;

                        //filter by company gln
                        var Company = Base.Config.ConfigSettings.Companies.FirstOrDefault(x => x.erdpou == DirName);
                        if (Company != null) {
                            var gln = Company.gln;
                        }
                        //var ORDRSP = 1;

                        var xmlFilesList = Directory.GetFiles(dirPath);
                        foreach (var file in xmlFilesList)
                        {
                            //parse *_DESADV_*.xml:
                            if (Regex.IsMatch(file, pattern1) && File.Exists(file)) {
                                var desadvClass = XML.ConvertXMLFileToClass<lib.classes.VchasnoEDI.DESADV>(file);
                                if (desadvClass != null) {
                                    lib.classes.VchasnoEDI.Order _founded = null;
                                    foreach (var item in ordersListFiltered.Where(x => x.number == desadvClass.ORDERNUMBER))
                                    {
                                        _founded = item;
                                        break;
                                    }

                                    if (_founded != null) {
                                        var posList = desadvClass.HEAD.PACKINGSEQUENCE.POSITION;
                                        for (int i = 0; i < posList.Count(); i++)
                                        {
                                            var item = _founded.as_json.items.FirstOrDefault<lib.classes.VchasnoEDI.OrderDataItem>(
                                                x => x.product_code == posList[i].PRODUCT
                                            );
                                            if (item != null) {
                                                posList[i].PRODUCTIDBUYER = "" + (item.buyer_code != null ? item.buyer_code : "0");
                                            }
                                        }
                                    }

                                    //Base.Log(desadvClass.HEAD.BUYER);
                                    string newFilepath = file; //file + "_"
                                    if (File.Exists(newFilepath))
                                        File.Delete(newFilepath);

                                    if (XML.ConvertClassToXMLFile(newFilepath, desadvClass, null))
                                        ResCount++;
/*
                                    if (Company != null) {
                                        //add to orrsp array
                                        var ordrspClass = new lib.classes.VchasnoEDI.ORDRSP();
                                        newFilepath = file+"_ordrsp";
                                        if (XML.ConvertClassToXMLFile(newFilepath, ordrspClass)) { 
                                        }
                                    }
*/
                                } else {
                                    if (File.Exists(file)) File.Delete(file);
                                }
                            }
                        }

                        if (Company != null) {
                            //ORDRSP
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Base.Log("Error in processOutBox(): " + ex.Message);
            }

            __exit:
                Base.Log("ResCount: "+ ResCount);
        }
    }

}
