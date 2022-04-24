//-------------------------------------------------------------------------------------------------
// Klasse zum Downloaden einer Internetseite
// Oliver Abraham, 4/2010
// Siehe auch: http://www.codeproject.com/KB/cs/SeansDownloader.aspx
//-------------------------------------------------------------------------------------------------


using System.IO;
using System.Net;
using System.Text;

namespace Abraham.Internet
{
	public class HttpClient
    {
        public string DownloadFromUrl(string url)
        {
            HttpWebRequest httpRequest = (HttpWebRequest)WebRequest.Create(url);
            httpRequest.CookieContainer = new CookieContainer();
            httpRequest.AllowAutoRedirect = true;
            string Seiteninhalt = "";

            HttpWebResponse httpResponse = (HttpWebResponse)httpRequest.GetResponse();
            if (httpResponse.StatusCode == HttpStatusCode.OK)
            {
                byte[] httpHeaderData = httpResponse.Headers.ToByteArray();
                Stream httpContentData = httpResponse.GetResponseStream();
                using (httpContentData)
                {
                    Encoding enc = Encoding.UTF8;
                    int AnzahlGelesen;
                    byte[] seiteninhalt = new byte[10000];
                    do
                    {
                        AnzahlGelesen = httpContentData.Read(seiteninhalt, 0, seiteninhalt.Length);
                        Seiteninhalt += enc.GetString(seiteninhalt, 0, AnzahlGelesen);
                    }
                    while (AnzahlGelesen > 0);
                }
            }
            httpResponse.Close();
            return Seiteninhalt;
        }
    }
}
