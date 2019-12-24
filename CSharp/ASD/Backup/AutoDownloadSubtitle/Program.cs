using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Net;
using System.IO;
using System.Xml;
using ASD;
using System.IO.Compression;

namespace AutoDownloadSubtitle
{
    class Program
    {
        public const int INDEX_PARAM_TARGET_PATH = 1;
        public const int INDEX_PARAM_SILENT_RUN = 2;

        // http://trac.opensubtitles.org/projects/opensubtitles/wiki/DevReadFirst
        public const string ARR_TARGET_EXTENSIONS = "*.avi,*.dat,*.divx,*.flc,*.flv,*.h264,*.m4v,*.mkv,*.moov,*.mov,*.movie,*.movx,*.mp4,*.mpe,*.mpeg,*.mpg,*.mpv,*.mpv2,*.ogg,*.ogm,*.omf,*.ps,*.swf,*.ts,*.vfw,*.vid,*.video,,*.wm,*.wmv,*.x264,*.xvid";

        //Server details + parameters
        public const string URL_RPC = "http://api.opensubtitles.org:80/xml-rpc";
        public const string OPENSUBTITLES_USERAGENT = "OSTestUserAgentTemp";
        public const string SUB_LANG = "rum,eng";

        static void Main(string[] args)
        {
            Console.WriteLine("#############################");
            Console.WriteLine("# Auto Subtitles Downloader #");
            Console.WriteLine("#############################");
            Console.WriteLine(" param1: target folder path");
            Console.WriteLine(" param2: silent run");
            Console.WriteLine("#############################");

            //READING ARGS
            string rootTargetPath = ".\\";
            bool boolVerboseRun = true;
            
            //PATH
            if (args.Length > 0 && args[0] != "")
            {
                rootTargetPath = args[0];
            }
            if (Directory.Exists(rootTargetPath))
            {
                if (boolVerboseRun) Console.WriteLine("Target directory set : " + rootTargetPath);
            }
            else
            {
                Console.WriteLine("[ERROR] Invalid target directory : " + rootTargetPath);
            }

            //SILENT RUN
            if (args.Length > 1 && args[1] != "")
            {
                boolVerboseRun = false;
            }

            string strLoginToken = OpenSubtitleUtils.Login(URL_RPC);
            if (boolVerboseRun) Console.WriteLine("Valid token received : " + strLoginToken);

            string[] listFiles = (string[]) Directory.GetFiles(rootTargetPath, "*.*", SearchOption.TopDirectoryOnly).
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


            Console.WriteLine("Fount " + listFiles.Length + " video files. Processing ...");
            if (boolVerboseRun) Console.WriteLine("-------------------------------------------------------");
            foreach (string fileName in listFiles)
            {
                FileInfo fileInfo = new FileInfo(fileName);
                string fileNameNoExtension = Path.GetFileNameWithoutExtension(fileInfo.Name);
                string gzFile = fileNameNoExtension + ".gz";
                string subFile = fileNameNoExtension + ".srt";

                if (boolVerboseRun) Console.WriteLine(fileName);
                string strResponseData = OpenSubtitleUtils.SearchSubtitle4Movie(URL_RPC, fileName, strLoginToken);
                List<string> listReponseSearch = OpenSubtitleUtils.processXmlResponse(strResponseData);
                for (int i=0; i<listReponseSearch.Count; i++)
                {
                    //Console.WriteLine(listReponseSearch[i]);
                    if (listReponseSearch[i] == "SubDownloadLink")
                    {
                        if (listReponseSearch[i+3].StartsWith("http"))
                        {
                            using (var client = new WebClient())
                            {
                                try
                                {
                                    client.DownloadFile(listReponseSearch[i + 3], rootTargetPath + gzFile);
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine("[ERROR] " + e.Message);
                                    Console.WriteLine("Press any key to continue...");
                                    Console.ReadKey();
                                    Environment.Exit(0);
                                }

                                if (boolVerboseRun) Console.WriteLine("Downloaded : " + gzFile);

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
                                if (boolVerboseRun) Console.WriteLine("Decompressed : " + subFile);
                                File.Delete(rootTargetPath + gzFile);
                                if (boolVerboseRun) Console.WriteLine("Deleted : " + gzFile);
                            }
                            if (boolVerboseRun)
                            {
                                Console.WriteLine("Successfully saved : " + subFile);
                            }
                            else
                            {
                                Console.Write(".");
                            }
                            break;
                        }
                    }
                    if (i >= listReponseSearch.Count - 1)
                    {
                        Console.WriteLine("[NOTICE] Subtitle not found for : " + fileName);
                    }
                }
                //Console.WriteLine(strResponseData);
                if (boolVerboseRun) Console.WriteLine("-------------------------------------------------------");
            }
            Console.WriteLine("DONE!");

            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();

        }
    }
}
