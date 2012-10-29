YFinance_WebRequest Application
---------------

This application automatically logs into the Yahoo account and requests a comma-separated file. The file is then parsed and the information from the file is stored int the database. The key points of the application are

**Authentication using HttpWebRequest**

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

**Using the cookies to request the file**

    HttpWebRequest req = (HttpWebRequest)WebRequest.Create(_downloadUrl);
    req.CookieContainer = _yahooContainer;
    HttpWebResponse resp = (HttpWebResponse)req.GetResponse();

    using(StreamReader streamReader = new StreamReader(resp.GetResponseStream()))
    {
    	string t = streamReader.ReadToEnd();
    }

**See also my [blog entry] on the subject**

  [blog entry]: http://justmycode.blogspot.com.au/2012/10/automating-website-authentication.html