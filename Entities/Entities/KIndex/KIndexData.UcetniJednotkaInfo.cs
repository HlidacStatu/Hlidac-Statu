﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace HlidacStatu.Entities.KIndex
{
    public partial class KIndexData
    {
        public class UcetniJednotkaInfo
        {
            public static UcetniJednotkaInfo Load(string ico)
            {
                using (DbEntities db = new DbEntities())
                {
                    var i = db.UcetniJednotka.AsQueryable().Where(m => m.Ico == ico)
                        .ToArray()
                        .OrderByDescending(m => (m.EndDate ?? DateTime.MaxValue))
                        .ToArray()
                        .FirstOrDefault();

                    if (i == null)
                        return null;

                    UcetniJednotkaInfo uji = new UcetniJednotkaInfo();
                    uji.Cofog = COFOG.Parse(i.CofogId.ToString());
                    uji.DruhUcetniJednotky = i.DruhujId ?? 0;
                    uji.PodDruhUcetniJednotky = i.PoddruhujId ?? 0;
                    uji.FormaUcetniJednotky = i.FormaId ?? 0;
                    uji.InstitucionalniSektor = i.IsektorId ?? 0;
                    uji.PocetObyvatelObce = i.Pocob ?? 0;
                    uji.LastUpdated = DateTime.Now;
                    return uji;

                }
            }


            //https://monitor.statnipokladna.cz/datovy-katalog/ciselniky/prohlizec/9
            public int DruhUcetniJednotky { get; set; }
            public string DruhUcetniJednotkyPopis()
            {
                return GetPopis("druhuj", DruhUcetniJednotky.ToString());
            }

            //https://monitor.statnipokladna.cz/datovy-katalog/ciselniky/prohlizec/24

            public int PodDruhUcetniJednotky { get; set; }

            //https://monitor.statnipokladna.cz/datovy-katalog/ciselniky/prohlizec/12
            public int FormaUcetniJednotky { get; set; }
            public string FormaUcetniJednotkyPopis()
            {
                return GetPopis("forma", FormaUcetniJednotky.ToString());
            }

            //https://monitor.statnipokladna.cz/datovy-katalog/ciselniky/prohlizec/17
            public int InstitucionalniSektor { get; set; }
            public string InstitucionalniSektorPopis()
            {
                return GetPopis("isektor", InstitucionalniSektor.ToString());
            }

            //KLASIFIKACE FUNKCÍ VLÁDNÍCH INSTITUCÍ
            //https://monitor.statnipokladna.cz/datovy-katalog/ciselniky/prohlizec/15
            //https://www.czso.cz/csu/czso/klasifikace_funkci_vladnich_instituci_-cz_cofog-
            public COFOG Cofog { get; set; }

            public int PocetObyvatelObce { get; set; } = 0;


            [Nest.Date]
            public DateTime LastUpdated { get; set; }

            static private object lockObj = new object();
            static Dictionary<string, Dictionary<string, string>> _popisy = new Dictionary<string, Dictionary<string, string>>();
            static private string GetPopis(string prefix, string value)
            {
                if (_popisy.ContainsKey(prefix) == false)
                {
                    lock (lockObj)
                    {
                        if (_popisy.ContainsKey(prefix) == false)
                        {

                            var val = GetCiselnik(prefix);
                            if (val != null)
                                _popisy.Add(prefix, val);
                        }
                    }
                }
                if (_popisy.ContainsKey(prefix)  && _popisy[prefix].ContainsKey(value))
                    return _popisy[prefix][value];
                else
                    return value;
            }


            private static Dictionary<string, string> GetCiselnik(string propertyPrefix)
            {
                string url = $"https://monitor.statnipokladna.cz/data/xml/{propertyPrefix}.xml";
                try
                {
                    using (Devmasters.Net.HttpClient.URLContent net = new Devmasters.Net.HttpClient.URLContent(url))
                    {
                        net.Timeout = 1000 * 180;
                        var d = net.GetContent();
                        XElement xe = XElement.Parse(d.Text);

                        return xe.Elements()
                            .Select(m => new
                            {
                                k = m.Element($"{propertyPrefix}_id").Value,
                                v = m.Element($"{propertyPrefix}_nazev").Value
                            })
                            .ToDictionary(m => m.k, m => m.v);

                    }

                }
                catch
                {
                    return null;
                }

            }
        }

    }

}

