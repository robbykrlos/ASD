using System;
using System.Linq;
using System.Net;
using System.IO;
using MovieCollection.OpenSubtitles.Models;
using MovieCollection.OpenSubtitles;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;

namespace AutoSubtitleDownloader
{
    //old xml-rpc API
    public static class ASD
    {
        // HttpClient is intended to be instantiated once per application, rather than per-use.
        // See https://docs.microsoft.com/en-us/dotnet/api/system.net.http.httpclient
        private static readonly HttpClient _httpClient = new HttpClient();

        private static OpenSubtitlesOptions _options;
        private static OpenSubtitlesService _service;

        private static string _username;
        private static string _password;

        private static string _token;

        private static string APP_START_TIME = DateTime.Now.ToString("yyyy-MM-dd_HHmmss").ToString();
        private static string OPENSUBTITLES_API_KEY = string.Empty; //invalid API KEY
        private static string[] SUB_LANGUAGES = new[] { "ro", "en" };

        private const int INDEX_PARAM_TARGET_PATH = 0;
        private const int INDEX_PARAM_API_KEY = 1;
        private const int INDEX_PARAM_USERNAME = 2;
        private const int INDEX_PARAM_PASSWORD = 3;
        private const int INDEX_PARAM_SUB_LANGUAGES = 4;
        private const int INDEX_PARAM_SILENT_RUN = 5;

        private const string NEWLINE = "\r\n";

        // http://trac.opensubtitles.org/projects/opensubtitles/wiki/DevReadFirst
        public const string ARR_TARGET_EXTENSIONS = "*.avi,*.dat,*.divx,*.flc,*.flv,*.h264,*.m4v,*.mkv,*.moov,*.mov,*.movie,*.movx,*.mp4,*.mpe,*.mpeg,*.mpg,*.mpv,*.mpv2,*.ogg,*.ogm,*.omf,*.ps,*.swf,*.ts,*.vfw,*.vid,*.video,,*.wm,*.wmv,*.x264,*.xvid";

        public static async Task<string> StartAsync(string[] args)
        {
            string VersionNumber = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            
            string output = string.Empty;
            output += $"##############################################################{NEWLINE}";
            output += $"##    Auto Subtitles Downloader  (v{ VersionNumber})  Made by CRK    ##{NEWLINE}";
            output += $"##############################################################{NEWLINE}";

            //first 3 params are mandatory1
            if (args.Length < 1)
            {
                output += $" p0:PATH [p1:languages [p2:username [p3:password [p4:/s|/q]]]]{NEWLINE}{NEWLINE}";
                output += $" p0 REQ: target folder path where video files are present (with triling \\).{NEWLINE}";
                output += $" p1 REQ: api-key{NEWLINE}";
                output += $" p2 REQ: username{NEWLINE}";
                output += $" p3 REQ: password{NEWLINE}";
                output += $" p4 OPT: subtitle language(s), csv, ISO 639-2. Default rum,eng{NEWLINE}";
                output += $" p5 OPT: silent run (/q, /s silent/quiet. Default verbose){NEWLINE}{NEWLINE}";
                output += $"NOTE: Existing subs will be backed up - not overwritten{NEWLINE}";
                output += $"HINT: works well with TotalCommander with key-bind and arg %P{NEWLINE}";
                output += $"##############################################################{NEWLINE}";
                return output;
            }



            //READING ARGS
            string rootTargetPath = ".\\";
            bool boolVerboseRun = true;

            //PATH
            if (args.Length > INDEX_PARAM_TARGET_PATH && args[INDEX_PARAM_TARGET_PATH] != string.Empty)
            {
                rootTargetPath = args[INDEX_PARAM_TARGET_PATH];
            }
            if (Directory.Exists(rootTargetPath))
            {
                if (boolVerboseRun) output += $"Target directory : {NEWLINE}" + rootTargetPath + NEWLINE;
            }
            else
            {
                output += "[ERROR] Invalid target directory : " + rootTargetPath + NEWLINE;
            }

            //API KEY:
            if (args.Length > INDEX_PARAM_API_KEY && args[INDEX_PARAM_API_KEY] != string.Empty)
            {
                OPENSUBTITLES_API_KEY = args[INDEX_PARAM_API_KEY];
            }

            //USERNAME AND PASSWORD:
            if (args.Length > INDEX_PARAM_PASSWORD && args[INDEX_PARAM_USERNAME] != string.Empty && args[INDEX_PARAM_PASSWORD] != string.Empty)
            {
                _username = args[INDEX_PARAM_USERNAME];
                _password = args[INDEX_PARAM_PASSWORD];
            }

            //SUBTITLE LANGAUGES:
            if (args.Length > INDEX_PARAM_SUB_LANGUAGES && args[INDEX_PARAM_SUB_LANGUAGES] != string.Empty)
            {
                SUB_LANGUAGES = args[INDEX_PARAM_SUB_LANGUAGES].Split(',');
            }

            //SILENT RUN
            if (args.Length > INDEX_PARAM_SILENT_RUN && args[INDEX_PARAM_SILENT_RUN] != string.Empty)
            {
                boolVerboseRun = args[INDEX_PARAM_SILENT_RUN] != "/s" && args[INDEX_PARAM_SILENT_RUN] != "/q";
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

            //before doing any API call check if any files are found 
            if (listFiles.Length == 0)
            {
                output += "Fount " + listFiles.Length + " video files. Nothing to do. Exit!";
                return output;
            }
            else
            {
                output += "Fount " + listFiles.Length + " video files. Processing " + (boolVerboseRun ? $": {NEWLINE}" : "[");
            }

            _options = new OpenSubtitlesOptions
            {
                ApiKey = OPENSUBTITLES_API_KEY,
                ProductInformation = new ProductHeaderValue("robbykrlos-agent", VersionNumber),
            };

            _service = new OpenSubtitlesService(_httpClient, _options);

            output += await Login();

            //START ITERATING FILES AND REQUEST SUBS
            if (boolVerboseRun) output += "-------------------------------------------------------" + NEWLINE;
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
                    output += NEWLINE + NEWLINE + backupOutput;
                    output += "EXIT FORCEFULLY : backup cannot be done. Risk to overwrite existing subs!";
                    output += NEWLINE + "WORKAROUND : to force download, backup existing subs yourself and/or manually delete existing subs from target folder.";
                    return output;
                }
                output += backupOutput;

                if (boolVerboseRun) output += fileName + NEWLINE;

                var result = await GetSubtitlesByHash(fileName);
                output += "[NOTICE] Movie Hash : " + OpenSubtitlesHasher.GetFileHash(fileName) + NEWLINE;

                if (result.Data.Count > 0)
                {
                    //output += "[NOTICE] Movie Hash : " + OpenSubtitlesHasher.GetFileHash(fileName) + NEWLINE;
                    output += await DownloadSubtitle(result.Data.First().Attributes.Files.First().FileId, rootTargetPath, fileNameNoExtension);
                }
                else
                {
                    output += "[NOTICE] Subtitle not found for : " + fileName + NEWLINE;
                }


                if (boolVerboseRun) output += "-------------------------------------------------------" + NEWLINE;
            }

