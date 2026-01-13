using Devmasters.Net.HttpClient;
using HlidacStatu.Entities;
using HlidacStatu.Entities.XSD;
using HlidacStatu.Lib.Data.External;
using HlidacStatu.Lib.OCR;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Threading.Tasks;
using static HlidacStatu.Connectors.IO.PrilohaFile;
using static HlidacStatu.Entities.Smlouva;


namespace HlidacStatu.Repositories
{
    public partial class SmlouvaRepo
    {
        private static Connectors.IO.PrilohaFile PrilohaLocalCopy = new Connectors.IO.PrilohaFile();

        public static bool ExistLocalCopyOfPriloha(Smlouva obj, Smlouva.Priloha priloha,
            RequestedFileType filetype = RequestedFileType.Original)
        {
            bool weHaveCopy = System.IO.File.Exists(PrilohaLocalCopy.GetFullPath(obj, priloha, filetype));
            return weHaveCopy;
        }

        public static void SaveAttachmentsToDisk(Smlouva smlouva, bool rewriteExisting = false)
        {
            var io = PrilohaLocalCopy;

            int count = 0;
            string listing = "";
            if (smlouva.Prilohy != null)
            {
                if (!Directory.Exists(io.GetFullDir(smlouva)))
                    Directory.CreateDirectory(io.GetFullDir(smlouva));

                foreach (var p in smlouva.Prilohy)
                {
                    string attUrl = p.odkaz;
                    if (string.IsNullOrEmpty(attUrl))
                        continue;
                    count++;
                    string fullPath = io.GetFullPath(smlouva, p);
                    listing = listing + string.Format("{0} : {1} \n", count, WebUtility.UrlDecode(attUrl));
                    if (!File.Exists(fullPath) || rewriteExisting)
                    {
                        try
                        {
                            using (URLContent url =
                                   new URLContent(attUrl))
                            {
                                url.Tries = 5;
                                url.IgnoreHttpErrors = true;
                                url.TimeInMsBetweenTries = 1000;
                                url.Timeout = url.Timeout * 20;
                                byte[] data = url.GetBinary().Binary;
                                File.WriteAllBytes(fullPath, data);
                                //p.LocalCopy = System.Text.UTF8Encoding.UTF8.GetBytes(io.GetRelativePath(item, p));
                            }
                        }
                        catch (Exception e)
                        {
                            _logger.Error(e, attUrl);
                        }
                    }

                    if (p.hash == null)
                    {
                        using (FileStream filestream = new FileStream(fullPath, FileMode.Open))
                        {
                            using (SHA256 mySHA256 = SHA256.Create())
                            {
                                filestream.Position = 0;
                                byte[] hashValue = mySHA256.ComputeHash(filestream);
                                p.hash = new tHash()
                                {
                                    algoritmus = "sha256",
                                    Value = BitConverter.ToString(hashValue).Replace("-", String.Empty)
                                };
                            }
                        }
                    }
                }
            }
        }


        public static string GetPathFromPrilohaRepository(Smlouva smlouva)
        {
            return PrilohaLocalCopy.GetFullDir(smlouva);
        }

        public static string GetFullFilePathFromPrilohaRepository(Smlouva smlouva, Priloha priloha,
            RequestedFileType filetype = RequestedFileType.Original)
        {
            var fn = PrilohaLocalCopy.GetFullPath(smlouva, priloha);
            if (filetype == RequestedFileType.PDF)
                fn = fn + ".pdf";
            return fn;
        }


