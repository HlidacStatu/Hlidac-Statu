using HlidacStatu.Util;
using System;
using System.Linq;
using Devmasters.Enums;
using HlidacStatu.Util.Cache;

namespace HlidacStatu.Entities
{
    public partial class Firma
    {
        public class Zatrideni
        {
            public class Item
            {
                public string Ico { get; set; }
                public string Jmeno { get; set; }
                public string KrajId { get; set; } = "";
                public string Kraj { get; set; } = "";
                public string Group { get; set; } = "";
            }


            

            [Groupable]
            [ShowNiceDisplayName()]
            [Sortable(SortableAttribute.SortAlgorithm.BySortValueAndThenAlphabetically)]
            public enum StatniOrganizaceObor
            {
                [Disabled]
                Ostatni = 0,

                [Disabled]
                [GroupValue("Zdravotnictví")]
                [NiceDisplayName("Zdravotní ústavy")]
                Zdravotni_ustavy = 158,
                [GroupValue("Zdravotnictví")]
                [NiceDisplayName("Zdravotní pojišťovny")]
                Zdravotni_pojistovny = 186,
                [GroupValue("Zdravotnictví")]
                [NiceDisplayName("Nemocnice")]
                Nemocnice = 10001,
                [GroupValue("Zdravotnictví")]
                [NiceDisplayName("Velké nemocnice v ČR")]
                Velke_nemocnice = 10002,
                [GroupValue("Zdravotnictví")]
                [NiceDisplayName("Fakultní nemocnice")]
                Fakultni_nemocnice = 10003,

                [Disabled]
                [GroupValue("Justice")]
                [NiceDisplayName("Krajská státní zastupitelství")]
                Krajska_statni_zastupitelstvi = 143,
                [GroupValue("Justice")]
                [NiceDisplayName("Krajské soudy")]
                Krajske_soudy = 107,
                [Disabled]
                [GroupValue("Justice")]
                [NiceDisplayName("Všechny soudy")]
                Soudy = 123,

                [GroupValue("Samospráva")]
                [NiceDisplayName("Kraje a hl. m. Praha")]
                Kraje_Praha = 12,
                [GroupValue("Samospráva")]
                [NiceDisplayName("Obce s rozšířenou působností")]
                Obce_III_stupne = 11,
                [Disabled()]
                [GroupValue("Samospráva")]
                [NiceDisplayName("Obce")]
                Obce = 14,
                [GroupValue("Samospráva")]
                [NiceDisplayName("Statutární města")]
                Statutarni_mesta = 103,
                [GroupValue("Samospráva")]
                [NiceDisplayName("Městské části Prahy")]
                Mestske_casti_Prahy = 600,


                [GroupValue("Státní úřady a organizace")]
                [NiceDisplayName("Hasičscké záchranné sbory")]
                Hasicsky_zachranny_sbor = 135,
                [Disabled]
                [GroupValue("Státní úřady a organizace")]
                [NiceDisplayName("Krajské hygienické stanice")]
                Krajske_hygienicke_stanice = 113,
                [GroupValue("Státní úřady a organizace")]
                [NiceDisplayName("Krajská ředitelství policie")]
                Krajska_reditelstvi_policie = 145,
                [GroupValue("Státní úřady a organizace")]
                [NiceDisplayName("Státní fondy")]
                Statni_fondy = 980,
                [Disabled]
                [GroupValue("Státní úřady a organizace")]
                [NiceDisplayName("Okresní správy sociálního zabezpečení")]
                OSSZ = 128,
                [Disabled]
                [GroupValue("Státní úřady a organizace")]
                [NiceDisplayName("Katastrální úřady")]
                Katastralni_urady = 127,
                [GroupValue("Státní úřady a organizace")]
                [NiceDisplayName("Ministerstva")]
                Ministerstva = 2926,
                [GroupValue("Státní úřady a organizace")]
                [NiceDisplayName("Organizační složky státu")]
                Organizacni_slozky_statu = 191,