            output += await Logout();
            output += (boolVerboseRun ? string.Empty : "] ") + "DONE!" + NEWLINE;

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
                    return verbose ? $"BACKUP existing sub sucessfuly done!{NEWLINE}" : ":";
                }
                catch (Exception ex)
                {
                    return "[ERROR] BACKUP existing sub FAILED! " + ex.Message.ToString() + NEWLINE;
                }
            }
            return string.Empty; //no backup needed - retur no message.
        }

        private static async Task<string> Login()
        {
            if (!string.IsNullOrEmpty(_token))
            {
                return "### LOGIN ### Already logged in." + NEWLINE;
            }

            var login = new NewLogin
            {
                Username = _username,
                Password = _password,
            };

            var result = await _service.LoginAsync(login);

            string output = string.Empty;

            if (result.Status == 200)
            {
                // Login was successful, save the token.
                _token = result.Token;

                //output += $"Token: {result.Token}" + $"AllowedDownloads: {result.User.AllowedDownloads}" + NEWLINE;
                //output += $"Level: {result.User.Level}" + NEWLINE;
                //output += $"UserId: {result.User.UserId}" + NEWLINE;
                //output += $"ExtInstalled: {result.User.ExtInstalled}" + NEWLINE;
                //output += $"Vip: {result.User.Vip}" + NEWLINE;
            }
            else
            {
                // Login failed, show the error message.
                output += $"### LOGIN ### User: {result.User} Status: {result.Status} Message: {result.Message}{NEWLINE}";
            }

            return output;
        }

        private static async Task<string> Logout()
        {
            if (string.IsNullOrEmpty(_token))
            {
                return "### LOGOUT ### Please login first." + NEWLINE;
            }

            var logout = await _service.LogoutAsync(_token);
            _token = string.Empty;

            return $"### LOGOUT ### Status: {logout.Status} Message: {logout.Message}{NEWLINE}";
        }

        private static async Task<PagedResult<AttributeResult<Subtitle>>> GetSubtitlesByHash(string filePath)
        {
            var search = new NewSubtitleSearch
            {
                Query = Path.GetFileName(filePath),
                MovieHash = OpenSubtitlesHasher.GetFileHash(filePath),
                Languages = new[] { "en", "fa" },
            };

            var result = await _service.SearchSubtitlesAsync(search);

            return result;
        }

        private static async Task<string> DownloadSubtitle(int fileId, string destionationPath, string subFileName)
        {
            string output = string.Empty;
            var download = new NewDownload
            {
                FileId = fileId,
                FileName = subFileName
            };

            var result = await _service.GetSubtitleForDownloadAsync(download, _token);

            //Console.WriteLine($"FileName: {result.FileName}");
            //Console.WriteLine($"Requests: {result.Requests}");
            //Console.WriteLine($"Remaining: {result.Remaining}");
            //Console.WriteLine($"Message: {result.Message}");
            //Console.WriteLine($"Link: {result.Link}");

            var path = Path.Combine(destionationPath, result.FileName);

            try
            {
                //Console.WriteLine();
                //Console.WriteLine($"Downloading to: {path}");

                var webClient = new WebClient();
                await webClient.DownloadFileTaskAsync(result.Link, path);

                output +="Download was successful." + NEWLINE;
            }
            catch (Exception ex)
            {
                output += $"Download file error: { ex.Message} + " + NEWLINE;
            }

            return output;
        }

        private static void Print(AttributeResult<Subtitle> item)
        {
            Console.WriteLine($"SubtitleId: {item.Attributes.SubtitleId}");
            Console.WriteLine($"Language: {item.Attributes.Language}");
            Console.WriteLine($"DownloadCount: {item.Attributes.DownloadCount}");
            Console.WriteLine($"NewDownloadCount: {item.Attributes.NewDownloadCount}");
            Console.WriteLine($"Release: {item.Attributes.Release}");
            Console.WriteLine($"HearingImpaired: {item.Attributes.HearingImpaired}");

            Console.WriteLine();

            foreach (var file in item.Attributes.Files)
            {
                Console.WriteLine($"FileId: {file.FileId}");
                Console.WriteLine($"FileName: {file.FileName}");
                Console.WriteLine($"CD Number: {file.CdNumber}");
                Console.WriteLine();
            }
        }
    }
}
