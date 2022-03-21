using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using MySql.Data.MySqlClient;
using System.Threading;
using System.Text.RegularExpressions;
using System.Diagnostics;
using Nager.PublicSuffix;

namespace AutoCrawling_SEM
{
    class Program
    {
        static void Main(string[] args)
        {


            Exams.RunApplecationBase();
        }

    }

    public class Exams
    {
        protected static ChromeDriverService _driverService = null;
        protected static ChromeOptions _options = null;
        protected static ChromeDriver _driver = null;

        static string myConnectionString = "server=mysqldb.modvisor.com;database=user_modvisor;uid=modvisor;pwd=Qwer1234!!;";


        static List<similarstores> SetStoreData = new List<similarstores>();
        static List<stores> DBStoreData = new List<stores>();
        public static void RunApplecationBase()
        {
            try
            {
                _driverService = ChromeDriverService.CreateDefaultService();
                _driverService.HideCommandPromptWindow = true;

                _options = new ChromeOptions();
                _options.AddArgument("disable-gpu");
            }
            catch (Exception exc)
            {
                Trace.WriteLine(exc.Message);
                AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
            }
            try
            {

                //login form
                _driver = new ChromeDriver(_driverService, _options);

                _driver.Navigate().GoToUrl("https://www.semrush.com/login/");

                _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);

                //  var element = _driver.FindElement(By.XPath("//*[@id='loginForm']/div[1]/div[2]/section[1]/div[2]/button"));
                //  var element = _driver.FindElement(By.CssSelector(".table-container > .tableContainer[style*='none']"));

                //element.Click();
                var loginid = _driver.FindElement(By.XPath("//*[@id='email']"));
                var loginpwd = _driver.FindElement(By.XPath("//*[@id='password']"));
                var loginenter = _driver.FindElement(By.XPath("//*[@data-test='login-page__btn-login']"));
                //_driver.FindElement(By.CssSelector(".AuthForm__button--161NN ___SButton_rkzvx_gg_"));

                loginid.SendKeys("connectshopsco@gmail.com");
                loginpwd.SendKeys("AppleTree1659@");

                loginenter.Click();
                Thread.Sleep(3000);

                //after login

                GetStoreListFromDB();
                GetDataFromWEBSave();



            }
            catch (Exception exc)
            {
                Trace.WriteLine(exc.Message);
            }

            Thread.Sleep(5000);
            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;


        }

