using Android.App;
using Android.OS;
using Android.Support.V7.App;
using Android.Runtime;
using Android.Widget;
using Android.Support.Design.Widget;
using System.Net;
using System.IO;
using System.Text;
using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System.Xml;
using System.Reflection;
using SQLite;
using Android.Views.InputMethods;
using System.Linq;

namespace Dividend
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        bool kor = true;
        XmlNodeList xmlList = null;
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_main);
            //string json = this.Request_Json();
            //this.ParseJson(json);

            //최초 화면 로드시 xml파일 로드
            Assembly assembly = Assembly.GetExecutingAssembly();
            Stream xml_stream = assembly.GetManifestResourceStream("TEST.Resources.xml.CORPCODE.xml");
            StreamReader xml_streamReader = new StreamReader(xml_stream);
            XmlDocument xml = new XmlDocument();
            xml.Load(xml_streamReader);
            xmlList = xml.SelectNodes("/result/list");


            ListView listView_Stock = FindViewById<ListView>(Resource.Id.listView_Stock);

            string dbPath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), "database.db3");
            var db = new SQLiteConnection(dbPath);
            db.CreateTable<Stock>();
            //데이터가 있는 경우만 조회
            if (db.Table<Stock>().Count() > 0)
            {
                ArrayAdapter<string> ListAdapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleListItem1, (from i in db.Table<Stock>() select "종목명 : " + i.Symbol + " 주식수 : " + i.cnt + " 배당금 : " + i.cost).ToList());
                listView_Stock.SetAdapter(ListAdapter);
            }

            Button button_add = FindViewById<Button>(Resource.Id.button_add);
            button_add.Click += (sender, e) => { Add_Stock(); };

            Button button_modify = FindViewById<Button>(Resource.Id.button_modify);
            button_modify.Click += (sender, e) => { Modify_Stock(); };

            Button button_delete = FindViewById<Button>(Resource.Id.button_delete);
            button_delete.Click += (sender, e) => { Delete_Stock(); };

            //Button lastest_dividens = FindViewById<Button>(Resource.Id.button_lastest_dividends);
            //lastest_dividens.Click += (sender, e) => { Search_Dividends("3m"); };

            //Button next_dividens = FindViewById<Button>(Resource.Id.button_next_dividends);
            //next_dividens.Click += (sender, e) => { Search_Dividends("next"); };

            EditText editText = FindViewById<EditText>(Resource.Id.EditText_Stock_Name);

            editText.EditorAction += (sender, e) =>
            {
                if (e.ActionId == ImeAction.Done)
                {
                    Search_Dividends("3m");
                }
                else
                {
                    e.Handled = false;
                }
            };
        }
        //public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        //{
        //    Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

        //    base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        //}

        public void Add_Stock()
        {
            string dbPath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), "database.db3");
            var db = new SQLiteConnection(dbPath);

            EditText editText_stock_name = FindViewById<EditText>(Resource.Id.EditText_Stock_Name);
            EditText editText_stock_cnt = FindViewById<EditText>(Resource.Id.EditText_Stock_Cnt);
            TextView amount = FindViewById<TextView>(Resource.Id.textView_amount);
            ListView listView_Stock = FindViewById<ListView>(Resource.Id.listView_Stock);

            Search_Dividends("3m");
            if (editText_stock_name.Text != null && editText_stock_cnt.Text != null)
            {
                var newStock = new Stock();
                newStock.Symbol = editText_stock_name.Text;
                newStock.cnt = Convert.ToInt32(editText_stock_cnt.Text);
                newStock.cost = Convert.ToInt32(amount.Text.Replace(",", "")) * newStock.cnt;
                db.Insert(newStock);
            }

            else
            { }

            if (db.Table<Stock>().Count() > 0)
            {

                ArrayAdapter<string> ListAdapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleListItem1, (from i in db.Table<Stock>() select "종목명 : " + i.Symbol + " 주식수 : " + i.cnt + " 배당금 : " + i.cost).ToList());
                listView_Stock.SetAdapter(ListAdapter);
            }

        }

        public void Modify_Stock()
        {

        }

        public void Delete_Stock()
        {

        }
        public void Search_Dividends(string period)
        {
            //Parallel.Invoke(
            //                 () => { },
            //                 () => { }
            //               );
            EditText editText = FindViewById<EditText>(Resource.Id.EditText_Stock_Name);
            string corp_name = editText.Text;
            string json = this.Make_url(corp_name, period);
            this.ParseJson(json, period);
        }

        private string Make_url(string corp_name, string period)
        {
            string url = null;
            string corp_code = null;   //회사고유코드
            //string period = null;      //배당 구분

            foreach (XmlNode xnl in xmlList)
            {
                //입력한 종목 값이 있으면(한국주식이면) 해당 고유코드를 가져와서 조회함.
                if (xnl["corp_name"].InnerText == corp_name)
                {
                    corp_code = xnl["corp_code"].InnerText;
                }
            }
            //한국주식이면
            if (corp_code != null)
            {
                kor = true;
                url = "https://opendart.fss.or.kr/api/alotMatter.json?crtfc_key=04fa6fa1e3c214b8942a875b87d67950ad6dd32e&corp_code=" + corp_code + "&bsns_year=2020&reprt_code=11013";
            }
            //한국주식이 아니면 미국주식 종목으로 검색
            else
            {
                kor = false;
                url = "https://cloud.iexapis.com/stable/stock/" + corp_name + "/dividends/" + period + "?token=pk_33adfd1f5e8044e5b6286a30891ad9f7";
            }

            return Request_Json(url);

        }
        private string Request_Json(string url)
        {
            string result = null;
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.UserAgent = "";
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Stream stream = response.GetResponseStream();
                StreamReader reader = new StreamReader(stream, Encoding.UTF8);
                //URF8인코딩을 한 상태로 Json을 읽어온다.
                result = reader.ReadToEnd();
                stream.Close();
                response.Close();
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.Message); // 혹시나 에러가 발생하면 메시지를 보세요~ 
            }
            return result; //Json형태의 문자열을 리턴
        }
        private void ParseJson(string json, string period)
        //제이슨 문자열을 매개변수로 가져와, 원하는 정보만 가져온다.
        {
            TextView amount = FindViewById<TextView>(Resource.Id.textView_amount);
            TextView exdate = FindViewById<TextView>(Resource.Id.textView_exdate);
            TextView paymentdate = FindViewById<TextView>(Resource.Id.textView_paymentdate);

            List<List> InfoLists = new List<List>();
            List InfoList = new List();

            if (kor)
            {
                try
                {
                    JObject data = JObject.Parse(json);
                    var a = data.SelectToken("list");

                    foreach (var itemObj in a)
                    {
                        if (itemObj.SelectToken("se").ToString() == "주당 현금배당금(원)") //&& itemObj.SelectToken("stock_knd").ToString() == "보통주")
                        {
                            InfoList.THSTRM = itemObj.SelectToken("thstrm").ToString();
                            InfoList.FRMTRM = itemObj.SelectToken("frmtrm").ToString();
                            break;
                        }
                    }

                    amount.Text = InfoList.FRMTRM;
                    //if (InfoList.THSTRM == "-")
                    //{
                    //   // amount.Text = "배당금 : " + InfoList.FRMTRM + "원";
                    //    amount.Text = InfoList.FRMTRM;
                    //}
                    //else
                    //    // amount.Text = "분기 배당금 : " + InfoList.THSTRM + "원" + "\n" + "1년 총 배당금: " + InfoList.FRMTRM + "원";
                    //    amount.Text = InfoList.FRMTRM;
                }

                catch (Exception e)
                {
                    amount.Text = null;
                }
                //exdate.Text = InfoList
            }
            else
            {
                //3개월 전의 배당 조회
                if (period == "3m")
                {
                    try
                    {
                        JArray jsonArray = JArray.Parse(json);
                        dynamic data = JObject.Parse(jsonArray[0].ToString());


                        foreach (JObject itemObj in jsonArray)
                        //linq를 사용하면 쉽게 역정렬을 할 수 있다.
                        {
                            InfoList.EXDATE = itemObj["exDate"].ToString();
                            InfoList.PAYMENTDATE = itemObj["paymentDate"].ToString();
                            InfoList.AMOUNT = itemObj["amount"].ToString();

                            amount.Text = "배당금 : $" + InfoList.AMOUNT;
                            exdate.Text = "배당락일 : " + InfoList.EXDATE;
                            paymentdate.Text = "배당지급일 : " + InfoList.PAYMENTDATE;
                        }
                    }
                    catch (Exception e)
                    {
                        amount.Text = null;
                    }
                }
                //다음 배당 조회 시
                else
                {
                    try
                    {
                        JObject data = JObject.Parse(json);
                        InfoList.EXDATE = data.SelectToken("exDate").ToString();
                        InfoList.PAYMENTDATE = data.SelectToken("paymentDate").ToString();
                        InfoList.AMOUNT = data.SelectToken("amount").ToString();

                        amount.Text = "배당금 : $" + InfoList.AMOUNT;
                        exdate.Text = "배당락일 : " + InfoList.EXDATE;
                        paymentdate.Text = "배당지급일 : " + InfoList.PAYMENTDATE;
                    }
                    catch (Exception e)
                    {
                        amount.Text = null;
                    }
                }
                //true로 초기화 해줌 그래야 다시 조건 비교함
                kor = true;
            }

            if (string.IsNullOrEmpty(amount.Text))
            {
                kor = false;
                amount.Text = "조회안됨";
                exdate.Text = "배당락일";
                paymentdate.Text = "배당지급일";
            }
        }
    }
    public class List
    {
        //배당락일
        public string EXDATE { get; set; }
        //배당지급일
        public string PAYMENTDATE { get; set; }
        //레코드데이트
        public string RECORDDATE { get; set; }
        //결정된 날
        public string DECLAREDDATE { get; set; }
        //배당금
        public string AMOUNT { get; set; }
        //분기
        public string FREQUENCY { get; set; }
        //분기별 배당금액
        public string THSTRM { get; set; }
        //1년간 배당금액
        public string FRMTRM { get; set; }
    }

    [Table("Stock_List")]
    public class Stock
    {
        //종목명
        public string Symbol { get; set; }
        //수량
        public int cnt { get; set; }
        //배당금
        public decimal cost { get; set; }
    }
}