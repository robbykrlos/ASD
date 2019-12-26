<?php

echo "\r\n";
echo "#############################", "\r\n";
echo "# Auto Subtitles Downloader #", "\r\n";
echo "#############################", "\r\n";
echo "\r\n";

if (!extension_loaded('xmlrpc')) {
    echo '[NOTICE] php.ini should have extension=php_xmlrpc.dll enabled', "\r\n";
} else {
    echo '[XMLPRC] Extension loaded.', "\r\n";
}

/**
 * PARAMETERS & CONFIGURATIONS
 */
//CLI parameters
$INDEX_PARAM_TARGET_PATH = 1;
$INDEX_PARAM_SILENT_RUN = 2;

// http://trac.opensubtitles.org/projects/opensubtitles/wiki/DevReadFirst
$ARR_TARGET_EXTENSIONS = '*.avi,*.dat,*.divx,*.flc,*.flv,*.h264,*.m4v,*.mkv,*.moov,*.mov,*.movie,*.movx,*.mp4,*.mpe,*.mpeg,*.mpg,*.mpv,*.mpv2,*.ogg,*.ogm,*.omf,*.ps,*.swf,*.ts,*.vfw,*.vid,*.video,,*.wm,*.wmv,*.x264,*.xvid';

//Server details + parameters
$URL_RPC = "http://api.opensubtitles.org:80/xml-rpc";
$OPENSUBTITLES_USERAGENT = 'OSTestUserAgentTemp';
$SUB_LANG = 'rum,eng';
//END : PARAMETERS & CONFIGURATIONS


//SERVER CONNECTION: 
$objXMLRPClient = new XMLRPCClient($URL_RPC);

//LOGIN
$arrUserAgent = array('', '', '', $OPENSUBTITLES_USERAGENT); //4th param
$arrResponse = $objXMLRPClient->__call('LogIn', $arrUserAgent);
//LOGIN END

if (isset($arrResponse['token'])) {
    $strTokenAccess = $arrResponse['token'];
    echo "LogIn - OK.";
} else {
    echo '[ERROR] LogIn - Failed.', "\r\n";
    var_dump($arrResponse);
    exit;
}

echo "Target folder : ", $argv[$INDEX_PARAM_TARGET_PATH], "\r\n";
$boolSilentRun = isset($argv[$INDEX_PARAM_SILENT_RUN]);