        public static void GetStoreListFromDB()
        {
            MySqlConnection cnn = new MySqlConnection(myConnectionString);
            try
            {
                string sql_query = "Select website,id From stores ";
                sql_query += "where id =22";// not in (SELECT store_id FROM user_modvisor.similarstores_history WHERE store_id =13)";// >389 and  lastupdate >= (NOW() - INTERVAL 3 MONTH)) limit 1";

                using (var bag = new MySqlConnection(myConnectionString))
                using (var cmd = new MySqlCommand(sql_query, bag))
                {
                    bag.Open();
                    using (MySqlDataReader oku = cmd.ExecuteReader())
                    {
                        while (oku.Read())
                        {
                            DBStoreData.Add(new stores() { storeid = Convert.ToString(oku[1]), storewebsite = Convert.ToString(oku[0]) });
                        }
                    }

                    bag.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Cannot open connection!");
            }
        }

        private static void GetDataFromWEBSave()
        {
            try
            {
                foreach (var aPart in DBStoreData)
                {

                    string websiteaddr = Convert.ToString(aPart.storewebsite);
                    string storeid = Convert.ToString(aPart.storeid);
                    string url_link = "https://www.semrush.com/analytics/organic/competitors/?sortField=&sortDirection=desc&db=us&q="+ websiteaddr + "&searchType=domain";

                    _driver.Navigate().GoToUrl(url_link);
                    _driver.Navigate().Refresh();
                    Thread.Sleep(3000);


                    IList<IWebElement> hiddenElements = _driver.FindElements(By.XPath("//*[@data-ui-name='DefinitionTable.Body']"));
                    //_driver.FindElements(By.CssSelector(".table-container > .tableContainer[style*='none']"));
                    


                    if (hiddenElements.Count > 0)
                    {
                        var table = _driver.FindElement(By.XPath("//*[@data-ui-name='DefinitionTable.Body']"));
                        var trs = table.FindElements(By.XPath("//*[@class='___SRow_1ji8l-red-team']")); 

                        //get store list data
                        foreach (var item in trs)
                        {
                             
                            var WorskpaceLink = item.FindElement(By.CssSelector(".cl-display-domain__link >  a >  span > div > div"));
                            var score = item.FindElement(By.CssSelector(".cl-display-competition-level__info")); 
                           // var average_value = item.FindElement(By.Name("traffic")).FindElement(By.CssSelector(".___SBoxInline_1tphm-red-team > span > span"));
                            string website = StripTagsRegex(Convert.ToString(WorskpaceLink.GetAttribute("innerHTML")).Replace("\"", "'"));
                            string webscore = Convert.ToString(score.GetAttribute("innerHTML")).Replace("%", "");
                            string webrank = "";// Convert.ToString(score.GetAttribute("innerHTML"));
                            



                               SetStoreData.Add(new similarstores() { storeid = storeid, storewebsite = website, score = webscore, rank_num = webrank });
                        }

                        if (SetStoreData.Count > 0)
                        {
                            //set store list data to db
                            SeTStoreListToDB(SetStoreData, storeid);
                            SetStoreData.Clear();
                        }

                        _driver.Navigate().GoToUrl("https://www.semrush.com/projects/");
                        Thread.Sleep(5000);
                    }
                }
            }
            catch (Exception exc)
            {
                Trace.WriteLine(exc.Message);
            }
        }

        public static void SeTStoreListToDB(List<similarstores> setStoreData, string store_id)
        {
            try
            {

                using (var bag = new MySqlConnection(myConnectionString))
                {
                    bag.Open(); 

                    //delete first
                    var cmd1_1 = new MySqlCommand("delete from similarities where store_id='" + store_id + "'", bag);
                    MySqlDataReader MyReader_1;
                    MyReader_1 = cmd1_1.ExecuteReader();
                    MyReader_1.Close();

                    //insert

                    foreach (var aPart in setStoreData)
                    {
                        string websiteaddr = Convert.ToString(aPart.storewebsite);
                        string webscore = Convert.ToString(aPart.score);
                        string webrank = Convert.ToString(aPart.rank_num);
                        int int_rank = 0;
                        webscore = webscore.Replace("%", "");//.Replace("$", "").Replace(".", "").Replace("-", "");
                        //if (webrank.IndexOf(".") > 0)
                        //{
                        //    webrank  = webrank.Replace("K", "00");
                        //}
                        //else
                        //{
                        //    webrank.Replace("K", "000");
                        //}
                        //webrank = webrank.Replace(".", "");
                        //if (webrank == "") { int_rank = 0; }
                        //else
                        //{
                        //    int_rank = Convert.ToInt32(webrank);
                        //}
                        int_rank = 0;

                        string temp_simi_id = GetStoreListFromDB_BaseOnUrl(websiteaddr);
                        if (temp_simi_id != "")
                        {

                            string Query = "insert into similarities(store_id,similar_store_id,score,calculated_score,voting_score,`rank`,created_at,updated_at,created_by_id,updated_by_id) ";
                            Query += "values('" + store_id + "','" + temp_simi_id + "','" + webscore + "','0','0','" + int_rank + "',CURRENT_TIMESTAMP(),CURRENT_TIMESTAMP(),'4',null);";

                            var MyCommand = new MySqlCommand(Query, bag);
                            MySqlDataReader MyReader2;
                            MyReader2 = MyCommand.ExecuteReader();
                            MyReader2.Close();

                        }
                        else
                        {
                            var domainParser = new DomainParser(new WebTldRuleProvider());
                            var domainInfo = domainParser.Parse(websiteaddr);

                            string tempwebbsite = websiteaddr;
                            try
                            {
                                tempwebbsite = domainInfo.Domain.ToString();
                            }
                            catch { }

                            string slug = "";
                            slug = tempwebbsite.Replace(" ", "-");
                            slug = slug.Replace(" ", "-");
                            slug = slug.Replace(".com", "");

                            slug = Regex.Replace(slug, @"/[^A-Za-z0-9\-]/", "");
                            slug = Regex.Replace(slug, @"/-+/", "-");
                            slug = slug.ToLower();

                            string Query2_3 = "insert into stores(name,website,slug,status,created_by,updated_by,review_score,monthly_visitor,review_number,price_range_id,monthly_rank,created_at,updated_at,affiliate_switch,is_affiliated,`type`)";
                            Query2_3 += "values('" + slug + "','" + websiteaddr + "','" + slug + "','inactive','332','332',0,0,'0',1,0,CURRENT_TIMESTAMP(),CURRENT_TIMESTAMP(),0,0,'Boutique');";
                            // no store add

                            var MyCommand2_3 = new MySqlCommand(Query2_3, bag);
                            MySqlDataReader MyReader2_3;
                            MyReader2_3 = MyCommand2_3.ExecuteReader();
                            MyReader2_3.Close();


                        }
                    }
                    //delete first
                    var cmd_d = new MySqlCommand("delete from similarstores_history where store_id='" + store_id + "'", bag);
                    MySqlDataReader MyReader4;
                    MyReader4 = cmd_d.ExecuteReader();
                    MyReader4.Close();


                    string Query3 = "insert into similarstores_history(store_id,lastupdate) ";
                    Query3 += "values('" + store_id + "',CURRENT_TIMESTAMP());";
                    var cmd = new MySqlCommand(Query3, bag);
                    MySqlDataReader MyReader3;
                    MyReader3 = cmd.ExecuteReader();
                    MyReader3.Close();


                    Thread.Sleep(4000);


                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Cannot open connection!" + ex.Message);
            }

        }
        public static string GetStoreListFromDB_BaseOnUrl(string url)
        {
            string return_storeid = "";
            MySqlConnection cnn = new MySqlConnection(myConnectionString);
            try
            {
                string sql_query = "Select  id as store_id From stores where website ='" + url + "' limit 1";

                using (var bag = new MySqlConnection(myConnectionString))
                using (var cmd = new MySqlCommand(sql_query, bag))
                {
                    bag.Open();
                    using (MySqlDataReader oku = cmd.ExecuteReader())
                    {
                        while (oku.Read())
                        {
                            return_storeid = Convert.ToString(oku[0]);
                        }
                    }

                    bag.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Cannot open connection!");
            }
            return return_storeid;
        }
        public static string StripTagsRegex(string source)
        {
            return Regex.Replace(source, "<.*?>", string.Empty);
        }
        public static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            try
            {
                _driver.Quit();
                Environment.Exit(0);

            }
            catch (Exception exc)
            {
                Trace.WriteLine(exc.Message);
            }
        }
    }

    public class stores
    {
        public string storeid { get; set; }

        public string storewebsite { get; set; }

    }
    public class similarstores
    {
        public string storeid { get; set; }

        public string storewebsite { get; set; }
        public string score { get; set; }

        public string rank_num { get; set; }


    }
}
