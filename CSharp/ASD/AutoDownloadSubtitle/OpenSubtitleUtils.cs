using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Net;
using System.Xml;

namespace AutoSubtitleDownloader
{
    public static class OpenSubtitleUtils
    {
        private const int INDEX_RESPONSE_TOKEN_VALUE = 10;
        private const int INDEX_RESPONSE_LOGIN_STATUS_VALUE = 16;
        private const int INDEX_RESPONSE_LOGIN_TOKEN_KEY = 7;
        private const int INDEX_RESPONSE_LOGIN_STATUS_KEY = 13;

        private const int INDEX_RESPONSE_LOGOUT_STATUS_KEY = 7;
        private const int INDEX_RESPONSE_LOGOUT_STATUS_VALUE = 10;

        public static string Login(string URL_RPC, string username, string password, string useragent, string lang)
        {
            string content = CallWebService(URL_RPC, GetLoginRequestData(username, password, lang, useragent));

            List<string> responseList = ProcessXmlResponse(content);
            if (CheckLogin(responseList))
            {
                return responseList[INDEX_RESPONSE_TOKEN_VALUE];//token
            }
            else
            {
                return "[ERROR] Login FAILED!" + responseList[INDEX_RESPONSE_LOGIN_STATUS_VALUE] + "\r\n";
            }
        }

        public static string Logout(string URL_RPC, string token)
        {
            string content = CallWebService(URL_RPC, GetLogoutRequestData(token));

            List<string> responseList = ProcessXmlResponse(content);
            if (CheckLogout(responseList))
            {
                return "Logout Sucessfully!" + "\r\n";
            }
            else
            {
                return "[ERROR] Logout FAILED!" + DisplayResponse(responseList) + "\r\n";
            }
        }

        private static string CallWebService(string URL_RPC, string strRequestData)
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

        private static bool CheckLogin(List<string> responseList)
        {
            //displayResponse(responseList);

            return
                responseList[INDEX_RESPONSE_LOGIN_TOKEN_KEY] == "token" &&
                responseList[INDEX_RESPONSE_LOGIN_STATUS_KEY] == "status" &&
                responseList[INDEX_RESPONSE_LOGIN_STATUS_VALUE] == "200 OK";

        }

        private static bool CheckLogout(List<string> responseList)
        {
            //displayResponse(responseList);

            return
                responseList[INDEX_RESPONSE_LOGOUT_STATUS_KEY] == "status" &&
                responseList[INDEX_RESPONSE_LOGOUT_STATUS_VALUE] == "200 OK";

        }

        public static List<string> ProcessXmlResponse(string content)
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

        public static string SearchSubtitle4Movie(string URL_RPC, string fileName, string strToken, string subLanguages)
        {
            byte[] moviehash = MovieHash.ComputeMovieHash(fileName);
            string strMovieHash = MovieHash.ToHexadecimal(moviehash);

            FileInfo fileInfo = new FileInfo(fileName);
            string strSearchRequestData = GetSearchRequestData(strToken, strMovieHash, fileInfo.Length.ToString(), subLanguages);

            return CallWebService(URL_RPC, strSearchRequestData);
        }

        private static string GetLoginRequestData(string username, string password, string lang, string useragent)
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

        private static string GetLogoutRequestData(string token)
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

        private static string GetSearchRequestData(string strToken, string strMovieHash, string strMovieSize, string subLanguages)
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
                                     <value><string>" + subLanguages + @"</string>
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

        private static string DisplayResponse(List<string> responseList)
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
