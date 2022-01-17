using Devmasters.Enums;

using Force.DeepCloner;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HlidacStatu.Datastructures.Graphs
{
    public class Relation
    {

        public static TimeSpan NedavnyVztahDelka = TimeSpan.FromDays((365 * 5) + 2); //5 let


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
            Podnikatel_z_OR = 00,
            [NiceDisplayName("Člen statutárního orgánu")]
            Clen_statutarniho_organu = 01,
            [NiceDisplayName("Likvidátor")]
            Likvidator = 02,
            [NiceDisplayName("Prokurista")]
            Prokurista = 03,
            [NiceDisplayName("Člen dozorčí rady")]
            Clen_dozorci_rady = 04,
            [NiceDisplayName("Jediný akcionář")]
            Jediny_akcionar = 05,
            [NiceDisplayName("Člen družstva s vkladem")]
            Clen_druzstva_s_vkladem = 06,
            [NiceDisplayName("Člen dozorčí rady v zastoupení")]
            Clen_dozorci_rady_v_zastoupeni = 07,
            [NiceDisplayName("Člen kontrolní komise v zastoupení")]
            Clen_kontrolni_komise_v_zastoupeni = 08,
            [NiceDisplayName("Komplementář")]
            Komplementar = 09,
            [NiceDisplayName("Komanditista")]
            Komanditista = 10,
            [NiceDisplayName("Správce konkursu")]
            Spravce_konkursu = 11,
            [NiceDisplayName("Likvidátor v zastoupení")]
            Likvidator_v_zastoupeni = 12,
            [NiceDisplayName("Oddělený insolvenční správce")]
            Oddeleny_insolvencni_spravce = 13,
            [NiceDisplayName("Pobočný spolek")]
            Pobocny_spolek = 14,
            [NiceDisplayName("Podnikatel")]
            Podnikatel = 15,
            [NiceDisplayName("Předběžný insolvenční správce")]
            Predbezny_insolvencni_spravce = 16,
            [NiceDisplayName("Předběžný správce")]
            Predbezny_spravce = 17,
            [NiceDisplayName("Představenstvo")]
            Predstavenstvo = 18,
            [NiceDisplayName("Podílník")]
            Podilnik = 19,
            [NiceDisplayName("Revizor")]
            Revizor = 20,
            [NiceDisplayName("Revizor v zastoupení")]
            Revizor_v_zastoupeni = 21,
            [NiceDisplayName("Člen rozhodčí komise")]
            Clen_rozhodci_komise = 22,
            [NiceDisplayName("Vedoucí odštěpného závodu")]
            Vedouci_odstepneho_zavodu = 23,
            [NiceDisplayName("Společník")]
            Spolecnik = 24,
            [NiceDisplayName("Člen správní rady v zastoupení")]
            Clen_spravni_rady_v_zastoupeni = 25,
            [NiceDisplayName("Člen statutárního orgánu zřizovatele")]
            Clen_statutarniho_organu_zrizovatele = 26,
            [NiceDisplayName("Člen statutárního orgánu v zastoupení")]
            Clen_statutarniho_organu_v_zastoupeni = 28,
            [NiceDisplayName("Insolvenční správce vyrovnávací")]
            Insolvencni_spravce_vyrovnavaci = 29,
            [NiceDisplayName("Člen správní rady")]
            Clen_spravni_rady = 31,
            [NiceDisplayName("Statutární orgán zřizovatele v zastoupení")]
            Statutarni_organ_zrizovatele_v_zastoupeni = 32,
            [NiceDisplayName("Zakladatel")]
            Zakladatel = 33,
            [NiceDisplayName("Nástupce zřizovatele")]
            Nastupce_zrizovatele = 34,
            [NiceDisplayName("Zakladatel s vkladem")]
            Zakladatel_s_vkladem = 35,
            [NiceDisplayName("Člen sdružení")]
            Clen_sdruzeni = 36,
            [NiceDisplayName("Zástupce insolvenčního správce")]
            Zastupce_insolvencniho_spravce = 37,
            [NiceDisplayName("Člen kontrolní komise")]
            Clen_kontrolni_komise = 38,
            [NiceDisplayName("Insolvenční správce")]
            Insolvencni_spravce = 39,
            [NiceDisplayName("Zástupce správce")]
            Zastupce_spravce = 40,
            [NiceDisplayName("Zvláštní insolvenční správce")]
            Zvlastni_insolvencni_spravce = 41,
            [NiceDisplayName("Zvláštní správce")]
            Zvlastni_spravce = 42,
            [NiceDisplayName("Podnikatel z RŽP")]
            Podnikatel_z_RZP = 400,
            [NiceDisplayName("Statutár")]
            Statutar = 401,
            [NiceDisplayName("Vedoucí org. složky")]
            Vedouci_org_slozky = 402,

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




        public static Graph.Edge[] AktualniVazby_old(IEnumerable<Graph.Edge> allRelations, AktualnostType minAktualnost)
        {
            if (allRelations == null)
                return new Graph.Edge[] { };

            if (minAktualnost <= AktualnostType.Neaktualni)
                return allRelations.ToArray();

            return allRelations
                .Where(m => m.Aktualnost >= minAktualnost)
                .ToArray();
        }

        public static Graph.Edge[] AktualniVazby(IEnumerable<Graph.Edge> allRelations, AktualnostType minAktualnost, Graph.Edge root)
        {
            if (allRelations == null)
                return new Graph.Edge[] { };

            if (minAktualnost <= AktualnostType.Neaktualni)
                return allRelations.ToArray();

            root.Aktualnost = minAktualnost;

            //filter per distanceallRelations.First(m => m.Root == true)
            var filteredRels = _childrenVazby(root, 
                allRelations.Where(m=>!m.Root).DeepClone(), 
                new Graph.Edge[] { },
                minAktualnost,0, allRelations.FirstOrDefault(m=>m.Root));

            //add root
            //var res = filteredRels.Prepend(root);

            return filteredRels
                .Where(m => m.Aktualnost >= minAktualnost)
                .ToArray();
        }

        private static Graph.Edge[] _childrenVazby(Graph.Edge parent, IEnumerable<Graph.Edge> vazby, 
            IEnumerable<Graph.Edge> exclude, AktualnostType minAktualnost, int callDeep, Graph.Edge originalRoot)
        {
            //AktualnostType akt = parent.Aktualnost;
            //if (minAktualnost >= parent.Aktualnost)


            List<Graph.Edge> items = new List<Graph.Edge>();

            if (callDeep > 100)
            {
                //primitive stackoverflow protection
                HlidacStatu.Util.Consts.Logger.Error("_childrenVazby stackoverflow protection {@parentFrom}-{parentTo} {@originalRootFrom}-{@originalRootTo}", 
                    parent.From?.UniqId, parent.To?.UniqId, originalRoot.From?.UniqId, originalRoot.To?.UniqId);
                return items.ToArray();
            }

            var fVazby = vazby
                    .Where(m =>
                            parent.To.UniqId == m.From.UniqId
                            //&& m.Distance == parent.Distance + 1
                            && m.Aktualnost >= parent.Aktualnost
                            && m.Aktualnost >= minAktualnost
                        )
                    .ToArray();
            //var fVazby2 = vazby
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

                if (ch.Aktualnost > minAktualnost)
                    ch.Aktualnost = minAktualnost;
                items.Add(ch);
                var chVazby = vazby;//.Where(m => ch.To.UniqId == m.From.UniqId && m.Distance == ch.Distance + 1).ToArray();
                items.AddRange(
                    _childrenVazby(ch, vazby, exclude.Concat(items), minAktualnost, callDeep+1, originalRoot)
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
        public static string TypVazbyToDescription(int typVazby)
        {
            RelationSimpleEnum simple = TypVazbyToRelationSimple(typVazby);

            switch (typVazby)
            {

                case 1:
                    return RelationSimpleEnum.Jednatel.ToNiceDisplayName();
                case 3:
                    return RelationSimpleEnum.Prokura.ToNiceDisplayName();
                case 4:
                case 7:
                case 2:
                case 18:
                case 25:
                case 26:
                case 28:
                case 31:
                    return RelationSimpleEnum.Dozorci_rada.ToNiceDisplayName();

                case 33:
                case 34:
                case 35:
                    return RelationSimpleEnum.Dozorci_rada.ToNiceDisplayName();
                case 5:
                case 9:
                case 10:
                case 15:
                case 19:
                case 24:
                    return RelationSimpleEnum.Spolecnik.ToNiceDisplayName();
                case 100:
                    return RelationSimpleEnum.Jednatel.ToNiceDisplayName();
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
                default:
                    return simple.ToNiceDisplayName();
            }
        }
    }
}
