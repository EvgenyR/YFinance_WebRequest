using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.IO;
using System.Data.Objects;
using System.Globalization;
using System.Net;

namespace Finance
{
    public class FinanceBrowser
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType); //logging
        public delegate void EventHandler(object sender, EventArgs args);
        public event EventHandler DataDownloaded = delegate { };
        public event EventHandler Authenticated = delegate { };

        private string _login;

        public string Login
        {
            set { _login = value; }
        }
        private string _password;

        public string Password
        {
             set { _password = value; }
        }

        private const string LoginUrl = "https://login.yahoo.com/config/login";
        private const string MyYahoo = "my.yahoo.com";
        private readonly Uri _downloadUrl = null;
        private readonly FinanceEntities _db;
        private readonly Timer _timer;
        private CookieContainer _yahooContainer;

        public FinanceBrowser(FinanceEntities DB)
        {
            _db = DB;
            _downloadUrl = GetDownloadUrl();

            _timer = new Timer {Interval = 5000};
            _timer.Tick += new System.EventHandler(timer_Tick);
        }

        void timer_Tick(object sender, EventArgs e)
        {
            DownloadData();
        }

        public void Authenticate()
        {
            string strPostData = String.Format("login={0}&passwd={1}", _login, _password);

            // Setup the http request.
            HttpWebRequest wrWebRequest = WebRequest.Create(LoginUrl) as HttpWebRequest;
            wrWebRequest.Method = "POST";
            wrWebRequest.ContentLength = strPostData.Length;
            wrWebRequest.ContentType = "application/x-www-form-urlencoded";
            _yahooContainer = new CookieContainer();
            wrWebRequest.CookieContainer = _yahooContainer;

            // Post to the login form.
            using (StreamWriter swRequestWriter = new StreamWriter(wrWebRequest.GetRequestStream()))
            {
                swRequestWriter.Write(strPostData);
                swRequestWriter.Close();           
            }

            // Get the response.
            HttpWebResponse hwrWebResponse = (HttpWebResponse)wrWebRequest.GetResponse();

            if (hwrWebResponse.ResponseUri.AbsoluteUri.Contains(MyYahoo))
            {
                Authenticated(this, new EventArgs());
            }
        }

        public void DownloadData()
        {
            _timer.Stop();

            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(_downloadUrl);
            req.CookieContainer = _yahooContainer;
            HttpWebResponse resp = (HttpWebResponse)req.GetResponse();

            using(StreamReader streamReader = new StreamReader(resp.GetResponseStream()))
            {
                string t = streamReader.ReadToEnd();
                string[] strings = t.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
                InsertData(strings);
            }
        }

        private int GetSymbol(string name)
        {
            var symbol = new Symbol();
            try
            {
                symbol = _db.Symbols.Where(s => s.Name == name).FirstOrDefault();
            }
            catch (Exception ex)
            {
                log.Fatal("Could not find Symbol " + name, ex);
            }
            return symbol.Id;
        }

        private Uri GetDownloadUrl()
        {
            // example url: "http://download.finance.yahoo.com/d/quotes.csv?s=ACC.NS+ICICIBANK.NS+ENGINERSI.NS&f=snd1l1t1vb3b2hg"
            string url = "http://download.finance.yahoo.com/d/quotes.csv?s=";

            try
            {
                var urlDb = new FinanceEntities();
                ObjectSet<Symbol> symbols = urlDb.Symbols;
                int total = symbols.Count();
                int count = 0;
                foreach (Symbol symbol in symbols)
                {
                    url = url + symbol.Name;
                    if (count < total - 1)
                    {
                        url = url + "+";
                    }
                    count++;
                }
                url = url + "&f=snd1l1t1vb3b2hg";
            }
            catch(Exception ex)
            {
                log.Fatal("Error retrieving download url: ", ex);
            }
            return new Uri(url);
        }

        private void InsertData(IEnumerable<string> lines)
        {
            try
            {
                foreach (string line in lines)
                {
                    if (!String.IsNullOrEmpty(line))
                    {
                        Datum datum = GetDatum(line);
                        _db.Data.AddObject(datum);
                        _db.SaveChanges();
                    }
                }
                _db.Refresh(RefreshMode.StoreWins, _db.Data);
                DataDownloaded(this, new EventArgs());
            }
            catch (Exception ex)
            {
                log.Fatal("Exception in InsertData: ", ex);
            }

            _timer.Start();
        }

        private Datum GetDatum(string line)
        {
            var datum = new Datum();
            try
            {
                string[] splitLine = line.Split(',');
                datum = new Datum
                {
                    SymbolId = GetSymbol(splitLine[0].Replace("\"", "")),
                    Name = splitLine[1].Replace("\"", ""),
                    Date =
                        DateTime.ParseExact(splitLine[2].Replace("\"", ""), "MM/dd/yyyy",
                                            CultureInfo.InvariantCulture),
                    LTP = decimal.Parse(splitLine[3]),
                    Time = DateTime.Parse(splitLine[4].Replace("\"", "")),
                    Volume = decimal.Parse(splitLine[5]),
                    Ask = decimal.Parse(splitLine[6]),
                    Bid = decimal.Parse(splitLine[7]),
                    High = decimal.Parse(splitLine[8]),
                    Low = decimal.Parse(splitLine[9])
                };
            }
            catch (Exception ex)
            {
                log.Fatal("Exception in GetDatum: ", ex);
            }
            return datum;
        }

        internal void StopDownloading()
        {
            _timer.Stop();
        }
    }
}