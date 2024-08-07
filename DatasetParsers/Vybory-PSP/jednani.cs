﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Vybory_PSP
{
    public class jednani : HlidacStatu.Api.Dataset.Connector.IDatasetItem
    {
        public class dokument
        {
            public string HsProcessType { get; } = "documentsave";
            public string DocumentUrl { get; set; }
            public string DocumentPlainText { get; set; }
            public string jmeno { get; set; }
            public string popis { get; set; }
            public string typ { get; set; }
        }
        public class mp3
        {
            public string HsProcessType { get; } = "audiosave";
            public string DocumentUrl { get; set; }
            public string DocumentPlainText { get; set; }
            public string jmeno { get; set; }

            public blok[] prepisAudia { get; set; }

            public class blok
            {
                public long sekundOdZacatku { get; set; }
                public string text { get; set; }
            }

        }

        public void SetId()
        {
            var s = $"{vyborId}-{cisloJednani}-{datum:yyyyMMdd}";

            _id = s;
        }

        private string _id = null;
        public string Id
        {
            get { return _id; }
            set { _id = value; }
        }
        public DateTime datum { get; set; }
        public int cisloJednani { get; set; }
        public string vec { get; set; }

        public string vybor { get; set; }
        public int vyborId { get; set; }
        public string vyborUrl { get; set; }

        public string zapisJednani { get; set; }

        public dokument[] dokumenty { get; set; }
        public mp3[] audio { get; set; }


        public static jednani Merge(jednani prim, jednani sec, out bool changed)
        {
            Console.WriteLine($"Merging {prim.cisloJednani} with {sec.cisloJednani}");
            changed = false;
            if (!string.IsNullOrEmpty(sec.vec) && prim.vec != sec.vec)
            {
                prim.vec = sec.vec;
                changed = true;
            }

            //merge
            if (sec.audio?.Length != prim.audio?.Length)
            {
                if (sec.audio == null)
                {
                    sec.audio = prim.audio;
                }
                else
                {
                    List<mp3> docs = new List<mp3>(prim.audio ?? new mp3[] { });
                    foreach (var a in sec.audio ?? new mp3[] { })
                    {
                        if (!docs.Any(m => m.DocumentUrl == a.DocumentUrl))
                        {
                            docs.Add(a);
                            changed = true;
                        }
                    }
                    for (int i = 0; i < docs.Count(); i++)
                    {

                        foreach (var a in sec.audio ?? new mp3[] { })
                        {
                            if (docs[i].DocumentUrl == a.DocumentUrl && docs[i].prepisAudia == null)
                            {
                                docs[i].prepisAudia = a.prepisAudia;
                                changed = true;
                            }
                        }

                    }
                    prim.audio = docs.ToArray();
                }
            }
            if (sec.dokumenty?.Length != prim.dokumenty?.Length)
            {
                if (sec.dokumenty == null)
                {
                    sec.dokumenty = prim.dokumenty;
                }
                else
                {
                    List<dokument> docs = new List<dokument>(prim.dokumenty ?? new dokument[] { });
                    foreach (var a in sec.dokumenty ?? new dokument[] { })
                    {
                        if (!docs.Any(m => m.DocumentUrl == a.DocumentUrl))
                        {
                            docs.Add(a);
                            changed = true;
                        }
                    }
                    prim.dokumenty = docs.ToArray();
                }
            }

            return prim;
        }
    }
}
