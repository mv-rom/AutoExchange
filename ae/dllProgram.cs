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

        private static int initKeyN()
        {
            Random rnd = new Random();
            var n = rnd.Next(9999);
            return n;
        }

        private static string genarateKeyN(int n)
        {
            var ticks = Base.getCurentUnixDateTime();
            int whole = (int)ticks;
            int fraction = (int)((ticks % 1) * 100000000);
            return whole + "-" + fraction + "-" + n;
        }

        private static void expandDictFromList(
            List<lib.classes.VchasnoEDI.Order> source, 
            ref Dictionary<string, lib.classes.VchasnoEDI.Order> destination
        )
        {
            int n = initKeyN();

            if (destination == null) {
                destination = new Dictionary<string, lib.classes.VchasnoEDI.Order>();
            }

            foreach (var s in source) {
                bool isExist = false;
                string existKey = "";
                foreach (KeyValuePair<string, lib.classes.VchasnoEDI.Order> d in destination)
                {
                    if (s.id.Equals(d.Value.id)) {
                        isExist = true;
                        existKey = d.Key;
                        break;
                    }
                }
                if (!isExist) {
                    n += 1;
                    string key = genarateKeyN(n);
                    destination.Add(key, s);
                } else {
                    if (!s.deal_status.Equals("new") && existKey.Length > 0) {
                        destination[existKey].deal_status = s.deal_status;
                        //TODO ?
                    }
                }
            }
        }


        private static List<lib.classes.Base1C.TTbyGLN_Elements> getTTbyGLNfrom1C(
            Dictionary<string, lib.classes.VchasnoEDI.Order> src)
        {
            string report1cName = "EDI_tt_by_gln";
            List<lib.classes.Base1C.TTbyGLN_Elements> result = null;

            string gln = "";
            if (Base.Config.ConfigSettings.BaseSetting.TryGetValue("gln", out gln))
            {
                var listTT = new List<lib.classes.Base1C.TTbyGLN_Elements>();

                foreach (var s in src)
                {
                    var self_gln = s.Value.as_json.seller_gln;
                    if (gln.Equals(self_gln)) {
                        bool found = false;
                        var glnTT = s.Value.as_json.buyer_gln;
                        var glnTT_gruz = s.Value.as_json.delivery_gln;

                        foreach (var l in listTT)
                        {
                            if (glnTT.Equals(l.glnTT) && glnTT_gruz.Equals(l.glnTT_gruz)) {
                                found = true;
                                break;
                            }
                        }

                        if (!found) {
                            listTT.Add(
                                new lib.classes.Base1C.TTbyGLN_Elements() {
                                    glnTT = long.Parse(glnTT),
                                    glnTT_gruz = long.Parse(glnTT_gruz),
                                    externalCodeTT = new lib.classes.Base1C.ExternalCodeTT() {
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
                            var glnTT = listTT[i].glnTT;
                            var glnTT_gruz = input.list[i].glnTT_gruz;

                            var item = output_listTT.
                                Where(x => x.glnTT == glnTT).                       //Where(x => x.glnTT.Equals(glnTT)).
                                FirstOrDefault(y => y.glnTT_gruz == glnTT_gruz);    //FirstOrDefault(y => y.glnTT_gruz.Equals(glnTT_gruz));
                            if (item != null) {
                                listTT[i].externalCodeTT = item.externalCodeTT;
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
            Dictionary<string, lib.classes.VchasnoEDI.Order> src,
            List<lib.classes.Base1C.TTbyGLN_Elements> listTT
        )
        {
            string report1cName = "EDI_product_profiles";
            List<lib.classes.Base1C.ProductProfiles_Group> result = null;

            //var result = new Dictionary<string, lib.classes.Base1C.BasePriceForTT>();
            //var productTT = getProductTTfromOrders(src);

            var groupPP = new List<lib.classes.Base1C.ProductProfiles_Group>();
            foreach (var s in src)
            {
                var glnTT = long.Parse(s.Value.as_json.buyer_gln);
                var glnTT_gruz = long.Parse(s.Value.as_json.delivery_gln);

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

                    //search group in listPP
                    bool tt_found_in_PP = false;
                    int tt_found_in_PP_i = 0;
                    foreach (var g in groupPP)
                    {
                        if (g.externalCodeTT.part1 == foundTT.externalCodeTT.part1 &&
                            g.externalCodeTT.part2 == foundTT.externalCodeTT.part2 &&
                            g.externalCodeTT.part3 == foundTT.externalCodeTT.part3
                        ) {
                            tt_found_in_PP = true;
                            break;
                        }
                        tt_found_in_PP_i++;
                    }

                    ProductProfiles_Group pp_g = null;
                    if (!tt_found_in_PP)
                    {
                        pp_g = new ProductProfiles_Group()
                        {
                            externalCodeTT = new ExternalCodeTT()
                            {
                                part1 = foundTT.externalCodeTT.part1,
                                part2 = foundTT.externalCodeTT.part2,
                                part3 = foundTT.externalCodeTT.part3
                            },
                            list = new List<ProductProfiles_Elements>()
                        };
                        groupPP.Add(pp_g);
                    }
                    else
                    {
                        pp_g = groupPP[tt_found_in_PP_i];
                    }

                    //search items in listPP.group.list
                    var listItems = s.Value.as_json.items;
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
                            pp_g.list.Add(new ProductProfiles_Elements() {
                                EAN = product_code,
                                Title = title
                            });
                        }
                    }
                } else {
                    Base.Log(
                        "Warning in getProductProfilesOfTTfrom1C: "+
                        "not found TT with GLN ["+glnTT+","+glnTT_gruz+"]!"
                    );
                }
            }

            if (groupPP.Count() > 0)
            {
                var input = new lib.classes.Base1C.ProductProfiles() {
                    group = new List<lib.classes.Base1C.ProductProfiles_Group>()
                };

                var output = ae.lib._1C.runReportProcessingData<lib.classes.Base1C.ProductProfiles>(report1cName, input);
                if (output != null) {
                    var output_listPP = output.group;
                    for (int i = 0; i < groupPP.Count(); i++)
                    {
                        //var glnTT = groupPP[i].glnTT;
                        //var glnTT_gruz = input.group[i].glnTT_gruz;

                        var item = output_listPP.
                            Where(x => x.glnTT == glnTT).                       //Where(x => x.glnTT.Equals(glnTT)).
                            FirstOrDefault(y => y.glnTT_gruz == glnTT_gruz);    //FirstOrDefault(y => y.glnTT_gruz.Equals(glnTT_gruz));
                        if (item != null) {
                            groupPP[i].externalCodeTT = item.externalCodeTT;
                        }
                    }
                    result = groupPP;
                    output_listPP = null;
                }
            }
            return result;
        }

        private static bool processingDevidedOrders(
            Dictionary<string, lib.classes.Base1C.BasePriceForTT>  obj1,
            Dictionary<string, lib.classes.Base1C.BasePriceForTT> obj2,
            ref Dictionary<string, lib.classes.AE.DevidedOrder> dest
        )
        {
            return false;
        }

        private static Dictionary<string, lib.classes.Base1C.InBoxOrder> CombinePreSaleAndOrders(
            Object src,
            Dictionary<string,
            lib.classes.AE.DevidedOrder> dst
        )
        {
            var result = default(Dictionary<string, lib.classes.Base1C.InBoxOrder>);
            return result;
        }

        private static bool CheckAndAddOrdersIn1C(Dictionary<string, lib.classes.Base1C.InBoxOrder> src)
        {
            return false;
        }


        public static void processInBox()
        {
            int ResCount = 0;
            FileStream fs;
            var dirPath = Base.InboxDir;
            var fileJSONName = "ae.json";
            var fileJSON_DevidedOrders = "ae_devided_orders.json";
            

            if (!Directory.Exists(dirPath))
                goto __exit;

            try
            {
                var VchasnoAPI = lib.classes.VchasnoEDI.API.getInstance();
                //get all type documents
                var yesterdayDT = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
                //var nowDT = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
                var nowDT = DateTime.Now.ToString("yyyy-MM-dd");

                //var obj1 = VchasnoAPI.getDocument("0faac24e-1960-3b29-94a1-1384badb60b7");
                //var obj1_2 = VchasnoAPI.getDocument("0fa9ee04-195e-2057-4624-9351763bae61");
                //var obj2_2 = VchasnoAPI.getDocument("0fa9fea1-c97b-633b-727b-f51721eda6b6");

                //var obj2 = VchasnoAPI.getDocument("0faacfd1-f9a9-cc82-dcb5-c8577937bd10");
                ///var obj3 = VchasnoAPI.getDocument("0faacfe1-74ce-1e6f-b6ff-ae45e5a6d0e9");
                //var obj4 = VchasnoAPI.getDocument("0faacfe1-80a3-a6ce-483d-a67422f7a510");

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

                //ordersListFiltered = ordersListFiltered.ConvertAll<lib.classes.VchasnoEDI.Order>(x => VchasnoAPI.getDocument(x.id));
                
                //Filter by deal_id (order_number)
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


                string jsonStr = "";
                if (ordersListFiltered != null && ordersListFiltered.Count > 0) {
                    var filePath = Path.Combine(dirPath, fileJSONName);
                    if (!File.Exists(filePath)) {
                        fs = File.Create(filePath);
                        fs.Close();
                    } else {
                        jsonStr = File.ReadAllText(filePath);
                    }

                    //read Main Order List
                    var mainOrdersList = JSON.fromJSON<Dictionary<string, lib.classes.VchasnoEDI.Order>>(jsonStr);
                    jsonStr = "";
                    //expand Main Order List
                    expandDictFromList(ordersListFiltered, ref mainOrdersList);

                    //processing 1C
                    var TTbyGLN_List = getTTbyGLNfrom1C(mainOrdersList);
                    if (TTbyGLN_List == null)
                        throw new Exception("Result of getTTbyGLNfrom1C is null.");

                    var dictBasePriceTT = getProductProfilesOfTTfrom1C(mainOrdersList, TTbyGLN_List);
                    if (dictBasePriceTT == null)
                        throw new Exception("Result of getTTbyGLNfrom1C is null.");

                    if (dictBasePriceTT.Count > 0) {
                        var filePath_DevidedOrders = Path.Combine(dirPath, fileJSON_DevidedOrders);
                        if (!File.Exists(filePath_DevidedOrders)) {
                            fs = File.Create(filePath_DevidedOrders);
                            fs.Close();
                        } else {
                            jsonStr = File.ReadAllText(filePath_DevidedOrders);
                        }

                        //read Main Order List
                        var devidedOrders = JSON.fromJSON<Dictionary<string, lib.classes.AE.DevidedOrder>>(jsonStr);
                        //processingDevidedOrders(productTT, dictBasePriceTT, ref devidedOrders);

                        var AbInbevEfesAPI = lib.classes.AbInbevEfes.API.getInstance();
                        if (AbInbevEfesAPI != null) {
                            var PreSaleResult = AbInbevEfesAPI.getPreSaleProfile(devidedOrders);
                            var InBoxOrders = CombinePreSaleAndOrders(PreSaleResult, devidedOrders);
                            if (CheckAndAddOrdersIn1C(InBoxOrders))
                                Base.Log("AddtoA1C is successful.");
                            else
                                Base.Log("Is not add to 1C!");
                        }

                        //save Devided Order List
                        var jsonSubList = JSON.toJSON(devidedOrders);
                        File.WriteAllText(filePath_DevidedOrders + "_temp", jsonSubList);
                        if (File.Exists(filePath_DevidedOrders))
                            File.Delete(filePath_DevidedOrders);
                        File.Move(filePath_DevidedOrders + "_temp", filePath_DevidedOrders);
                    }

                    //save Main Order List
                    var jsonList = JSON.toJSON(mainOrdersList);
                    File.WriteAllText(filePath + "_temp", jsonList);
                    if (File.Exists(filePath))
                        File.Delete(filePath);
                    File.Move(filePath + "_temp", filePath);
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
