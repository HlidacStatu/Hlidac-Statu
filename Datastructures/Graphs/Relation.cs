using Devmasters.Enums;

using Force.DeepCloner;
using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HlidacStatu.DS.Graphs
{
    public class Relation
    {

        public static TimeSpan NedavnyVztahDelka = TimeSpan.FromDays((365 * 5) + 2); //5 let

        public enum CharakterVazbyEnum
        {
            VlastnictviKontrola = 0,
            Uredni = 1,

        }

        public static int[] CharakterVazby_UredniVazbyIds = new int[] { 
            (int)RelationEnum.Likvidator,
            (int)RelationEnum.Likvidator_v_zastoupeni,
            (int)RelationEnum.Spravce_konkursu,
            (int)RelationEnum.Oddeleny_insolvencni_spravce,
            (int)RelationEnum.Predbezny_insolvencni_spravce,
            (int)RelationEnum.Predbezny_spravce,
            (int)RelationEnum.Insolvencni_spravce,
            (int)RelationEnum.Zvlastni_insolvencni_spravce,
            (int)RelationEnum.Zvlastni_spravce,
        };

        [ShowNiceDisplayName()]
        public enum RelationEnum
        {
            [NiceDisplayName("Zřizovatel příspěvkové organizace")]
            ZrizovatelPO = -10,
            [NiceDisplayName("Osobní vztah")]
            OsobniVztah = -3,
            [NiceDisplayName("Vliv")]
            Vliv = -2,
            [NiceDisplayName("Kontrola")]
            Kontrola = -1,

            [NiceDisplayName("Podnikatel z OR")]
            Podnikatel_z_OR = 0,
            [NiceDisplayName("Člen statutárního orgánu")]
            Clen_statutarniho_organu = 1,
            [NiceDisplayName("Likvidátor")]
            Likvidator = 2,
            [NiceDisplayName("Prokurista")]
            Prokurista = 3,
            [NiceDisplayName("Člen dozorčí rady")]
            Clen_dozorci_rady = 4,
            [NiceDisplayName("Jediný akcionář")]
            Jediny_akcionar = 5,
            [NiceDisplayName("Člen družstva s vkladem")]
            Clen_druzstva_s_vkladem = 6,

            // PATCHED by JSON
            [NiceDisplayName("Společník bez vkladu")]
            Clen_dozorci_rady_v_zastoupeni = 7,
            [NiceDisplayName("Společník s vkladem")]
            Clen_kontrolni_komise_v_zastoupeni = 8,

            [NiceDisplayName("Komplementář")]
            Komplementar = 9,
            [NiceDisplayName("Komanditista")]
            Komanditista = 10,
            [NiceDisplayName("Správce konkursu")]
            Spravce_konkursu = 11,

            // PATCHED by JSON
            [NiceDisplayName("Zástupce správce konkursu")]
            Likvidator_v_zastoupeni = 12,
            [NiceDisplayName("Zakladatel státního podniku")]
            Oddeleny_insolvencni_spravce = 13,
            [NiceDisplayName("Zakladatel o.p.s.")]
            Pobocny_spolek = 14,
            [NiceDisplayName("Zřizovatel odštěpného závodu")]
            Podnikatel = 15,

            [NiceDisplayName("Předběžný insolvenční správce")]
            Predbezny_insolvencni_spravce = 16,

            // PATCHED by JSON
            [NiceDisplayName("Předběžný insolvenční správce")]
            Predbezny_spravce = 17,

            [NiceDisplayName("Představenstvo")]
            Predstavenstvo = 18,
            [NiceDisplayName("Podílník")]
            Podilnik = 19,
            [NiceDisplayName("Revizor")]
            Revizor = 20,

            // PATCHED by JSON
            [NiceDisplayName("Zřizovatel nadace")]
            Revizor_v_zastoupeni = 21,
            [NiceDisplayName("Statutár - vedoucí odštěpného závodu")]
            Clen_rozhodci_komise = 22,

            [NiceDisplayName("Vedoucí odštěpného závodu")]
            Vedouci_odstepneho_zavodu = 23,

            // PATCHED by JSON
            [NiceDisplayName("Společník s vkladem")]
            Spolecnik = 24,
            [NiceDisplayName("Předběžný správce konkursní podstaty")]
            Clen_spravni_rady_v_zastoupeni = 25,
            [NiceDisplayName("Prokurista")]
            Clen_statutarniho_organu_zrizovatele = 26,

            // ADDED (missing in enum, from JSON)
            [NiceDisplayName("Člen statutárního orgánu společnosti")]
            Clen_statutarniho_organu_spolecnosti = 27,

            // PATCHED by JSON
            [NiceDisplayName("Člen statutárního orgánu komplementářů")]
            Clen_statutarniho_organu_v_zastoupeni = 28,
            [NiceDisplayName("Člen statutárního orgánu představenstva")]
            Insolvencni_spravce_vyrovnavaci = 29,

            // ADDED (missing in enum, from JSON)
            [NiceDisplayName("Člen představenstva")]
            Clen_predstavenstva = 30,

            [NiceDisplayName("Člen správní rady")]
            Clen_spravni_rady = 31,

            // PATCHED by JSON
            [NiceDisplayName("Oprávněná osoba nadace")]
            Statutarni_organ_zrizovatele_v_zastoupeni = 32,
            [NiceDisplayName("Oprávněná osoba nadačního fondu")]
            Zakladatel = 33,

            [NiceDisplayName("Nástupce zřizovatele")]
            Nastupce_zrizovatele = 34,

            // PATCHED by JSON
            [NiceDisplayName("Zřizovatel příspěvkové organizace")]
            Zakladatel_s_vkladem = 35,

            [NiceDisplayName("Člen sdružení")]
            Clen_sdruzeni = 36,

            // PATCHED by JSON
            [NiceDisplayName("Člen statutárního orgánu zřizovatele - Z")]
            Zastupce_insolvencniho_spravce = 37,
            [NiceDisplayName("Člen kontrolní komise - Z")]
            Clen_kontrolni_komise = 38,

            [NiceDisplayName("Insolvenční správce")]
            Insolvencni_spravce = 39,

            // PATCHED by JSON
            [NiceDisplayName("Ředitel o.p.s.")]
            Zastupce_spravce = 40,

            [NiceDisplayName("Zvláštní insolvenční správce")]
            Zvlastni_insolvencni_spravce = 41,
            [NiceDisplayName("Zvláštní správce")]
            Zvlastni_spravce = 42,

            // NOTE: původní enum měl 400/401/402, ale JSON má 200/201/202.
            // Nechávám původní kvůli kompatibilitě + přidávám JSON verze.
            [NiceDisplayName("Podnikatel z RŽP")]
            Podnikatel_z_RZP = 400,
            [NiceDisplayName("Statutár")]
            Statutar = 401,
            [NiceDisplayName("Vedoucí org. složky")]
            Vedouci_org_slozky = 402,

            // ADDED (missing in enum, from JSON)
            [NiceDisplayName("Podnikatel z RŽP")]
            Podnikatel_z_RZP_200 = 200,
            [NiceDisplayName("Statutár")]
            Statutar_201 = 201,
            [NiceDisplayName("Vedoucí org. složky")]
            Vedouci_org_slozky_202 = 202,

            // ADDED (missing in enum, from JSON)
            [NiceDisplayName("Podnikatel z ISPOZ")]
            Podnikatel_z_ISPOZ = 1600,
            [NiceDisplayName("Člen statutárního orgánu")]
            Clen_statutarniho_organu_ISPOZ = 1601,
            [NiceDisplayName("Odpovědný zástupce")]
            Odpovedny_zastupce = 1602,
            [NiceDisplayName("Neznámý")]
            Neznamy = 1699,
        }

        [ShowNiceDisplayName()]
        public enum RelationSimpleEnum
        {
            [NiceDisplayName("Zřizovatel příspěvkové organizace")]
            ZrizovatelPO = -10,
            [NiceDisplayName("Osobní vztah")]
            OsobniVztah = -3,
            [NiceDisplayName("Vliv")]
            Vliv = -2,
            [NiceDisplayName("Kontrola")]
            Kontrola = -1,
            [NiceDisplayName("Neznámý")]
            Neznamy = 0,
            [NiceDisplayName("Společník")]
            Spolecnik = 1,
            [NiceDisplayName("Jednatel")]
            Jednatel = 2,
            [NiceDisplayName("Prokura")]
            Prokura = 3,
            [NiceDisplayName("Člen dozorčí rady")]
            Dozorci_rada = 4,
            [NiceDisplayName("Člen statut. orgáni")]
            Statutarni_organ = 5,
            [NiceDisplayName("OSVČ")]
            OSVC = 6,
            [NiceDisplayName("Zakladatel")]
            Zakladatel = 7,
            [NiceDisplayName("Jiný")]
            Jiny = 99,
            [NiceDisplayName("Souhrnný")]
            Souhrnny = 100,
        }

        [ShowNiceDisplayName()]
        public enum RelationTypeEnum
        {
            [NiceDisplayName("Neznámý vztah vztah")]
            Neznamy = 0,
            [NiceDisplayName("Přímý vztah")]
            Primy = 1,
            [NiceDisplayName("Nepřímý vztah")]
            Neprimy = 2,
            [NiceDisplayName("Neoficiální vztah")]
            Neoficialni = 3,
            [NiceDisplayName("Osobní vztah")]
            Osobni = 4,
        }


        [Sortable(SortableAttribute.SortAlgorithm.BySortValue)]
        [ShowNiceDisplayName()]
        public enum AktualnostType
        {

            [NiceDisplayName("Aktuálně podle obch.rejstříku")]
            Aktualni = 2,
            [NiceDisplayName("Aktuálně podle obch.rejstříku či posledních 5 letech")]
            Nedavny = 1,
            [NiceDisplayName("Aktuálně podle obch.rejstříku či kdykoliv v minulosti")]
            Neaktualni = 0,
            [NiceDisplayName("")]
            Libovolny = -1,

        }


        public enum TiskEnum
        {
            Text,
            Html,
            Json,
            Checkbox
        }


        public static string ExportTabDataDebug(IEnumerable<Graph.Edge> data)
        {
            if (data == null)
                return "";
            if (data.Count() == 0)
                return "";

            StringBuilder sb = new StringBuilder(1024);

            foreach (var i in data)
            {
                sb.AppendFormat($"{i.From?.UniqId}\t{i.To?.UniqId}\t{i.Descr} {i.RelFrom?.ToShortDateString() ?? "Ø"} -> {i.RelTo?.ToShortDateString() ?? "Ø"}\t{i.Root}\n");
            }
            return sb.ToString();
        }

        public static HlidacStatu.DS.Graphs.Graph.Edge[] SkutecnaDobaVazby(Graph.Edge[] vazby)
        {
            //najdi konec vazby podle nadrizenych nodu
            for (int i = 0; i < vazby.Length; i++)
            {
                var v = vazby[i];
                if (v.Distance == 0)
                    continue; //root
                //najdi nadrizene vazby
                DateTime? maxDate =
                    vazby.Any(m => m.To.UniqId == v.From.UniqId && m.Distance == v.Distance - 1) ?
                        vazby.Where(m => m.To.UniqId == v.From.UniqId)
                            .Max(m => m.RelTo ?? DateTime.MaxValue)
                        : (DateTime?)null;
                //najdi nadrizene vazby
                DateTime? minDate =
                    vazby.Any(m => m.To.UniqId == v.From.UniqId && m.Distance == v.Distance - 1) ?
                        vazby.Where(m => m.To.UniqId == v.From.UniqId)
                            .Max(m => m.RelFrom ?? DateTime.MinValue)
                        : (DateTime?)null;

                if (maxDate.HasValue)
                {
                    if (v.RelTo.HasValue && v.RelTo > maxDate
                        || v.RelTo.HasValue == false
                        )
                    {
                        v.RelTo = (maxDate == DateTime.MaxValue ? null : maxDate);
                    }
                }
                if (minDate.HasValue)
                {
                    if (v.RelFrom.HasValue && v.RelFrom < minDate
                        || v.RelTo.HasValue == false
                        )
                    {
                        v.RelFrom = (minDate == DateTime.MinValue ? null : minDate);
                    }
                }
            }
            return vazby;
        }

        public static Graph.Edge[] AktualniVazby(IEnumerable<Graph.Edge> allRelations, AktualnostType minAktualnost, Graph.Edge root)
        {
            if (allRelations == null)
                return new Graph.Edge[] { };

            DateTime? from = null ;
            DateTime? to = DateTime.Now.Date.AddDays(1);

            switch (minAktualnost)
            {
                case AktualnostType.Aktualni:
                    from = DateTime.Now.Date.AddDays(-2);
                    break;
                case AktualnostType.Nedavny:
                    from = to - Relation.NedavnyVztahDelka;
                    break;
                case AktualnostType.Neaktualni:
                case AktualnostType.Libovolny:
                default:
                    return allRelations.ToArray();
            }


            //filter per distanceallRelations.First(m => m.Root == true)
            var filteredRels = _childrenVazby(root, 
                allRelations.Where(m=>!m.Root).DeepClone(), 
                new Graph.Edge[] { },
                from,to,
                0, allRelations.FirstOrDefault(m=>m.Root));

            //add root
            //var res = filteredRels.Prepend(root);

            return filteredRels
                .ToArray();
        }

        private static Graph.Edge[] _childrenVazby(Graph.Edge parent, IEnumerable<Graph.Edge> vazby, 
            IEnumerable<Graph.Edge> exclude, DateTime? from, DateTime? to, int callDeep, Graph.Edge originalRoot)
        {
            //AktualnostType akt = parent.Aktualnost;
            //if (minAktualnost >= parent.Aktualnost)

            //Console.WriteLine($"{callDeep} - {parent.UniqId}");

            List<Graph.Edge> items = new List<Graph.Edge>();

            if (callDeep > 200)
            {
                //primitive stackoverflow protection
                //_logger.Error("_childrenVazby stackoverflow protection {@parentFrom}-{parentTo} {@originalRootFrom}-{@originalRootTo}", 
                //    parent.From?.UniqId, parent.To?.UniqId, originalRoot.From?.UniqId, originalRoot.To?.UniqId);
                return items.ToArray();
            }

            var fVazby2a = vazby
        .Where(m =>
                parent.To.UniqId == m.From.UniqId);
            var fVazby2b = fVazby2a
                .Where(m=> Devmasters.DT.Util.IsOverlappingIntervals(from,to,m.RelFrom,m.RelTo));
            var fVazby2c = fVazby2b 
                .Where(m=> Devmasters.DT.Util.IsOverlappingIntervals(parent.RelFrom, parent.RelTo, m.RelFrom, m.RelTo))
                .ToArray();

            var fVazby = fVazby2c;
            //var fVazby = vazby
            //        .Where(m =>
            //                parent.To.UniqId == m.From.UniqId
            //                //&& m.Distance == parent.Distance + 1
            //                && m.Aktualnost >= parent.Aktualnost
            //                && m.Aktualnost >= minAktualnost
            //            )
            //        .ToArray();


            foreach (var ch in fVazby)
            {
                if (exclude.Any(m => m.HasSameEdges(ch) ))
                    continue;
                if (parent.HasSameEdges(ch) )
                    continue;

                //if (ch.Aktualnost > minAktualnost)
                //    ch.Aktualnost = minAktualnost;
                items.Add(ch);
                var chVazby = vazby;//.Where(m => ch.To.UniqId == m.From.UniqId && m.Distance == ch.Distance + 1).ToArray();
                items.AddRange(
                    _childrenVazby(ch, vazby, exclude.Concat(items), from,to, callDeep+1, originalRoot)
                    );
            }
            return items.ToArray();

        }


        /*
     * Přehled kódů angažmá:
(00-99 jsou angažmá z OR, 400-499 z RŽP)
 00 - Podnikatel z OR
 01 - Člen statutárního orgánu
 02 - Likvidátor
 03 - Prokurista
 04 - Člen dozorčí rady
 05 - Jediný akcionář
 06 - Člen družstva s vkladem
 07 - Člen dozorčí rady v zastoupení
 08 - Člen kontrolní komise v zastoupení
 09 - Komplementář
 10 - Komanditista
 11 - Správce konkursu
 12 - Likvidátor v zastoupení
 13 - Oddělený insolvenční správce
 14 - Pobočný spolek
 15 - Podnikatel
 16 - Předběžný insolvenční správce
 17 - Předběžný správce
 18 - Představenstvo
 19 - Podílník
 20 - Revizor
 21 - Revizor v zastoupení
 22 - Člen rozhodčí komise
 23 - Vedoucí odštěpného závodu
 24 - Společník
 25 - Člen správní rady v zastoupení
 26 - Člen statutárního orgánu zřizovatele
 28 - Člen statutárního orgánu v zastoupení
 29 - Insolvenční správce - vyrovnávací
 31 - Člen správní rady
 32 - Statutární orgán zřizovatele v zastoupení
 33 - Zakladatel
 34 - Nástupce zřizovatele
 35 - Zakladatel s vkladem
 36 - Člen sdružení
 37 - Zástupce insolvenčního správce
 38 - Člen kontrolní komise
 39 - Insolvenční správce
 40 - Zástupce správce
 41 - Zvláštní insolvenční správce
 42 - Zvláštní správce
=400 - Podnikatel z RŽP
=401 - Statutár
=402 - Vedoucí org. složky
      */
        public static RelationSimpleEnum TypVazbyToRelationSimple(int typVazby)
        {
            switch (typVazby)
            {

                case -1:
                    return RelationSimpleEnum.Kontrola;
                case -2:
                    return RelationSimpleEnum.Vliv;
                case -3:
                    return RelationSimpleEnum.OsobniVztah;

                case 1:
                    //rel.Relationship = Relation.RelationDescriptionEnum.Jednatel;
                    return RelationSimpleEnum.Statutarni_organ;
                case 3:
                    //rel.Relationship = Relation.RelationDescriptionEnum.Prokura;
                    return RelationSimpleEnum.Statutarni_organ;
                case 4:
                case 7:
                case 2:
                case 18:
                case 25:
                case 26:
                case 28:
                case 31:
                    //rel.Relationship = Relation.RelationDescriptionEnum.Dozorci_rada;
                    return RelationSimpleEnum.Statutarni_organ;
                case 33:
                case 34:
                case 35:
                    //rel.Relationship = Relation.RelationDescriptionEnum.Dozorci_rada;
                    return RelationSimpleEnum.Zakladatel;
                case 5:
                case 9:
                case 10:
                case 15:
                case 19:
                case 24:
                    return RelationSimpleEnum.Spolecnik;
                case 100:
                    //rel.Relationship = Relation.RelationDescriptionEnum.Jednatel;
                    return RelationSimpleEnum.Souhrnny;
                case 23://
                case 29://
                case 11://
                case 12://
                case 13://
                case 16://
                case 17://
                case 37://
                case 40://
                case 41://
                case 42: //
                case 99:
                    return RelationSimpleEnum.Jiny;
                default:
                    //rel.Relationship = Relation.RelationDescriptionEnum.Jednatel;
                    return RelationSimpleEnum.Jiny;
            }
        }
    }
}
