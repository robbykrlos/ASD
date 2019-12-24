using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Net;
using System.Xml;
using ASD;

namespace AutoDownloadSubtitle
{
    public static class OpenSubtitleUtils
    {
        public static string Login(string URL_RPC, string username, string password)
        {
            string content = callWebService(URL_RPC, getLoginRequestData(username, password, "en", "TemporaryUserAgent"));

            List<string> responseList = processXmlResponse(content);
            if (checkLogin(responseList))
            {
                return responseList[10];//token
            }
            else
            {
                return "[ERROR] Login FAILED!" + responseList[16] + "\r\n";
            }
        }

        public static string Logout(string URL_RPC, string token)
        {
            string content = callWebService(URL_RPC, getLogoutRequestData(token));

            List<string> responseList = processXmlResponse(content);
            if (checkLogout(responseList))
            {
                return "Logout Sucessfully!" + "\r\n";
            }
            else
            {
                return "[ERROR] Logout FAILED!" + displayResponse(responseList) + "\r\n";
            }
        }

        private static string callWebService(string URL_RPC, string strRequestData)
        {
            WebRequest myReq = WebRequest.Create(URL_RPC);
            myReq.Method = "POST";

            ASCIIEncoding encoding = new ASCIIEncoding();

            byte[] data = encoding.GetBytes(strRequestData);
            myReq.ContentLength = data.Length;

            Stream newStream = myReq.GetRequestStream();
            newStream.Write(data, 0, data.Length);
            newStream.Close();

            WebResponse wr = myReq.GetResponse();
            Stream receiveStream = wr.GetResponseStream();
            StreamReader reader = new StreamReader(receiveStream, Encoding.UTF8);
            return reader.ReadToEnd();
        }

        private static bool checkLogin(List<string> responseList)
        {
            //displayResponse(responseList);

            return
                responseList[7] == "token" &&
                responseList[13] == "status" &&
                responseList[16] == "200 OK";

        }

        private static bool checkLogout(List<string> responseList)
        {
            //displayResponse(responseList);

            return
                responseList[7] == "status" &&
                responseList[10] == "200 OK";

        }

        public static List<string> processXmlResponse(string content)
        {
            XmlTextReader xmlReader = new XmlTextReader(new StringReader(content));

            List<string> responseList = new List<string>();

            while (xmlReader.Read())
            {
                switch (xmlReader.NodeType)
                {
                    case XmlNodeType.Element:
                        responseList.Add(xmlReader.Name);
                        break;
                    case XmlNodeType.Text:
                        responseList.Add(xmlReader.Value);
                        break;
                    case XmlNodeType.EndElement:
                        break;
                }
            }
            return responseList;
        }

        public static string SearchSubtitle4Movie(string URL_RPC, string fileName, string strToken)
        {
            byte[] moviehash = MovieHash.ComputeMovieHash(fileName);
            string strMovieHash = MovieHash.ToHexadecimal(moviehash);

            FileInfo fileInfo = new FileInfo(fileName);
            string strSearchRequestData = getSearchRequestData(strToken, strMovieHash, fileInfo.Length.ToString());

            //Console.WriteLine(strMovieHash);
            //Console.WriteLine(fileInfo.Length.ToString());

            return callWebService(URL_RPC, strSearchRequestData);
        }

        private static string getLoginRequestData(string username, string password, string lang, string useragent)
        {
            return "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                                    "<methodCall>" +
                                    "<methodName>LogIn</methodName>" +
                                    "<params>" +
                                    "<param>" +
                                    "<value><string>" + username + "</string></value>" +
                                    "</param>" +
                                    "<param>" +
                                    "<value><string>" + password + "</string></value>" +
                                    "</param>" +
                                    "<param>" +
                                    "<value><string>" + lang + "</string></value>" +
                                    "</param>" +
                                    "<param>" +
                                    "<value><string>" + useragent + "</string></value>" +
                                    "</param>" +
                                    "</params>" +
                                    "</methodCall>";
        }

        private static string getLogoutRequestData(string token)
        {
            return "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
            "<methodCall>" +
                "<methodName>LogOut</methodName>" +
                "<params>" +
                    "<param>" +
                        "<value><string>" + token + "</string></value>" +
                    "</param>" +
                "</params>" +
            "</methodCall>";
        }

        private static string getSearchRequestData(string strToken, string strMovieHash, string strMovieSize)
        {
            return @"<?xml version=""1.0""?>
                            <methodCall>
                            <methodName>SearchSubtitles</methodName>
                            <params><param>
                               <value><string>" + strToken + @"</string></value>
                              </param>
                              <param>
                               <value>
                                <array>
                                 <data>
                                  <value>
                                   <struct>
                                    <member>
                                     <name>sublanguageid</name>
                                     <value><string>rum,eng</string>
                                     </value>
                                    </member>
                                    <member>
                                     <name>moviehash</name>
                                     <value><string>" + strMovieHash + @"</string></value>
                                    </member>
                                    <member>
                                     <name>moviebytesize</name>
                                     <value><double>" + strMovieSize + @"</double></value>
                                    </member>
                                   </struct>
                                  </value>
                                 </data>
                                </array>
                               </value>
                              </param></params>
                            </methodCall>";
        }

        private static string displayResponse(List<string> responseList)
        {
            string responseString = "";
            foreach (string str in responseList)
            {
                responseString += str + " -> ";
            }
            return responseString;
        }
}
}
