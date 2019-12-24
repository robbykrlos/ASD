using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.IO;
using System.IO.Compression;

namespace AutoDownloadSubtitle
{
    public static class ASD
    {
        public const int INDEX_PARAM_TARGET_PATH = 0;
        public const int INDEX_PARAM_USERNAME = 1;
        public const int INDEX_PARAM_PASSWORD = 2;
        public const int INDEX_PARAM_SILENT_RUN = 3;

        // http://trac.opensubtitles.org/projects/opensubtitles/wiki/DevReadFirst
        public const string ARR_TARGET_EXTENSIONS = "*.avi,*.dat,*.divx,*.flc,*.flv,*.h264,*.m4v,*.mkv,*.moov,*.mov,*.movie,*.movx,*.mp4,*.mpe,*.mpeg,*.mpg,*.mpv,*.mpv2,*.ogg,*.ogm,*.omf,*.ps,*.swf,*.ts,*.vfw,*.vid,*.video,,*.wm,*.wmv,*.x264,*.xvid";

        //Server details + parameters
        public const string URL_RPC = "https://api.opensubtitles.org:443/xml-rpc";
        public const string OPENSUBTITLES_USERAGENT = "OSTestUserAgentTemp";
        public const string SUB_LANG = "rum,eng";

        public static string Start(string[] args)
        {
            string VersionNumber = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();

            string output = "";
            output += "##########################################################\r\n";
            output += "######     Auto Subtitles Downloader  (v"+ VersionNumber + ")    ######\r\n";
            output += "##########################################################\r\n";
            //first 3 params are mandatory
            if (args.Length < 1)
            {
                output += " param 0 REQ: target folder path\r\n";
                output += " param 1 OPT: username (works anonymous)\r\n";
                output += " param 2 OPT: password (works anonymous)\r\n";
                output += " param 3 OPT: silent run\r\n";
                output += "##########################################################\r\n";
                return output;
            }

            string OPENSUBTITLES_USERNAME = "";
            string OPENSUBTITLES_PASSWORD = "";

            //READING ARGS
            string rootTargetPath = ".\\";
            bool boolVerboseRun = true;

            //PATH
            if (args.Length > INDEX_PARAM_TARGET_PATH && args[INDEX_PARAM_TARGET_PATH] != "")
            {
                rootTargetPath = args[INDEX_PARAM_TARGET_PATH];
            }
            if (Directory.Exists(rootTargetPath))
            {
                if (boolVerboseRun) output += "Target directory set : " + rootTargetPath + "\r\n";
            }
            else
            {
                output += "[ERROR] Invalid target directory : " + rootTargetPath + "\r\n";
            }

            //USERNAME AND PASSWORD:
            if (args.Length > INDEX_PARAM_PASSWORD && args[INDEX_PARAM_USERNAME] != "" && args[INDEX_PARAM_PASSWORD] != "")
            {
                OPENSUBTITLES_USERNAME = args[INDEX_PARAM_USERNAME];
                OPENSUBTITLES_PASSWORD = args[INDEX_PARAM_PASSWORD];
            }

            //SILENT RUN
            if (args.Length > INDEX_PARAM_SILENT_RUN && args[INDEX_PARAM_SILENT_RUN] != "")
            {
                boolVerboseRun = false;
            }

            string strLoginToken = OpenSubtitleUtils.Login(URL_RPC, OPENSUBTITLES_USERNAME, OPENSUBTITLES_PASSWORD);
            if (boolVerboseRun)
            {
                if (!strLoginToken.Contains("[ERROR]"))
                {
                    output += "Login SUCCESSFUL - valid token received : " + strLoginToken + "\r\n";
                }
                else
                {
                    output += strLoginToken + "INVALID token!" + "\r\n";
                    return output;
                }
            }

            string[] listFiles = (string[])Directory.GetFiles(rootTargetPath, "*.*", SearchOption.TopDirectoryOnly).
                Where(s => s.EndsWith(".avi") ||
                    s.EndsWith(".dat") ||
                    s.EndsWith(".divx") ||
                    s.EndsWith(".flc") ||
                    s.EndsWith(".flv") ||
                    s.EndsWith(".h264") ||
                    s.EndsWith(".m4v") ||
                    s.EndsWith(".mkv") ||
                    s.EndsWith(".moov") ||
                    s.EndsWith(".mov") ||
                    s.EndsWith(".movie") ||
                    s.EndsWith(".movx") ||
                    s.EndsWith(".mp4") ||
                    s.EndsWith(".mpe") ||
                    s.EndsWith(".mpeg") ||
                    s.EndsWith(".mpg") ||
                    s.EndsWith(".mpv") ||
                    s.EndsWith(".mpv2") ||
                    s.EndsWith(".ogg") ||
                    s.EndsWith(".ogm") ||
                    s.EndsWith(".omf") ||
                    s.EndsWith(".ps") ||
                    s.EndsWith(".swf") ||
                    s.EndsWith(".ts") ||
                    s.EndsWith(".vfw") ||
                    s.EndsWith(".vid") ||
                    s.EndsWith(".video") ||
                    s.EndsWith(".wm") ||
                    s.EndsWith(".wmv") ||
                    s.EndsWith(".x264") ||
                    s.EndsWith(".xvid")).ToArray();


            output += "Fount " + listFiles.Length + " video files. Processing ..." + "\r\n";
            if (boolVerboseRun) output += "-------------------------------------------------------" + "\r\n";
            foreach (string fileName in listFiles)
            {
                FileInfo fileInfo = new FileInfo(fileName);
                string fileNameNoExtension = Path.GetFileNameWithoutExtension(fileInfo.Name);
                string gzFile = fileNameNoExtension + ".gz";
                string subFile = fileNameNoExtension + ".srt";

                if (boolVerboseRun) output += fileName + "\r\n";
                string strResponseData = OpenSubtitleUtils.SearchSubtitle4Movie(URL_RPC, fileName, strLoginToken);
                List<string> listReponseSearch = OpenSubtitleUtils.processXmlResponse(strResponseData);
                for (int i = 0; i < listReponseSearch.Count; i++)
                {
                    //output += "listReponseSearch[i]);
                    if (listReponseSearch[i] == "SubDownloadLink")
                    {
                        if (listReponseSearch[i + 3].StartsWith("http"))
                        {
                            using (var client = new WebClient())
                            {
                                try
                                {
                                    client.DownloadFile(listReponseSearch[i + 3], rootTargetPath + gzFile);
                                }
                                catch (Exception e)
                                {
                                    output += "[ERROR] " + e.Message + "\r\n";
                                    output += "Press any key to continue..." + "\r\n";
                                    Console.ReadKey();
                                    Environment.Exit(0);
                                }

                                if (boolVerboseRun) output += "Downloaded : " + gzFile + "\r\n";

                                using (Stream fd = File.Create(rootTargetPath + subFile))
                                using (Stream fs = File.OpenRead(rootTargetPath + gzFile))
                                using (Stream csStream = new GZipStream(fs, CompressionMode.Decompress))
                                {
                                    byte[] buffer = new byte[1024];
                                    int nRead;
                                    while ((nRead = csStream.Read(buffer, 0, buffer.Length)) > 0)
                                    {
                                        fd.Write(buffer, 0, nRead);
                                    }
                                }
                                if (boolVerboseRun) output += "Decompressed : " + subFile + "\r\n";
                                File.Delete(rootTargetPath + gzFile);
                                if (boolVerboseRun) output += "Deleted : " + gzFile + "\r\n";
                            }
                            if (boolVerboseRun)
                            {
                                output += "Successfully saved : " + subFile + "\r\n";
                            }
                            else
                            {
                                output += ".";
                            }
                            break;
                        }
                    }
                    if (i >= listReponseSearch.Count - 1)
                    {
                        output += "[NOTICE] Subtitle not found for : " + fileName + "\r\n";
                    }
                }
                //output += "strResponseData);
                if (boolVerboseRun) output += "-------------------------------------------------------" + "\r\n";
            }

            //LOGOUT
            if (boolVerboseRun)
            {
                output += "\r\n" + OpenSubtitleUtils.Logout(URL_RPC, strLoginToken) + "\r\n";
            }
            output += "DONE!" + "\r\n";

            return output;
        }
    }
}