        public static async Task<string> GetDownloadedPrilohaPathAsync(Smlouva.Priloha att,
            Smlouva smlouva, RequestedFileType filetype = RequestedFileType.Original)
        {
            var ext = ".pdf";
            try
            {
                ext = new System.IO.FileInfo(att.nazevSouboru).Extension;
            }
            catch (Exception)
            {
                _logger.Warning("invalid file name " + (att?.nazevSouboru ?? "(null)"));
            }


            string localDir = PrilohaLocalCopy.GetFullDir(smlouva);
            if (!System.IO.Directory.Exists(localDir))
                System.IO.Directory.CreateDirectory(localDir);

            string localFile = PrilohaLocalCopy.GetFullPath(smlouva, att);

            //System.IO.File.Delete(fn);
            if (!System.IO.File.Exists(localFile))
            {
                try
                {
                    _logger.Debug(
                        $"Downloading priloha {att.nazevSouboru} for smlouva {smlouva.Id} from URL {att.odkaz}");
                    byte[] data = null;
                    using (Devmasters.Net.HttpClient.URLContent web =
                           new Devmasters.Net.HttpClient.URLContent(att.odkaz))
                    {
                        web.Timeout = web.Timeout * 10;
                        data = web.GetBinary().Binary;
                        await System.IO.File.WriteAllBytesAsync(localFile, data);
                    }

                    _logger.Debug(
                        $"Downloaded priloha {att.nazevSouboru} for smlouva {smlouva.Id} from URL {att.odkaz}");
                }
                catch (Exception)
                {
                    try
                    {
                        if (Uri.TryCreate(att.odkaz, UriKind.Absolute, out var _))
                        {
                            byte[] data = null;
                            _logger.Debug(
                                $"Second try: Downloading priloha {att.nazevSouboru} for smlouva {smlouva.Id} from URL {att.odkaz}");
                            using (Devmasters.Net.HttpClient.URLContent web =
                                   new Devmasters.Net.HttpClient.URLContent(att.odkaz))
                            {
                                web.Tries = 5;
                                web.IgnoreHttpErrors = true;
                                web.TimeInMsBetweenTries = 1000;
                                web.Timeout = web.Timeout * 20;
                                data = web.GetBinary().Binary;
                                await System.IO.File.WriteAllBytesAsync(localFile, data);
                            }

                            _logger.Debug(
                                $"Second try: Downloaded priloha {att.nazevSouboru} for smlouva {smlouva.Id} from URL {att.odkaz}");
                        }
                        else
                            return null;
                    }
                    catch (Exception e)
                    {
                        _logger.Error(e, att.odkaz);
                        return null;
                    }
                }
            }

            if (filetype == RequestedFileType.PDF)
            {
                if (System.IO.File.Exists($"{localFile}.pdf"))
                    return $"{localFile}.pdf";

                if (HlidacStatu.Util.FileMime.HasPdfHeader(localFile) == false)
                {
                    try
                    {
                        var pdfdata =
                            await ConvertPrilohaToPDF.PrilohaToPDFfromFileAsync(await System.IO.File.ReadAllBytesAsync(localFile));
                        if (pdfdata == null)
                            return localFile;
                        else
                            await System.IO.File.WriteAllBytesAsync($"{localFile}.pdf", pdfdata);

                        return $"{localFile}.pdf";
                    }
                    catch (Exception e)
                    {
                        _logger.Error(e, "Cannot convert into PDF {localfile}", localFile);
                        return localFile;
                    }
                }
                else //this is PDF
                    return localFile;
            }

            return localFile;
        }


        public static async Task<string> GetCopyOfDownloadedPrilohaPathAsync(Smlouva.Priloha att,
            Smlouva smlouva, RequestedFileType filetype = RequestedFileType.Original)
        {
            string tmpFnSystem = null;
            string tmpFn = null;
            try
            {
                var origFile = await GetDownloadedPrilohaPathAsync(att, smlouva, filetype);

                if (string.IsNullOrEmpty(origFile))
                    return null;

                string localFile = PrilohaLocalCopy.GetFullPath(smlouva, att);
                var tmpPath = System.IO.Path.GetTempPath();
                //Devmasters.IO.IOTools.DeleteFile(tmpPath);
                if (!System.IO.Directory.Exists(tmpPath))
                {
                    try
                    {
                        System.IO.Directory.CreateDirectory(tmpPath);
                    }
                    catch
                    {
                    }
                }

                tmpFnSystem = System.IO.Path.GetTempFileName();
                tmpFn = tmpFnSystem + DocTools.PrepareFilenameForOCR(att.nazevSouboru);
                System.IO.File.Copy(origFile, tmpFn, true);
                return tmpFn;
            }
            catch (Exception e)
            {
                _logger.Error(e, "Error in GetCopyOfDownloadedPrilohaPath {smlouvaId} {prilohaHash}", smlouva.Id,
                    att.UniqueHash());
                throw;
            }
            finally
            {
                if (tmpFnSystem != null && System.IO.File.Exists(tmpFnSystem))
                {
                    _ = Devmasters.IO.IOTools.DeleteFile(tmpFnSystem);
                }
            }
        }


        public static async Task UpdateStatisticsAsync(this Priloha p, Smlouva s)
        {
            p.Lenght = p.PlainTextContent?.Length ?? 0;
            p.WordCount = Devmasters.TextUtil.CountWords(p.PlainTextContent);
            var variance = Devmasters.TextUtil.WordsVarianceInText(p.PlainTextContent);
            p.UniqueWordsCount = variance.Item2;
            p.WordsVariance = variance.Item1;

            //find content type
            //first check in already exists
            if (string.IsNullOrEmpty(p.ContentType))
            {
                string contentType = "";
                //scan metadata
                if (p.FileMetadata.Any(m => m.Key.ToLower() == "content-type"))
                    contentType = p.FileMetadata.First(m => m.Key.ToLower() == "content-type").Value;
                else
                {
                    var fnType = HlidacStatu.Util.FileMime.GetTopFileType(
                        await GetDownloadedPrilohaPathAsync(p, s, RequestedFileType.Original)
                    );
                    if (fnType != null)
                        contentType = fnType.MimeType;
                    else
                    {
                        if (fnType == null &&
                            (
                                p.nazevSouboru?.EndsWith(".txt") == true
                                || p.nazevSouboru?.EndsWith(".csv") == true
                                || p.nazevSouboru?.EndsWith(".tab") == true
                            )
                           ) //probably text file
                        {
                            contentType = "text/plain";
                        }

                        if (string.IsNullOrEmpty(contentType))
                        {
                            var tikaRes =
                                await AI.Doc.GetTextAsync(await GetDownloadedPrilohaPathAsync(p, s, RequestedFileType.Original));

                            if (tikaRes != null)
                                contentType = tikaRes.ContentType;
                        }
                    }
                }

                p.ContentType = contentType;
            }
        }
    }
}