if (isset($argv[$INDEX_PARAM_TARGET_PATH])) {
    $strTargetPath = $argv[$INDEX_PARAM_TARGET_PATH];
    if (file_exists($strTargetPath)) {
        chdir($strTargetPath);
        $arrVideos = glob('{' . $ARR_TARGET_EXTENSIONS . '}', GLOB_BRACE);
        if (count($arrVideos)) {
            echo 'Found ' . count($arrVideos) . ' video files. Starting Automatic Downloading Subtitles ...', "\r\n\r\n";
            if (!$boolSilentRun) echo "---------------------------------------------------------------------\r\n";
            foreach ($arrVideos as $strVideoFilesName) {
                //Preparing RPC DATA: movie search criteria:
                $arrData = array();
                $arrSubtitleSearch = array();
                //Generate movie hash
                $strHashFileName = OpenSubtitlesHash($strVideoFilesName);

                $arrSubtitleSearch['sublanguageid'] = $SUB_LANG;
                $arrSubtitleSearch['moviehash'] = $strHashFileName;
                $arrSubtitleSearch['moviebytesize'] = find_filesize($strVideoFilesName);

                $arrData = array($strTokenAccess, array('data', $arrSubtitleSearch));
                $arrResponse = $objXMLRPClient->__call('SearchSubtitles', $arrData);

                if (count($arrResponse) && isset($arrResponse['data'])) {
                    if (!$boolSilentRun) echo "Subtitles found for :", $strVideoFilesName, "\r\n";

                    //@TODO [DONE]: here iterate other subtitles instead of taking the first one - based on whatever reasons: 
                    foreach ($arrResponse['data'] as $arrSubtitleInfo) {
                        //$arrSubtitleInfo = reset($arrResponse['data']);

                        if (isset($arrSubtitleInfo['SubDownloadLink']) && isset($arrSubtitleInfo['SubFileName'])) {
                            $strSource = $arrSubtitleInfo['SubDownloadLink'];
                            $strGZSubFileName = $arrSubtitleInfo['SubFileName'] . '.gz';
                            if (!$boolSilentRun) echo "Compressed Subtitle : ", $strGZSubFileName, "\r\n";
                            if (!$boolSilentRun) echo "Link : ", $strSource, "\r\n";

                            //Download gz file locally
                            $ch = curl_init();
                            curl_setopt($ch, CURLOPT_URL, $strSource);
                            curl_setopt($ch, CURLOPT_RETURNTRANSFER, 1);
                            curl_setopt($ch, CURLOPT_SSLVERSION, 3);
                            $data = curl_exec($ch);
                            $error = curl_error($ch);
                            curl_close($ch);

                            $file = fopen($strGZSubFileName, "w+");
                            fputs($file, $data);
                            fclose($file);
                            if (!$boolSilentRun) echo "Downloaded : ", $strGZSubFileName, "\r\n";

                            if (file_exists($strGZSubFileName)) {
                                //Extract gz file
                                //http://stackoverflow.com/questions/11265914/how-can-i-extract-or-uncompress-gzip-file-using-php
                                // Raising this value may increase performance
                                $buffer_size = 4096; // read 4kb at a time

                                //force subtitle name same as movie name.
                                $path_parts = pathinfo($strVideoFilesName);
                                $out_file_name = $path_parts['filename'] . '.srt';

                                // Open our files (in binary mode)
                                $file = gzopen($strGZSubFileName, 'rb');
                                $out_file = fopen($out_file_name, 'wb');

                                // Keep repeating until the end of the input file
                                while (!gzeof($file)) {
                                    // Read buffer-size bytes
                                    // Both fwrite and gzread and binary-safe
                                    fwrite($out_file, gzread($file, $buffer_size));
                                }

                                // Files are done, close files
                                fclose($out_file);
                                gzclose($file);
                                if (!$boolSilentRun) echo "Decompressed : ", $out_file_name, "\r\n";

                                //Deleting gz archive
                                unlink($strGZSubFileName);
                                if (!$boolSilentRun) echo "Deleted archive : ", $strGZSubFileName, "\r\n";
                            }
                            break;
                        } else {
                            if ($boolSilentRun) echo "\r\n";
                            echo '[NOTICE] No subtitle found : ', $strVideoFilesName, " - searching more...\r\n";
                            continue;
                        }
                    }
                } else {
                    if ($boolSilentRun) echo "\r\n";
                    echo '[NOTICE] No response received : ', $strVideoFilesName, "\r\n";
                }
                if (!$boolSilentRun) {
                    echo "---------------------------------------------------------------------\r\n\r\n";
                } else {
                    echo '.';
                }
            }
        } else {
            echo '[NOTICE] No targeted Files found.', "\r\n";
        }
    } else {
        echo '[NOTICE] Target folder does not exists.', "\r\n";
    }
} else {
    echo '[NOTICE] Parameter 1 missing: No target folder set.', "\r\n";
}

function find_filesize($file)
{
    if (substr(PHP_OS, 0, 3) == "WIN") {
        exec('for %I in ("' . $file . '") do @echo %~zI', $output);
        $return = $output[0];
    } else {
        $return = filesize($file);
    }
    return $return;
}

/**
 * Source:
 * http://trac.opensubtitles.org/projects/opensubtitles/wiki/HashSourceCodes
 */
function OpenSubtitlesHash($file)
{
    $handle = fopen($file, "rb");
    $fsize = find_filesize($file);

    $hash = array(3 => 0,
            2 => 0,
            1 => ($fsize >> 16) & 0xFFFF,
            0 => $fsize & 0xFFFF);

    for ($i = 0; $i < 8192; $i++) {
        $tmp = ReadUINT64($handle);
        $hash = AddUINT64($hash, $tmp);
    }

    $offset = $fsize - 65536;
    fseek64($handle, $offset > 0 ? $offset : 0);

    for ($i = 0; $i < 8192; $i++) {
        $tmp = ReadUINT64($handle);
        $hash = AddUINT64($hash, $tmp);
    }

    fclose($handle);
    return UINT64FormatHex($hash);
}