                [GroupValue("Státní úřady a organizace")]
                [NiceDisplayName("Všechny ústřední orgány státní správy")]
                Vsechny_ustredni_organy_statni_spravy = 10104,

                [GroupValue("Státní úřady a organizace")]
                [NiceDisplayName("Média")]
                Media = 10105,

                [Disabled()]
                [GroupValue("Státní úřady a organizace")]
                [NiceDisplayName("Další ústřední orgány státní správy")]
                Dalsi_ustredni_organy_statni_spravy = 104,
                [Disabled]
                [GroupValue("Státní úřady a organizace")]
                [NiceDisplayName("Celní úřady")]
                Celni_urady = 105,
                [Disabled]
                [GroupValue("Státní úřady a organizace")]
                [NiceDisplayName("Finanční úřady")]
                Financni_urady = 109,
                [Disabled]
                [GroupValue("Státní úřady a organizace")]
                [NiceDisplayName("Výjimky z registru smluv")]
                Vyjimky_RS = 10100,

                [GroupValue("Státní úřady a organizace")]
                [NiceDisplayName("Vybrané státní podniky")]
                Statni_podniky = 10101,
                [GroupValue("Státní úřady a organizace")]
                [NiceDisplayName("Agentury")]
                Agentury = 10102,


                [GroupValue("Školství")]
                [NiceDisplayName("Veřejné vysoké školy")]
                Verejne_vysoke_skoly = 1129,

                [GroupValue("Školství")]
                [NiceDisplayName("Základní a střední školy")]
                Zakladni_a_stredni_skoly = 1128,

                [Disabled]
                [GroupValue("Školství")]
                [NiceDisplayName("Konzervatoře")]
                Konzervatore = 1120,

                [GroupValue("Služby")]
                [NiceDisplayName("Krajské správy silnic")]
                Krajske_spravy_silnic = 10004,
                [GroupValue("Služby")]
                [NiceDisplayName("Dopravní podniky měst")]
                Dopravni_podniky = 10005,
                [GroupValue("Služby")]
                [NiceDisplayName("Technické služby")]
                Technicke_sluzby = 10006,

                //Sportovni_zarizeni = 10007,

                [Disabled]
                [GroupValue("Služby")]
                [NiceDisplayName("Domovy důchodců")]
                Domov_duchodcu = 10008,

                [GroupValue("Věda a výzkum")]
                [NiceDisplayName("Akademické instituce")]
                Akademicke_instituce = 11001,

                [GroupValue("Kultura")]
                [NiceDisplayName("Knihovny")]
                Knihovny = 12001,
                [GroupValue("Kultura")]
                [NiceDisplayName("Muzea a galerie")]
                Muzea_a_galerie = 12002,
                [GroupValue("Kultura")]
                [NiceDisplayName("Kulturní a kongresová centra")]
                Kulturni_a_kongresova_centra = 12003,
                [GroupValue("Kultura")]
                [NiceDisplayName("Divadla")]
                Divadla = 12004,
                [GroupValue("Kultura")]
                [NiceDisplayName("ZOO")]
                ZOO = 12005,



                //Veznice = 10009,
                //Pamatky = 10010, //narodni pamatkovy ustav
                //Zachranne_sluzby = 10011,

                //spolovny, prehrady, zasobarny pitne vody
                // vodarny
                // kulturni zarizeni, divadla, kina, strediska
                // spravy povodi
                // ZOO
                // hrbitovy, krematoria
                // lazne, blazince
                // regionalni rozvojove agentury, vinarstvi, ...
                // domy deti a mladeze


                [Disabled]
                OVM_pro_evidenci_skutecnych_majitelu = 3106,

                [Disabled]
                Vse = -1,
            }

            /*
             Nemocnice:
             select distinct f.ICO, f.Jmeno from firma f inner join Firma_NACE fn on f.ICO = fn.ICO where nace like '86%' and f.IsInRS = 1

             */

            


        }
    }
}
