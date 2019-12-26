using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.IO;
using System.IO.Compression;

namespace AutoSubtitleDownloader
{
    public static class ASD
    {
        private static string APP_START_TIME = DateTime.Now.ToString("yyyy-MM-dd_HHmmss").ToString();

        private const int INDEX_PARAM_TARGET_PATH = 0;
        private const int INDEX_PARAM_SUB_LANGUAGES = 1;
        private const int INDEX_PARAM_USERNAME = 2;
        private const int INDEX_PARAM_PASSWORD = 3;
        private const int INDEX_PARAM_SILENT_RUN = 4;

        // http://trac.opensubtitles.org/projects/opensubtitles/wiki/DevReadFirst
        public const string ARR_TARGET_EXTENSIONS = "*.avi,*.dat,*.divx,*.flc,*.flv,*.h264,*.m4v,*.mkv,*.moov,*.mov,*.movie,*.movx,*.mp4,*.mpe,*.mpeg,*.mpg,*.mpv,*.mpv2,*.ogg,*.ogm,*.omf,*.ps,*.swf,*.ts,*.vfw,*.vid,*.video,,*.wm,*.wmv,*.x264,*.xvid";

        //Server details + parameters
        private const string URL_RPC = "https://api.opensubtitles.org:443/xml-rpc";
        private const string OPENSUBTITLES_USERAGENT = "TemporaryUserAgent";
        private const string OPENSUBTITLES_COMMUNICATION_LANG = "en";

        public static string Start(string[] args)
        {
            string VersionNumber = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();

            string output = "";
            output += "##############################################################\r\n";
            output += "##    Auto Subtitles Downloader  (v"+ VersionNumber + ")   Made by CRK   ##\r\n";
            output += "##############################################################\r\n";
            
            //first 3 params are mandatory1
            if (args.Length < 1)
            {
                output += " p0:PATH [p1:languages [p2:username [p3:password [p4:/s|/q]]]]\r\n\r\n";
                output += " p0 REQ: target folder path where video files are present.\r\n";
                output += " p1 OPT: subtitle language(s), csv, ISO 639-2. Default rum,eng\r\n";
                output += " p2 OPT: username (works anonymously too, ommit or \"\")\r\n";
                output += " p3 OPT: password (works anonymously too, ommit or \"\")\r\n";
                output += " p4 OPT: silent run (/q, /s silent/quiet. Default verbose)\r\n\r\n";
                output += "NOTE: Existing subs will be backed up - not overwritten\r\n";
                output += "HINT: works well with TotalCommander with keybind and arg %P\r\n";
                output += "##############################################################\r\n";
                return output;
            }

            string OPENSUBTITLES_USERNAME = "";
            string OPENSUBTITLES_PASSWORD = "";
            string SUB_LANGUAGES = "rum,eng";//default;

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

            //SUBTITLE LANGAUGES:
            if (args.Length > INDEX_PARAM_SUB_LANGUAGES && args[INDEX_PARAM_SUB_LANGUAGES] != "")
            {
                SUB_LANGUAGES = args[INDEX_PARAM_SUB_LANGUAGES];
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
                boolVerboseRun = args[INDEX_PARAM_SILENT_RUN] != "/s" && args[INDEX_PARAM_SILENT_RUN] != "/q";
            }

            //LOGIN
            string strLoginToken = OpenSubtitleUtils.Login(URL_RPC, OPENSUBTITLES_USERNAME, OPENSUBTITLES_PASSWORD, OPENSUBTITLES_USERAGENT, OPENSUBTITLES_COMMUNICATION_LANG);
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

            //START ITERATING FILES AND REQUEST SUBS
            output += "Fount " + listFiles.Length + " video files. Processing " + (boolVerboseRun ? "..." : "[");
            if (boolVerboseRun) output += "-------------------------------------------------------" + "\r\n";
            foreach (string fileName in listFiles)
            {
                FileInfo fileInfo = new FileInfo(fileName);
                string fileNameNoExtension = Path.GetFileNameWithoutExtension(fileInfo.Name);
                string gzFile = fileNameNoExtension + ".gz";
                string subFile = fileNameNoExtension + ".srt";

                //Handle existing sub backup
                string backupOutput = HandleBackupExistingSub(rootTargetPath, subFile, boolVerboseRun);
                if (backupOutput.Contains("[ERROR]"))
                {
                    //exit app since there is the risk to overwrite existing subs which failed to be backed up.
                    output += "\r\n\r\n" + backupOutput;
                    output += "EXIT FORCEFULY : backup cannot be done. Risk to overwrite existing subs!";
                    output += "\r\nWORKAROUND : to force download, backup existing subs yourself and/or manually delete existing subs from target folder.";
                    return output;
                }
                output += backupOutput;

                if (boolVerboseRun) output += fileName + "\r\n";
                string strResponseData = OpenSubtitleUtils.SearchSubtitle4Movie(URL_RPC, fileName, strLoginToken, SUB_LANGUAGES);
                List<string> listReponseSearch = OpenSubtitleUtils.ProcessXmlResponse(strResponseData);
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
            string logoutResponseOutput = OpenSubtitleUtils.Logout(URL_RPC, strLoginToken);
            if (boolVerboseRun)
            {
                output += "\r\n" + logoutResponseOutput + "\r\n";
            }
            output += (boolVerboseRun ? "" : "] ") + "DONE!" + "\r\n";

            return output;
        }

        private static string HandleBackupExistingSub(string rootFolderPath, string subFileName, bool verbose)
        {
            string subFullFilePath = rootFolderPath + subFileName;
            if (File.Exists(subFullFilePath))
            {
                try
                {
                    string backupFolder = rootFolderPath + "SUB_backups";
                    if (!Directory.Exists(backupFolder)) Directory.CreateDirectory(backupFolder);
                    string currentBackupFolder = backupFolder + "\\" + APP_START_TIME;
                    if (!Directory.Exists(currentBackupFolder)) Directory.CreateDirectory(currentBackupFolder);
                    File.Move(subFullFilePath, currentBackupFolder + "\\" + subFileName);
                    return verbose ? "BACKUP existing sub sucessfuly done!\r\n" : ":";
                } catch (Exception ex)
                {
                    return "[ERROR] BACKUP existing sub FAILED! " + ex.Message.ToString() + "\r\n";
                }
            }
            return ""; //no backup needed - retur no message.
        }
    }
}