function fseek64(&$fh, $offset)
{
    fseek($fh, 0, SEEK_SET);
    $t_offset   = '' . PHP_INT_MAX;
    while (gmp_cmp((double)$offset, $t_offset) == 1)
    {
        $offset     = gmp_sub($offset, $t_offset);
        fseek($fh, gmp_intval($t_offset), SEEK_CUR);
    }
    return fseek($fh, gmp_intval($offset), SEEK_CUR);
}


function my_fseek(&$fp, $pos, $first = 0)
{
// set to 0 pos initially, one-time
    if ($first) fseek($fp, 0, SEEK_SET);

// get pos float value
    $pos = floatval($pos);

// within limits, use normal fseek
    if ($pos <= PHP_INT_MAX)
        fseek($fp, $pos, SEEK_CUR);
// out of limits, use recursive fseek
    else {
        fseek($fp, PHP_INT_MAX, SEEK_CUR);
        $pos -= PHP_INT_MAX;
        my_fseek($fp, $pos);
    }
}


function ReadUINT64($handle)
{
    $u = unpack("va/vb/vc/vd", fread($handle, 8));
    return array(0 => $u["a"], 1 => $u["b"], 2 => $u["c"], 3 => $u["d"]);
}

function AddUINT64($a, $b)
{
    $o = array(0 => 0, 1 => 0, 2 => 0, 3 => 0);

    $carry = 0;
    for ($i = 0; $i < 4; $i++) {
        if (($a[$i] + $b[$i] + $carry) > 0xffff) {
            $o[$i] += ($a[$i] + $b[$i] + $carry) & 0xffff;
            $carry = 1;
        } else {
            $o[$i] += ($a[$i] + $b[$i] + $carry);
            $carry = 0;
        }
    }

    return $o;
}

function UINT64FormatHex($n)
{
    return sprintf("%04x%04x%04x%04x", $n[3], $n[2], $n[1], $n[0]);
}


/**
 * http://stackoverflow.com/questions/718377/need-sample-xml-rpc-client-code-for-php5
 * XMLRPC Client
 *
 * Provides flexible API to interactive with XMLRPC service. This does _not_
 * restrict the developer in which calls it can send to the server. It also
 * provides no introspection (as of yet).
 *
 * Example Usage:
 *
 * include("xmlrpcclient.class.php");
 * $client = new XMLRPCClient("http://my.server.com/XMLRPC");
 * print var_export($client->myRpcMethod(0));
 * $client->close();
 *
 * Prints:
 * >>> array (
 * >>>   'message' => 'RPC method myRpcMethod invoked.',
 * >>>   'success' => true,
 * >>> )
 */
class XMLRPCClient
{
    public function __construct($uri)
    {
        $this->uri = $uri;
        $this->curl_hdl = null;
    }

    public function __destruct()
    {
        $this->close();
    }

    public function close()
    {
        if ($this->curl_hdl !== null) {
            curl_close($this->curl_hdl);
        }
        $this->curl_hdl = null;
    }

    public function setUri($uri)
    {
        $this->uri = $uri;
        $this->close();
    }

    public function __call($method, $params)
    {
        $xml = xmlrpc_encode_request($method, $params);

        if ($this->curl_hdl === null) {
            // Create cURL resource
            $this->curl_hdl = curl_init();

            // Configure options
            curl_setopt($this->curl_hdl, CURLOPT_URL, $this->uri);
            curl_setopt($this->curl_hdl, CURLOPT_HEADER, 0);
            curl_setopt($this->curl_hdl, CURLOPT_RETURNTRANSFER, true);
            curl_setopt($this->curl_hdl, CURLOPT_POST, true);
        }

        curl_setopt($this->curl_hdl, CURLOPT_POSTFIELDS, $xml);
        //var_dump($xml);

        // Invoke RPC command
        $response = curl_exec($this->curl_hdl);

        $result = xmlrpc_decode_request($response, $method);

        return $result;
    }
}