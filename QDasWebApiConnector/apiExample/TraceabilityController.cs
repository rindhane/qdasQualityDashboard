using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CumminsQualityInspection.Models;

namespace CumminsQualityInspection.Controllers
{
    public class TraceabilityController : Controller
    {
        // GET: Traceability
        public ActionResult Index(string partSerialNo)
        {
            List<ComponentData> componentList = new List<ComponentData>();

            if (partSerialNo == null || partSerialNo.Trim() == "")
            {

                return View(componentList);

            }

            ComponentData model = new ComponentData();
            model.PartSerialNo = partSerialNo;
            model.operationList = new List<OperationData>();


            var connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["QDAS_SQL_Connection"].ToString();

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    //access SQL Server and run your command
                    string query = "SELECT MERKMAL.MENENNMAS, TEIL.TETEILNR, TEIL.TEBEZEICH, TEIL.TEARBEITSGANG, WERTEVAR.WVUNTERS, WERTEVAR.WVMERKMAL, WERTEVAR.WVWERTNR, " +
                                   "WERTEVAR.WVWERT, WERTEVAR.WVDATZEIT, WERTEVAR.WVATTRIBUT, WERTEVAR.WVPRUEFER, WERTEVAR.WVMASCHINE, WERTEVAR.WV0055, " +
                                   "MERKMAL.MEMERKBEZ, MERKMAL.MEUGW, MERKMAL.MEOGW, MERKMAL.MEMERKKLASSE, ALARM_VALUES.ALARM_EW, WERTEVAR.WVTEIL " +
                                    "FROM((([WERTEVAR] left join TEIL on[WERTEVAR].WVTEIL = TEIL.TETEIL) " +
                                    "left join MERKMAL on [WERTEVAR].WVMERKMAL = MERKMAL.MEMERKMAL and WERTEVAR.WVTEIL = MERKMAL.METEIL) " +
                                    "left join ALARM_VALUES on [WERTEVAR].WVWERTNR = ALARM_VALUES.K0000 and WERTEVAR.WVTEIL = ALARM_VALUES.K1000  and WERTEVAR.WVMERKMAL = ALARM_VALUES.K2000) " +
                                    "where WV0055 = '" + partSerialNo + "' order by WERTEVAR.WVWERTNR asc";

                    SqlCommand cmd = new SqlCommand(query, conn);

                    //open connection
                    conn.Open();
                    //execute the SQLCommand
                    SqlDataReader dr = cmd.ExecuteReader();

                    Dictionary<string, List<CharacteristicsData>> dictOperationList = new Dictionary<string, List<CharacteristicsData>>();

                    //check if there are records
                    if (dr.HasRows)
                    {

                        while (dr.Read())
                        {
                            model.PartID = dr.GetValue(1).ToString().Trim();
                            model.PartDescription = dr.GetValue(2).ToString().Trim();

                            CharacteristicsData charData = new CharacteristicsData();
                            charData.CharID = dr.GetValue(5).ToString().Trim();
                            charData.CharDesc = dr.GetValue(13).ToString().Trim();
                            charData.CharOp = dr.GetValue(3).ToString().Trim();
                            charData.CharClass = dr.IsDBNull(16) ? 2 : Convert.ToInt32(dr.GetValue(16));
                            charData.CharAlarm = dr.IsDBNull(17) ? 0 : (int)dr.GetValue(17);
                            if (charData.CharAlarm >= 1 && charData.CharAlarm <= 8)
                            {
                                charData.status = (int)Status.STATUS_DANGER;
                            }
                            else if (charData.CharAlarm >= 16 && charData.CharAlarm <= 128)
                            {
                                charData.status = (int)Status.STATUS_WARNING;
                            }

                            charData.CharNom = dr.GetValue(0).ToString().Trim();
                            charData.CharMin = dr.GetValue(14).ToString().Trim();
                            charData.CharMax = dr.GetValue(15).ToString().Trim();
                            charData.CharVal = dr.GetValue(7).ToString().Trim();
                            charData.CharDate = dr.GetValue(8).ToString().Trim().Split(' ')[0];
                            charData.CharTime = dr.GetValue(8).ToString().Trim().Split(' ')[1];

                            if (dictOperationList.ContainsKey(charData.CharOp))
                            {
                                dictOperationList[charData.CharOp].Add(charData);
                            }
                            else
                            {
                                List<CharacteristicsData> charListForOp = new List<CharacteristicsData>();
                                charListForOp.Add(charData);
                                dictOperationList[charData.CharOp] = charListForOp;
                            }
                        }

                        foreach (var item in dictOperationList)
                        {
                            OperationData operation = new OperationData();
                            operation.OpDesc = item.Key.ToString();
                            operation.characteristicsList = (List<CharacteristicsData>)item.Value;

                            foreach(CharacteristicsData charData in operation.characteristicsList)
                            {
                                if (charData.status == (int)Status.STATUS_DANGER)
                                {
                                    operation.status = (int)Status.STATUS_DANGER;
                                }
                                else if (charData.status == (int)Status.STATUS_WARNING)
                                {
                                    if (operation.status != (int)Status.STATUS_DANGER)
                                    {
                                        operation.status = (int)Status.STATUS_WARNING;
                                    }
                                }
                            }

                            if (operation.status == (int)Status.STATUS_DANGER)
                            {
                                model.status = (int)Status.STATUS_DANGER;
                            }
                            else if (operation.status == (int)Status.STATUS_WARNING)
                            {
                                if (model.status != (int)Status.STATUS_DANGER)
                                {
                                    model.status = (int)Status.STATUS_WARNING;
                                }
                            }
                            model.operationList.Add(operation);
                        }
                    }
                    else
                    {
                        model.remarks = "No Inspection data found for " + partSerialNo;
                    }
                    dr.Close();
                }

                componentList.Add(model);
            }
            catch (Exception ex)
            {
                //display error message
                model.remarks = ex.Message;
            }

            return View(componentList);
        }
    }
}