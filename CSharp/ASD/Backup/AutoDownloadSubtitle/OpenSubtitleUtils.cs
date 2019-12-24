using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using System.Xml;
using ASD;

namespace AutoDownloadSubtitle
{
    public static class OpenSubtitleUtils
    {
        public static string Login(string URL_RPC)
        {
            string content = callWebService(URL_RPC, getLoginRequestData());

            List<string> responseList = processXmlResponse(content);
            if (checkLogin(responseList))
            {
                return responseList[10];//token
            }
            else
            {
                return "";
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
            return
                responseList[7] == "token" &&
                responseList[13] == "status" &&
                responseList[16] == "200 OK";
                
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
            string strSearchRequestData = getSearchRequestData(strToken,strMovieHash,fileInfo.Length.ToString());

            Console.WriteLine(strMovieHash);
            Console.WriteLine(fileInfo.Length.ToString());

            return callWebService(URL_RPC, strSearchRequestData);
        }

        private static string getLoginRequestData()
        {
            return "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                                    "<methodCall>" +
                                    "<methodName>LogIn</methodName>" +
                                    "<params>" +
                                    "<param>" +
                                    "<value><string></string></value>" +
                                    "</param>" +
                                    "<param>" +
                                    "<value><string></string></value>" +
                                    "</param>" +
                                    "<param>" +
                                    "<value><string></string></value>" +
                                    "</param>" +
                                    "<param>" +
                                    "<value><string>OSTestUserAgentTemp</string></value>" +
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
                               <value><string>" + strToken+ @"</string></value>
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
    }
}
