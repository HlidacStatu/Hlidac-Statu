using Devmasters;
using HlidacStatu.Entities;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using HlidacStatu.Repositories.Cache;

namespace HlidacStatu.Repositories
{
    public static partial class FirmaRepo
    {
        public class RZP
        {
            public static Firma FromIco(string ico)
            {
                var resp = HlidacStatu.Connectors.External.RZP.Manager.RawSearchIco(ico);
                if (resp == null)
                    return null;

                if (resp.Items.Any(m => m.GetType() == typeof(Connectors.External.RZP.TPodnikatelSeznam)))
                {
                    HlidacStatu.Connectors.External.RZP.TPodnikatelSeznam xf = resp.Items
                        .First(m => m.GetType() == typeof(Connectors.External.RZP.TPodnikatelSeznam)) as HlidacStatu.Connectors.External.RZP.TPodnikatelSeznam;

                    Firma f = new Firma();
                    f.Jmeno = xf.ObchodniJmenoSeznam.Value;
                    f.ICO = ico;

                    var detail = HlidacStatu.Connectors.External.RZP.Manager.RawSearchDetail(xf.PodnikatelID);
                    if (detail == null)
                    {
                        f.Source = "RZP";
                        f.Popis = "Živnostník";
                        return f;
                    }

                    HlidacStatu.Connectors.External.RZP.TPodnikatelVypis vypis = detail.Items
                        .First(m => m.GetType() == typeof(Connectors.External.RZP.TPodnikatelVypis)) as HlidacStatu.Connectors.External.RZP.TPodnikatelVypis;

                    if (vypis.PodnikatelDetail?.SeznamZivnosti?.Zivnost?.Count() > 0)
                    {
                        f.Datum_Zapisu_OR = vypis.PodnikatelDetail.SeznamZivnosti.Zivnost
                            .Where(m => !string.IsNullOrEmpty(m.Vznik.Value))
                            .Select(m => DateTime.ParseExact(m.Vznik.Value, "dd.MM.yyyy", Util.Consts.czCulture))
                            .Min();

                        f.Source = "RZP";
                        f.Popis = "Živnostník";
                    }

                    f.RefreshDS();
                    FirmaRepo.SaveAsync(f);
                    return f;
                }
                else
                    return null;
            }
        }

        //todo: Upravit + přidat státní firma/organizace in text.
        public static Firma FirmaInText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return null;

            string value = TextUtil.RemoveDiacritics(TextUtil.NormalizeToBlockText(text).ToLower());

            foreach (var k in Firma.Koncovky.Select(m => m.ToLower()).OrderByDescending(m => m.Length))
            {
                if (value.Contains(k))
                {
                    value = value.Replace(k,
                        k.Replace(' ',
                            (char)160)); //nahrad mezery char(160) - non breaking space, aby to tvorilo 1 slovo
                }
                else if (k.EndsWith(".") && value.EndsWith(k.Substring(0, k.Length - 1)))
                {
                    value = value.Replace(k.Substring(0, k.Length - 1),
                        k.Replace(' ',
                            (char)160)); //nahrad mezery char(160) - non breaking space, aby to tvorilo 1 slovo
                }
            }

            //find company name
            string[] words = value.Split(new char[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries);

            //get back space instead of #160
            words = words.Select(m => m.Replace((char)160, ' ')).ToArray();

            for (int firstWord = 0; firstWord < words.Length; firstWord++)
            {
                for (int skipWord = 0; skipWord < words.Length - firstWord; skipWord++)
                {
                    string[] cutWords = words.Skip(firstWord) //preskoc slovo na zacatku
                        .Reverse().Skip(skipWord).Reverse() // a ubirej od konce
                        .ToArray();
                    string wordCombination = string.Join(" ", cutWords);

                    string firmaBezKoncovky = Firma.JmenoBezKoncovkyAsciiFull(wordCombination, out var koncovkaFirmy);
                    string simpleName = TextUtil.RemoveDiacritics(firmaBezKoncovky).ToLower().Trim();
                    //+ "|" + koncovka;


                    if (firmaBezKoncovky.Length > 3
                        && FirmaCache.FirmyNazvyOnlyAscii().ContainsKey(simpleName)
                    )
                    {
                        //nasel jsem ico?
                        foreach (var ico in FirmaCache.FirmyNazvyOnlyAscii()[simpleName])
                        {
                            Firma f = Firmy.Get(ico); //TODO StaticData.FirmyNazvyAscii.Get()[simpleName]);
                            if (f?.Valid == true)
                            {
                                var firmaFromText = TextUtil.ReplaceDuplicates(
                                    Regex.Replace(wordCombination, @"[,;_""']", " ", Validators.DefaultRegexOptions),
                                    ' ').ToLower();
                                var firmaFromDB = TextUtil.ReplaceDuplicates(
                                    Regex.Replace(f.JmenoBezKoncovky(), @"[,;_""']", " ", Validators.DefaultRegexOptions), ' ');
                                firmaFromDB = TextUtil.RemoveDiacritics(firmaFromDB).ToLower();
                                var rozdil = HlidacStatu.Util.TextTools.LevenshteinDistanceCompute(
                                    firmaFromDB,
                                    firmaFromText
                                );
                                var fKoncovka = f.KoncovkaFirmy();
                                var nextWord = "";
                                if (firstWord + cutWords.Length < words.Length - 1)
                                    nextWord = words[firstWord + cutWords.Length];

                                if (rozdil == 0 && string.IsNullOrEmpty(koncovkaFirmy))
                                    return f;
                                if (string.IsNullOrEmpty(fKoncovka))
                                    return f;
                                if (!string.IsNullOrEmpty(fKoncovka) &&
                                    HlidacStatu.Util.TextTools.LevenshteinDistanceCompute(cutWords.Last(), fKoncovka) < 2)
                                    return f;
                                if (!string.IsNullOrEmpty(fKoncovka) &&
                                    HlidacStatu.Util.TextTools.LevenshteinDistanceCompute(nextWord, fKoncovka) < 2)
                                    return f;
                            }
                        }
                        //looking for next
                        //return null;
                    }
                }
            }

            return null;
        }
    }
}