using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CumminsQualityInspection.QDASWebService;
using CumminsQualityInspection.Models;
using System.Data.SqlClient;
using System.Collections;
using System.Xml;

namespace CumminsQualityInspection.Controllers
{
    public class QualityController : Controller
    {

        // GET: Quality
        public ActionResult Index(String partIds)
        {

            String ModelsList = "";
            if (System.Configuration.ConfigurationManager.AppSettings["QDAS_MODELS"] != null)
            {
                ModelsList = System.Configuration.ConfigurationManager.AppSettings["QDAS_MODELS"].ToString();
            }

            String currentModel = "";

            if (System.Configuration.ConfigurationManager.AppSettings["QDAS_DEFAULT_MODEL"] != null)
            {
                currentModel = System.Configuration.ConfigurationManager.AppSettings["QDAS_DEFAULT_MODEL"].ToString();
            }

            HttpCookie reqCookies = Request.Cookies["QDAS_CURRENT_MODEL"];
            if (reqCookies != null)
            {

                currentModel = reqCookies.Value;
            }

            String dateFilter = "1W";
            string fromDate = "";
            string toDate = "";
            DateTime today = DateTime.Today;

            HttpCookie dateCookies = Request.Cookies["QDAS_DATE_FILTER"];
            if (reqCookies != null)
            {

                dateFilter = dateCookies.Value;
            }

            if (dateFilter == "Custom")
            {
                fromDate = Request.Cookies["QDAS_FROM_DATE"].Value;
                toDate = Request.Cookies["QDAS_TO_DATE"].Value;
            }
            else if (dateFilter == "1M")
            {
                toDate = today.ToString("yyyy-MM-dd");
                fromDate = today.AddMonths(-1).ToString("yyyy-MM-dd");
            }
            else if (dateFilter == "1D")
            {
                toDate = today.ToString("yyyy-MM-dd");
                fromDate = today.AddDays(-1).ToString("yyyy-MM-dd");
            }
            else
            {
                toDate = today.ToString("yyyy-mm-dd");
                fromDate = today.AddDays(-7).ToString("yyyy-mm-dd");
            }

            var connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["QDAS_SQL_Connection"].ToString();

            List<ProductQualityDetails> productList = new List<ProductQualityDetails>();
            String productDesc = "";

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    //access SQL Server and run your command
                    string query = "SELECT MERKMAL.MENENNMAS, TEIL.TETEILNR, TEIL.TEBEZEICH, TEIL.TEARBEITSGANG, WERTEVAR.WVUNTERS, WERTEVAR.WVMERKMAL, WERTEVAR.WVWERTNR, " +
                                   "WERTEVAR.WVWERT, WERTEVAR.WVDATZEIT, WERTEVAR.WVATTRIBUT, WERTEVAR.WVPRUEFER, WERTEVAR.WVMASCHINE, WERTEVAR.WV0055, " +
                                   "MERKMAL.MEMERKBEZ, MERKMAL.MEUGW, MERKMAL.MEOGW, MERKMAL.MEMERKKLASSE, ALARM_VALUES.ALARM_EW, WERTEVAR.WVTEIL, " +
                                    "TEIL.TEERZEUGNIS FROM((([WERTEVAR] left join TEIL on[WERTEVAR].WVTEIL = TEIL.TETEIL) " +
                                    "left join MERKMAL on [WERTEVAR].WVMERKMAL = MERKMAL.MEMERKMAL and WERTEVAR.WVTEIL = MERKMAL.METEIL) " +
                                    "left join ALARM_VALUES on [WERTEVAR].WVWERTNR = ALARM_VALUES.K0000 and WERTEVAR.WVTEIL = ALARM_VALUES.K1000  and WERTEVAR.WVMERKMAL = ALARM_VALUES.K2000) " +
                                    "where TEIL.TETEIL IN(" + partIds + ") AND " +
                                        "ALARM_VALUES.ALARM_DATETIME >= '" + fromDate + " 00:00:00' AND " +
                                        "ALARM_VALUES.ALARM_DATETIME <= '" + toDate + " 23:59:59' order by ALARM_VALUES.ALARM_EW, WERTEVAR.WVMERKMAL asc";

                    SqlCommand cmd = new SqlCommand(query, conn);

                    //open connection
                    conn.Open();
                    //execute the SQLCommand
                    SqlDataReader dr = cmd.ExecuteReader();

                    //check if there are records
                    if (dr.HasRows)
                    {
                        ProductQualityDetails product = new ProductQualityDetails();
                        product.characteristicsList = new List<CharacteristicsData>();
                        while (dr.Read())
                        {
                            product.productNr = dr.GetValue(1).ToString().Trim();
                            product.description = dr.GetValue(2).ToString().Trim();
                            product.partList = partIds;
                            product.productType = dr.GetValue(19).ToString().Trim();
                            product.modelType = currentModel;

                            CharacteristicsData charData = new CharacteristicsData();
                            charData.CharID = dr.GetValue(5).ToString().Trim();
                            charData.CharDesc = dr.GetValue(13).ToString().Trim();
                            charData.CharOp = dr.GetValue(3).ToString().Trim();
                            charData.CharClass = dr.IsDBNull(16) ? 2 : Convert.ToInt32(dr.GetValue(16));
                            charData.CharAlarm = dr.IsDBNull(17) ? 0 : (int)dr.GetValue(17);
                            if (charData.CharAlarm >= 1 && charData.CharAlarm <= 8)
                            {
                                product.status = (int)Status.STATUS_DANGER;
                            }
                            else if (charData.CharAlarm >= 16 && charData.CharAlarm <= 128)
                            {
                                if (product.status != (int)Status.STATUS_DANGER)
                                {
                                    product.status = (int)Status.STATUS_WARNING;
                                }
                            }

                            charData.CharNom = dr.GetValue(0).ToString().Trim();
                            charData.CharMin = dr.GetValue(14).ToString().Trim();
                            charData.CharMax = dr.GetValue(15).ToString().Trim();
                            charData.CharVal = dr.GetValue(7).ToString().Trim();
                            charData.CharDate = dr.GetValue(8).ToString().Trim().Split(' ')[0];
                            charData.CharTime = dr.GetValue(8).ToString().Trim().Split(' ')[1];

                            product.characteristicsList.Add(charData);

                            //display retrieved record (first column only/string value)
                        }

                        productList.Add(product);
                    }
                    else
                    {
                        
                    }
                    dr.Close();

                }

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("EXCEPTION: " + ex.Message);
            }

