using System;

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
                public decimal BlurredAreaPerc { get; set; }
                public int NumOfBlurredPages { get; set; }
                public int NumOfExtensivelyBlurredPages { get; set; }
                public int[] ListOfExtensivelyBlurredPages { get; set; }
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

            public void UpdateStatistics()
            {
                Lenght = PlainTextContent?.Length ?? 0;
                WordCount = Devmasters.TextUtil.CountWords(PlainTextContent);
                var variance = Devmasters.TextUtil.WordsVarianceInText(PlainTextContent);
                UniqueWordsCount = variance.Item2;
                WordsVariance = variance.Item1;

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

            public static string GetUrl(string smlouvaId, string prilohaUniqueHash, string prilohaOdkaz, bool local = true, UriKind uriKind = UriKind.RelativeOrAbsolute)
            {
                if (local)
                {
                    switch (uriKind)
                    {
                        case UriKind.RelativeOrAbsolute:
                        case UriKind.Absolute:
                            return "https://www.hlidacstatu.cz"+LocalCopyPath(smlouvaId, prilohaUniqueHash);
                            break;
                        case UriKind.Relative:
                        default:
                            return LocalCopyPath(smlouvaId, prilohaUniqueHash);
                    }
                    
                }
                else
                    return prilohaOdkaz;
            }


            public string GetUrl(string smlouvaId, bool local = true, UriKind uriKind = UriKind.RelativeOrAbsolute)
            {
                return GetUrl(smlouvaId, this.UniqueHash(), this.odkaz, local, uriKind);
            }
            public string LocalCopyPath(string smlouvaId, string identityName = null, string secret = null)
            {
                return LocalCopyPath(smlouvaId, this.UniqueHash(), identityName, secret);
            }
            public static string LocalCopyPath(string smlouvaId, string prilohaUniqueHash, string identityName = null, string secret = null)
            {
                var url = $"/KopiePrilohy/{smlouvaId}?hash={prilohaUniqueHash}";
                if (identityName != null)
                    url = url + $"&secret={LimitedAccessSecret(identityName, prilohaUniqueHash)}";
                else if (secret != null)
                    url = url + $"&secret={secret}";

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
