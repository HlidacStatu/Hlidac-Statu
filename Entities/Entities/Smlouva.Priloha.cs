using System;
using System.Collections.Generic;
using System.Linq;

using HlidacStatu.Util;

using Nest;

namespace HlidacStatu.Entities
{
    public partial class Smlouva
    {
        public class Priloha : XSD.tPrilohaOdkaz
        {
            public class KeyVal
            {
                [Keyword()]
                public string Key { get; set; }
                [Keyword()]
                public string Value { get; set; }
            }

            public class BlurredPagesStats
            {
                public BlurredPagesStats() { }
                public BlurredPagesStats(IEnumerable<PageMetadata> blurredPages)
                {
                    if (blurredPages == null)
                        throw new ArgumentNullException("blurredPages");

                    if (blurredPages.Any())
                    {
                        decimal wholeArea = (decimal)(blurredPages.Sum(m => m.Blurred.BlackenArea) + blurredPages.Sum(m => m.Blurred.TextArea));
                        if (wholeArea == 0)
                            this.BlurredAreaPerc = 0;
                        else
                            this.BlurredAreaPerc = (decimal)blurredPages.Sum(m => m.Blurred.BlackenArea)
                                / (decimal)(blurredPages.Sum(m => m.Blurred.BlackenArea) + blurredPages.Sum(m => m.Blurred.TextArea));
                        this.NumOfBlurredPages = blurredPages.Count(m => m.Blurred.BlackenAreaRatio() >= 0.05m);
                        this.NumOfExtensivelyBlurredPages = blurredPages.Count(m => m.Blurred.BlackenAreaRatio() >= 0.2m);

                        this.ListOfExtensivelyBlurredPages = blurredPages
                                .Where(m => m.Blurred.BlackenAreaRatio() >= 0.2m)
                                .Select(m => m.PageNum)
                                .ToArray();
                        this.Created = DateTime.Now;
                        int maxPageNum = blurredPages.Max(m => m.PageNum);
                        bool skipedPages = false;
                        if (blurredPages.Count() == 1 && maxPageNum > 1)
                            skipedPages = true;

                        if (blurredPages.Count() > 1)
                            skipedPages = blurredPages
                                            .Select(m => m.PageNum)
                                            .OrderBy(o => o)
                                            .Where((m, i) => m != i + 1)
                                            .Any(); 

                        this.AllPagesAnalyzed = (blurredPages.Count() == maxPageNum) && !skipedPages;
                    }
                    else
                    {
                        throw new System.ArgumentOutOfRangeException("blurredPages", "No data");
                    }
                }

                public decimal BlurredAreaPerc { get; set; }
                public int NumOfBlurredPages { get; set; }
                public int NumOfExtensivelyBlurredPages { get; set; }
                public int[] ListOfExtensivelyBlurredPages { get; set; }
                public bool? AllPagesAnalyzed { get; set; } = null;
                public DateTime? Created { get; set; }
            }

            public KeyVal[] FileMetadata = new KeyVal[] { };

            public BlurredPagesStats BlurredPages { get; set; }

            string _plainTextContent = "";
            [Text()]
            public string PlainTextContent
            {
                get { return _plainTextContent; }
                set
                {
                    _plainTextContent = value;
                }
            }


            public DataQualityEnum PlainTextContentQuality { get; set; } = DataQualityEnum.Unknown;

            [Date]
            public DateTime LastUpdate { get; set; } = DateTime.MinValue;

            public byte[] LocalCopy { get; set; } = null;

            [Keyword()]
            public string ContentType { get; set; }
            public int Lenght { get; set; }
            public int WordCount { get; set; }
            public long UniqueWordsCount { get; set; }
            public decimal WordsVariance { get; set; }

            public int Pages { get; set; }




            [Object(Ignore = true)]
            public bool EnoughExtractedText
            {
                get
                {
                    if (this.WordCount > 0 && this.Lenght>0 && this.UniqueWordsCount>0)
                        return HlidacStatu.Util.ParseTools.EnoughExtractedTextCheck(this.WordCount, this.Lenght, this.UniqueWordsCount, this.WordsVariance);
                    else
                        return ParseTools.EnoughExtractedTextCheck(PlainTextContent, Pages);
                }
            }

            public Priloha()
            {
            }
            public Priloha(XSD.tPrilohaOdkaz tpriloha)
            {
                hash = tpriloha.hash;
                nazevSouboru = tpriloha.nazevSouboru;
                odkaz = tpriloha.odkaz;
                //if (tpriloha.data != null)
                //{
                //    //priloha je soucasti tpriloha, uloz


                //}
            }

            public static string GetUrl(string smlouvaId, string prilohaUniqueHash, string prilohaOdkaz, bool local = true, UriKind uriKind = UriKind.RelativeOrAbsolute, bool pdfFormat = false)
            {
                if (local)
                {
                    switch (uriKind)
                    {
                        case UriKind.RelativeOrAbsolute:
                        case UriKind.Absolute:
                            return LocalCopyUrl(smlouvaId, prilohaUniqueHash, pdfFormat);
                        case UriKind.Relative:
                        default:
                            return LocalCopyUrl(smlouvaId, prilohaUniqueHash, pdfFormat).Replace("https://www.hlidacstatu.cz","");
                    }
                    
                }
                else
                    return prilohaOdkaz;
            }


            public string GetUrl(string smlouvaId, bool local = true, UriKind uriKind = UriKind.RelativeOrAbsolute, bool pdfFormat = false)
            {
                return GetUrl(smlouvaId, this.UniqueHash(), this.odkaz, local, uriKind, pdfFormat);
            }
            public string LocalCopyUrl(string smlouvaId, bool pdfFormat, string identityName = null, string secret = null)
            {
                return LocalCopyUrl(smlouvaId, this.UniqueHash(), pdfFormat, identityName, secret);
            }
            public static string LocalCopyUrl(string smlouvaId, string prilohaUniqueHash, bool pdfFormat, string identityName = null, string secret = null)
            {
                var url = "https://www.hlidacstatu.cz" + $"/KopiePrilohy/{smlouvaId}?hash={prilohaUniqueHash}";
                if (identityName != null)
                    url = url + $"&secret={LimitedAccessSecret(identityName, prilohaUniqueHash)}";
                else if (secret != null)
                    url = url + $"&secret={secret}";
                if (pdfFormat)
                    url = url + "&forcePDF=true";
                return url;
            }
            public string LimitedAccessSecret(string forEmail)
            {
                return LimitedAccessSecret(forEmail, this.UniqueHash());
            }
            public static string LimitedAccessSecret(string forEmail, string prilohaUniqueHash)
            {
                if (string.IsNullOrEmpty(forEmail))
                    throw new ArgumentNullException("forEmail");
                return Devmasters.Crypto.Hash.ComputeHashToHex(forEmail + prilohaUniqueHash + "__" + forEmail);
            }
        }

    }
}