            return View(productList);
        }

        // GET: RiskMatrix
        public ActionResult RiskMatrix(String partIds, String charClass, String riskLevel)
        { 
            
            String ModelsList = "";
            if (System.Configuration.ConfigurationManager.AppSettings["QDAS_MODELS"] != null)
            {
                ModelsList = System.Configuration.ConfigurationManager.AppSettings["QDAS_MODELS"].ToString();
            }

            String currentModel = "";

            if (System.Configuration.ConfigurationManager.AppSettings["QDAS_DEFAULT_MODEL"] != null)
            {
                currentModel = System.Configuration.ConfigurationManager.AppSettings["QDAS_DEFAULT_MODEL"].ToString();
            }

            HttpCookie reqCookies = Request.Cookies["QDAS_CURRENT_MODEL"];
            if (reqCookies != null)
            {

                currentModel = reqCookies.Value;
            }

            var connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["QDAS_SQL_Connection"].ToString();

            List<ProductData> productList = new List<ProductData>();
            String productDesc = "";

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    //access SQL Server and run your command
                    string query = "Select TETEIL, TEBEZEICH, TEERZEUGNIS, TEARBEITSGANG, TETYP " +
                                   "from dbo.TEIL WHERE ";

                    if (partIds != null && partIds.Trim() != "")
                    {
                        query = query + " TETEIL IN (" + partIds + ") AND ";                        
                    }
                    query = query + " TETYP ='" + currentModel + "' ";

                    SqlCommand cmd = new SqlCommand(query, conn);

                    //open connection
                    conn.Open();
                    //execute the SQLCommand
                    SqlDataReader dr = cmd.ExecuteReader();
                    Hashtable productHashTable = new Hashtable();
                    //check if there are records
                    if (dr.HasRows)
                    {

                        while (dr.Read())
                        {
                            productDesc = dr["TEBEZEICH"].ToString().Trim();
                            string product = dr["TEERZEUGNIS"].ToString().Trim();

                            string partNr = dr["TETEIL"].ToString().Trim();

                            if (!productHashTable.ContainsKey(product))
                            {
                                productHashTable[product] = partNr;
                            }
                            else
                            {
                                productHashTable[product] = productHashTable[product] + "," + partNr;
                            }
                        }
                    }
                    else
                    {
                    }
                    dr.Close();

                    foreach (DictionaryEntry item in productHashTable)
                    {
                        ProductData product = new ProductData();
                        product.productType = item.Key.ToString(); 
                        product.modelType = currentModel;
                        product.description = productDesc;
                        product.partList = item.Value.ToString();
                        product.riskMatrix = new string[9];
                        product.charEvalList = new List<CharEvalData>();

                        bool evalChar = true;

                        if (partIds == null || partIds == "") evalChar = false;

                        GetRiskMatrixData(product, charClass, riskLevel, evalChar);

                        productList.Add(product);
                    }
                }

            }
            catch (Exception ex)
            {
                //display error message
            }

            if (partIds == null || partIds == "")
            {
                return View("RiskMatrixModel", productList);
            }

            if (charClass != null)
            {
                return View("RiskMatrixSingle", productList);
            }
            return View(productList);
        }

        [HttpGet]
        public string GetRiskMatrixData(ProductData product, String charClass, String riskLevel, bool evalChar)
        {
            string resultString = "";


            Console.WriteLine("Start Getting Cp,Cpk info " + DateTime.Now.ToString());


            List<QualityData> QualityDataList = new List<QualityData>();
            try
            {


                String lastNValues = "125";


                HttpCookie reqCookies = Request.Cookies["QDAS_Filter"];
                if (reqCookies != null)
                {

                    lastNValues = Request.Cookies["QDAS_Filter"].Value;
                }

                IQdas_Web_Serviceservice ws = new IQdas_Web_Serviceservice();
                int handle = -1;
                int result = -1;

                if (ws != null)
                {
                    result = ws.WebConnect(20, 44, "superuser", "superuser", "", out handle);
                }


                String FieldList = "";

                int queryHandle = -1;
                int SQLHandle = -1;
                int filterHandle = -1;

                string StatResult_str2;
                string StatResult_str3;
                string StatResult_str4;
                string StatResult_str5;
                int outputCount;
                double StatResult_dbl1;
                double StatResult_dbl2;
                double StatResult_dbl3;
                double StatResult_dbl4;
                double StatResult_dbl5;


                // TEBEREICH Plant
                // TEERZEUGNIS Product
                // [TEARBEITSGANG] Operation
                // [TEMASCHINEBEZ] Machine
                // [TEMASCHINENR] Pallet
                // [TETYP] Model
                // [TEBEZEICH] Description
                // [TEWERKSTATT] Component

                if (result != -1)
                {
                    String partListStr = "";
                    foreach (String part in product.partList.Split(','))
                    {
                        partListStr += "<Part key='" + part + "'/>";
                    }

                    result = ws.CreateQuery(handle, out queryHandle);

                    result = ws.CreateFilter(handle, 1, 0, lastNValues, 129, out filterHandle);

                    if (result == 0)
                    {
                        result = ws.AddFilterToQuery(handle, queryHandle, filterHandle, 2, 0, 0);

                        if (result == 0)
                        {

                            String partListXML = "<PartCharList>" + partListStr + "</PartCharList>";


                            result = ws.ExecuteQuery_Ext(handle, queryHandle, partListXML, false, true);
                            result = ws.EvaluateAllChars(handle);

                            int partCount = -1;
                            int charCount = -1;
                            result = ws.GetGlobalInfo(handle, 0, 0, 1, out partCount);

                            string StatResult_str1 = "";
                            //Get Cp Value

                            if (partCount > 0)
                            {
                                // Get RiskMatrix Data

                                if (charClass == null)
                                {

                                    result = ws.GetStatResultEx(handle, 20031, 11, 0, 0, 0, 4, 0, 2, 0,
                                                                out StatResult_str1, out StatResult_str2, out StatResult_str3, out StatResult_str4,
                                                                 out StatResult_str5, out outputCount, out StatResult_dbl1, out StatResult_dbl2,
                                                                 out StatResult_dbl3, out StatResult_dbl4, out StatResult_dbl5);

                                    product.riskMatrix[0] = result == 0 ? StatResult_str1 : "ERROR";

                                    result = ws.GetStatResultEx(handle, 20031, 21, 0, 0, 0, 4, 0, 2, 0,
                                                            out StatResult_str1, out StatResult_str2, out StatResult_str3, out StatResult_str4,
                                                             out StatResult_str5, out outputCount, out StatResult_dbl1, out StatResult_dbl2,
                                                             out StatResult_dbl3, out StatResult_dbl4, out StatResult_dbl5);

                                    product.riskMatrix[1] = result == 0 ? StatResult_str1 : "ERROR";

                                    result = ws.GetStatResultEx(handle, 20031, 31, 0, 0, 0, 4, 0, 2, 0,
                                                            out StatResult_str1, out StatResult_str2, out StatResult_str3, out StatResult_str4,
                                                             out StatResult_str5, out outputCount, out StatResult_dbl1, out StatResult_dbl2,
                                                             out StatResult_dbl3, out StatResult_dbl4, out StatResult_dbl5);

                                    product.riskMatrix[2] = result == 0 ? StatResult_str1 : "ERROR";

                                    result = ws.GetStatResultEx(handle, 20031, 12, 0, 0, 0, 4, 0, 2, 0,
                                                            out StatResult_str1, out StatResult_str2, out StatResult_str3, out StatResult_str4,
                                                             out StatResult_str5, out outputCount, out StatResult_dbl1, out StatResult_dbl2,
                                                             out StatResult_dbl3, out StatResult_dbl4, out StatResult_dbl5);

                                    product.riskMatrix[3] = result == 0 ? StatResult_str1 : "ERROR";

                                    result = ws.GetStatResultEx(handle, 20031, 22, 0, 0, 0, 4, 0, 2, 0,
                                                            out StatResult_str1, out StatResult_str2, out StatResult_str3, out StatResult_str4,
                                                             out StatResult_str5, out outputCount, out StatResult_dbl1, out StatResult_dbl2,
                                                             out StatResult_dbl3, out StatResult_dbl4, out StatResult_dbl5);

                                    product.riskMatrix[4] = result == 0 ? StatResult_str1 : "ERROR";

                                    result = ws.GetStatResultEx(handle, 20031, 32, 0, 0, 0, 4, 0, 2, 0,
                                                            out StatResult_str1, out StatResult_str2, out StatResult_str3, out StatResult_str4,
                                                             out StatResult_str5, out outputCount, out StatResult_dbl1, out StatResult_dbl2,
                                                             out StatResult_dbl3, out StatResult_dbl4, out StatResult_dbl5);

                                    product.riskMatrix[5] = result == 0 ? StatResult_str1 : "ERROR";

                                    result = ws.GetStatResultEx(handle, 20031, 13, 0, 0, 0, 4, 0, 2, 0,
                                                            out StatResult_str1, out StatResult_str2, out StatResult_str3, out StatResult_str4,
                                                             out StatResult_str5, out outputCount, out StatResult_dbl1, out StatResult_dbl2,
                                                             out StatResult_dbl3, out StatResult_dbl4, out StatResult_dbl5);

                                    product.riskMatrix[6] = result == 0 ? StatResult_str1 : "ERROR";

                                    result = ws.GetStatResultEx(handle, 20031, 23, 0, 0, 0, 4, 0, 2, 0,
                                                            out StatResult_str1, out StatResult_str2, out StatResult_str3, out StatResult_str4,
                                                             out StatResult_str5, out outputCount, out StatResult_dbl1, out StatResult_dbl2,
                                                             out StatResult_dbl3, out StatResult_dbl4, out StatResult_dbl5);

                                    product.riskMatrix[7] = result == 0 ? StatResult_str1 : "ERROR";

                                    result = ws.GetStatResultEx(handle, 20031, 33, 0, 0, 0, 4, 0, 2, 0,
                                                            out StatResult_str1, out StatResult_str2, out StatResult_str3, out StatResult_str4,
                                                             out StatResult_str5, out outputCount, out StatResult_dbl1, out StatResult_dbl2,
                                                             out StatResult_dbl3, out StatResult_dbl4, out StatResult_dbl5);

                                    product.riskMatrix[8] = result == 0 ? StatResult_str1 : "ERROR";

                                }

                                // Get Characteristics Eval Data
                                for (int iPart = 1; iPart <= partCount; iPart++)
                                {
                                    string partNr = "";
                                    string partDesc = "";
                                    string partOp = "";
                                    result = ws.GetGlobalInfo(handle, iPart, 0, 2, out charCount);
                                    result = ws.GetPartInfo(handle, 1001, iPart, 0, out partNr);
                                    result = ws.GetPartInfo(handle, 1002, iPart, 0, out partDesc);
                                    result = ws.GetPartInfo(handle, 1086, iPart, 0, out partOp);

                                    product.productNr = partNr;
                                    product.description = partDesc;

                                    if (charCount > 0 && evalChar)
                                    {
                                        
                                        for (int iChar = 1; iChar <= charCount; iChar++)
                                        {
                                            String charInfo = "";
                                            double charDbl = 0.0;
                                            int charResult = -1;

                                            String classStr = "";

                                            charResult = ws.GetCharInfo(handle, 2005, iPart, iChar, out charInfo);
                                            classStr = charResult == 0 ? charInfo : "ERROR";

                                            String riskStr = "";
                                            charResult = ws.GetStatResult(handle, 20030, iPart, iChar, 0, out charInfo, out charDbl);
                                            riskStr = charResult == 0 ? charInfo : "ERROR";

                                            if (charClass == null || charClass == classStr)
                                            {
                                                if (riskLevel == null || riskLevel == riskStr)
                                                {
                                                    CharEvalData charData = new CharEvalData();

                                                    charData.partID = product.partList.Split(',')[iPart - 1];
                                                    charData.charID = iChar.ToString();

                                                    charResult = ws.GetCharInfo(handle, 2001, iPart, iChar, out charInfo);

                                                    charData.charNr = charResult == 0 ? charInfo : "ERROR";

                                                    charResult = ws.GetCharInfo(handle, 2002, iPart, iChar, out charInfo);
                                                    charData.CharDesc = charResult == 0 ? charInfo : "ERROR";

                                                    charData.CharClass = classStr;

                                                    charData.OpNo = partOp;

                                                    charResult = ws.GetStatResult(handle, 1000, iPart, iChar, 0, out charInfo, out charDbl);
                                                    charData.xBar = charResult == 0 ? charInfo : "ERROR";

                                                    charResult = ws.GetStatResult(handle, 2100, iPart, iChar, 0, out charInfo, out charDbl);
                                                    charData.stdDev = charResult == 0 ? charInfo : "ERROR";

                                                    charResult = ws.GetStatResult(handle, 5210, iPart, iChar, 0, out charInfo, out charDbl);
                                                    charData.potIndex = charResult == 0 ? charInfo : "ERROR";

                                                    charResult = ws.GetStatResult(handle, 5220, iPart, iChar, 0, out charInfo, out charDbl);
                                                    charData.criticalIndex = charResult == 0 ? charInfo : "ERROR";

                                                    charData.riskLevel = riskStr;

                                                    product.charEvalList.Add(charData);
                                                }
                                            }
                                        }
                                    }

                                }

                            }
                        }
                    }
                    ws.ClientDisconnect(handle);
                }

            }
            catch (Exception ex)
            {
            }

            return resultString;
        }

        // GET: CharCharts
        public ActionResult CharCharts(String partList, String partID, String charID)
        {
            string resultString = "";

            CharEvalData charModel = new CharEvalData();

            Console.WriteLine("Start Getting charts " + DateTime.Now.ToString());


            try
            {


                String lastNValues = "125";


                HttpCookie reqCookies = Request.Cookies["QDAS_Filter"];
                if (reqCookies != null)
                {

                    lastNValues = Request.Cookies["QDAS_Filter"].Value;
                }

                IQdas_Web_Serviceservice ws = new IQdas_Web_Serviceservice();
                int handle = -1;
                int result = -1;

                if (ws != null)
                {
                    result = ws.WebConnect(20, 44, "superuser", "superuser", "", out handle);
                }


                String FieldList = "";

                int queryHandle = -1;
                int SQLHandle = -1;
                int filterHandle = -1;



                // TEBEREICH Plant
                // TEERZEUGNIS Product
                // [TEARBEITSGANG] Operation
                // [TEMASCHINEBEZ] Machine
                // [TEMASCHINENR] Pallet
                // [TETYP] Model
                // [TEBEZEICH] Description
                // [TEWERKSTATT] Component

                if (result != -1)
                {
                    //String partListStr = "<Part key = '" + partID + "' />";

                    String partListStr = "<Part key = '" + partID + "'><Char key='" + charID + "'/></Part>";

                    result = ws.CreateQuery(handle, out queryHandle);

                    result = ws.CreateFilter(handle, 1, 0, lastNValues, 129, out filterHandle);

                    if (result == 0)
                    {
                        result = ws.AddFilterToQuery(handle, queryHandle, filterHandle, 2, 0, 0);

                        if (result == 0)
                        {

                            String partListXML = "<PartCharList>" + partListStr + "</PartCharList>";


                            result = ws.ExecuteQuery_Ext(handle, queryHandle, partListXML, false, true);
                            result = ws.EvaluateAllChars(handle);

                            int partCount = -1;
                            int charCount = -1;
                            result = ws.GetGlobalInfo(handle, 0, 0, 1, out partCount);

                            string StatResult_str1 = "";
                            //Get Cp Value

                            if (partCount  == 1)
                            {

                                result = ws.GetGlobalInfo(handle, 1, 0, 2, out charCount);

                                // Get Characteristics Eval Data

                                string partNr = "";
                                string partDesc = "";
                                string partOp = "";

                                string modelType = "";
                                string productType = "";

                                result = ws.GetGlobalInfo(handle, 1, 0, 2, out charCount);
                                result = ws.GetPartInfo(handle, 1001, 1, 0, out partNr);
                                result = ws.GetPartInfo(handle, 1002, 1, 0, out partDesc);
                                result = ws.GetPartInfo(handle, 1005, 1, 0, out productType);
                                result = ws.GetPartInfo(handle, 1008, 1, 0, out modelType);
                                result = ws.GetPartInfo(handle, 1086, 1, 0, out partOp);


                                if (charCount == 1)
                                {
                                    String charInfo = "";
                                    double charDbl = 0.0;
                                    int charResult = -1;

                                    String classStr = "";
                                    charResult = ws.GetCharInfo(handle, 2005, 1, 1, out charInfo);
                                    classStr = charResult == 0 ? charInfo : "ERROR";

                                    String riskStr = "";
                                    charResult = ws.GetStatResult(handle, 20030, 1, 1, 0, out charInfo, out charDbl);
                                    riskStr = charResult == 0 ? charInfo : "ERROR";

                                    String charNr = "";
                                    charResult = ws.GetCharInfo(handle, 2001, 1, 1, out charInfo);
                                    charNr = charResult == 0 ? charInfo : "ERROR";

                                    String charDesc = "";
                                    charResult = ws.GetCharInfo(handle, 2002, 1, 1, out charInfo);
                                    charDesc = charResult == 0 ? charInfo : "ERROR";


                                    String xBar = "";
                                    charResult = ws.GetStatResult(handle, 1000, 1, 1, 0, out charInfo, out charDbl);
                                    xBar = charResult == 0 ? charInfo : "ERROR";

                                    String stdDev = "";
                                    charResult = ws.GetStatResult(handle, 2100, 1, 1, 0, out charInfo, out charDbl);
                                    stdDev = charResult == 0 ? charInfo : "ERROR";

                                    String potIndex = "";
                                    charResult = ws.GetStatResult(handle, 5210, 1, 1, 0, out charInfo, out charDbl);
                                    potIndex = charResult == 0 ? charInfo : "ERROR";

                                    String criticalIndex = "";
                                    charResult = ws.GetStatResult(handle, 5220, 1, 1, 0, out charInfo, out charDbl);
                                    criticalIndex = charResult == 0 ? charInfo : "ERROR";

                                    String valGraphicStr = "";
                                    charResult = ws.GetGraphic(handle, 3100, 1, 1, 500, 300, out valGraphicStr);
                                    if (charResult == 0)
                                    {
                                        var xmlDocument = new XmlDocument();
                                        xmlDocument.LoadXml(valGraphicStr);

                                        charModel.valueChartImg = xmlDocument.SelectSingleNode("/Test/Image").InnerText;

                                    }

                                    String qccGraphicStr = "";
                                    charResult = ws.GetGraphic(handle, 6110, 1, 1, 500, 300, out qccGraphicStr);
                                    if (charResult == 0)
                                    {
                                        var xmlDocument = new XmlDocument();
                                        xmlDocument.LoadXml(qccGraphicStr);

                                        charModel.qccChartImg = xmlDocument.SelectSingleNode("/Test/Image").InnerText;

                                    }


                                    String histGraphicStr = "";
                                    charResult = ws.GetGraphic(handle, 3300, 1, 1, 250, 300, out histGraphicStr); 
                                    if (charResult == 0)
                                    {
                                        var xmlDocument = new XmlDocument();
                                        xmlDocument.LoadXml(histGraphicStr);

                                        charModel.histChartImg = xmlDocument.SelectSingleNode("/Test/Image").InnerText;
                                    }

                                    charModel.modelType = modelType;
                                    charModel.productNr = partNr;
                                    charModel.productType = productType;
                                    charModel.description = partDesc;
                                    charModel.partList = partList;
                                    charModel.OpNo = partOp;
                                    charModel.partID = partID;
                                    charModel.potIndex = potIndex;
                                    charModel.criticalIndex = criticalIndex;
                                    charModel.xBar = xBar;
                                    charModel.stdDev = stdDev;
                                    charModel.riskLevel = riskStr;
                                    charModel.CharClass = classStr;
                                    charModel.CharDesc = charDesc;
                                    charModel.charNr = charNr;


                                }

                            }
                        }
                    }
                    ws.ClientDisconnect(handle);
                }

            }
            catch (Exception ex)
            {
            }

            return View(charModel);
        }


    }
}