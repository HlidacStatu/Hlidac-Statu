﻿using Devmasters.Collections;

using HlidacStatu.Entities;
using HlidacStatu.Entities.Enhancers;
using HlidacStatu.Repositories;

using System.Linq;

namespace HlidacStatu.Plugin.Enhancers
{


    public class AddMissingData : IEnhancer
    {

        public int Priority => 10;

        public string Description
        {
            get
            {
                return "Add missing data";
            }
        }

        public string Name
        {
            get
            {
                return "AddMissingData";
            }
        }
        public void SetInstanceData(object data) { }


        public bool Update(ref Smlouva item)
        {
            //return; //DOTO
            //check missing DS/ICO
            bool changed = false;

            changed = changed | UpdateSubj(item.Platce, item, "platce");
            changed = changed | UpdateSubj(item.VkladatelDoRejstriku, item, "platce");
            for (int i = 0; i < item.Prijemce.Count(); i++)
            {
                changed = changed | UpdateSubj(item.Prijemce[i], item, $"platce[{i}]");
            }
            return changed;
        }

        public bool UpdateSubj(Smlouva.Subjekt subj, Smlouva _item, string path)
        {

            bool changed = false;
            var zahr = new Util.DataValidators.ZahranicniAdresa(subj.adresa);
            if (!string.IsNullOrEmpty(zahr.Country) && !string.IsNullOrEmpty(subj.ico))
            {
                var currPref = Devmasters.RegexUtil.GetRegexGroupValue(subj.ico, @"^(?<pref>\w{2}-).{1,}", "pref");
                if (string.IsNullOrEmpty(currPref))
                {
                    //NENI PREFIX, DOPLN HO
                    string newico = zahr.Country + "-" + subj.ico
                        .Replace("HlidacStatu.Util.DataValidators+ZahranicniAdresa-", "")
                        .Replace("HlidacStatu.Util.DataValidators+ZahranicniAdresa", "")
                        ;
                    _item.Enhancements = _item.Enhancements.AddOrUpdate(new Enhancement("Doplněno zahraniční ID subjektu. Doplněn prefix před ID firmy", "", path + ".ico", newico, subj.ico, this));
                    subj.ico = newico;
                    changed = true;
                }
                else if (currPref != zahr.Country)
                {
                    //je jiny PREFIX, uprav ho
                    string newico = zahr.Country + subj.ico.Substring(2)
                        .Replace("HlidacStatu.Util.DataValidators+ZahranicniAdresa-","")
                        .Replace("HlidacStatu.Util.DataValidators+ZahranicniAdresa", "")
                        ;
                    _item.Enhancements = _item.Enhancements.AddOrUpdate(new Enhancement("Upraveno zahraniční ID subjektu. Doplněn prefix před ID firmy", "", path + ".ico", zahr + "-" + subj.ico, subj.ico, this));
                    subj.ico = newico;
                    changed = true;
                }
                return changed;
            }
            else
            {
                var currPref2 = Devmasters.RegexUtil.GetRegexGroupValue(subj.ico, @"^(?<pref>\w{2}-).{1,}", "pref");
                if (!string.IsNullOrEmpty(currPref2) && subj.ico != null)
                {
                    subj.ico = subj.ico.Replace(currPref2, "");
                    changed = true;
                }
            }
            //check formal valid ICO
            string ico = subj.ico;
            if (!string.IsNullOrEmpty(ico)
                && !Devmasters.TextUtil.IsNumeric(ico)
                && zahr.IsZahranicniAdresa() == false
                )
            {
                //neco spatne v ICO
                ico = System.Text.RegularExpressions.Regex.Replace(ico.ToUpper(), @"[^0-9\-.,]", string.Empty);
                if (Util.DataValidators.CheckCZICO(ico))
                {
                    _item.Enhancements = _item.Enhancements.AddOrUpdate(new Enhancement("Opraveno IČO subjektu", "", path + ".ico", subj.ico, ico, this));
                    subj.ico = ico;
                    changed = true;
                }
            }

            if (string.IsNullOrEmpty(subj.ico)
                && !string.IsNullOrEmpty(subj.datovaSchranka)
                && zahr.IsZahranicniAdresa() == false
                )
            {
                Firma f = FirmaRepo.FromDS(subj.datovaSchranka, true);
                if (Firma.IsValid(f))
                {
                    subj.ico = f.ICO;
                    _item.Enhancements = _item.Enhancements.AddOrUpdate(new Enhancement("Doplněno IČO subjektu", "", path + ".ico", "", f.ICO, this));
                    changed = true;
                }
            }
            else if (!string.IsNullOrEmpty(subj.ico) && string.IsNullOrEmpty(subj.datovaSchranka))
            {
                Firma f = FirmaRepo.FromIco(subj.ico, false);
                if (Firma.IsValid(f) && f.DatovaSchranka != null && f.DatovaSchranka.Length > 0)
                {
                    subj.datovaSchranka = f.DatovaSchranka[0];
                    _item.Enhancements = _item.Enhancements.AddOrUpdate(new Enhancement("Doplněna datová schránka subjektu", "", path + ".datovaScranka", "", f.DatovaSchranka[0], this));
                    changed = true;
                }
            }
            else if (string.IsNullOrEmpty(subj.ico)
                        && string.IsNullOrEmpty(subj.datovaSchranka)
                        && !string.IsNullOrEmpty(subj.nazev)
                        && zahr.IsZahranicniAdresa() == false
                    )
            {
                //based on name
                //simple compare now
                if (Firma.Koncovky.Any(m => subj.nazev.Contains(m)))
                {
                    Firma f = FirmaRepo.FromName(subj.nazev, true);
                    if (Firma.IsValid(f))
                    {
                        subj.ico = f.ICO;
                        subj.datovaSchranka = f.DatovaSchranka.Length > 0 ? f.DatovaSchranka[0] : "";
                        _item.Enhancements = _item.Enhancements.AddOrUpdate(new Enhancement("Doplněno IČO subjektu", "", path + ".ico", "", f.ICO, this));
                        if (f.DatovaSchranka.Length > 0)
                            _item.Enhancements = _item.Enhancements.AddOrUpdate(new Enhancement("Doplněna datová schránka subjektu", "", path + ".datovaSchranka", "", f.DatovaSchranka[0], this));
                        changed = true;
                    }
                    else
                    {
                        //malinko uprav nazev, zrus koncovku  aposledni carku
                        string modifNazev = Firma.JmenoBezKoncovky(subj.nazev) + "%";


                        f = FirmaRepo.FromName(modifNazev,false, true);
                        if (Firma.IsValid(f))
                        {
                            subj.ico = f.ICO;
                            subj.datovaSchranka = f.DatovaSchranka.Length > 0 ? f.DatovaSchranka[0] : "";
                            _item.Enhancements = _item.Enhancements.AddOrUpdate(new Enhancement("Doplněno IČO subjektu", "", path + ".ico", "", f.ICO, this));
                            if (f.DatovaSchranka.Length > 0)
                                _item.Enhancements = _item.Enhancements.AddOrUpdate(new Enhancement("Doplněna datová schránka subjektu", "", path + ".datovaSchranka", "", f.DatovaSchranka[0], this));
                            changed = true;

                        }
                    }
                }
            }
            if (string.IsNullOrEmpty(subj.nazev) && !string.IsNullOrEmpty(subj.ico))
            {
                //dopln chybejici jmeno 
                Firma f = FirmaRepo.FromIcoExt(subj.ico, true);
                if (Firma.IsValid(f))
                {
                    subj.nazev = f.Jmeno;
                    _item.Enhancements = _item.Enhancements.AddOrUpdate(new Enhancement("Doplněn Název subjektu", "", path + ".nazev", "", f.ICO, this));
                    changed = true;
                }
            }
            if (string.IsNullOrEmpty(subj.adresa) && !string.IsNullOrEmpty(subj.ico))
            {
                //dopln chybejici jmeno 
                var fm = FirmaRepo.Merk.FromIcoFull(subj.ico);
                if (fm != null)
                {
                    subj.adresa = fm.address.street + " " + fm.address.number + ", " + fm.address.municipality;
                    _item.Enhancements = _item.Enhancements.AddOrUpdate(new Enhancement("Doplněna adresa subjektu", "", path + ".nazev", "", subj.ico, this));
                    changed = true;
                }
            }

            return changed;
        }



    }
}

