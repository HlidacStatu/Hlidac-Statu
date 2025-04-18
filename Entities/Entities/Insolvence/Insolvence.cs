﻿using Devmasters.Enums;

namespace HlidacStatu.Entities.Insolvence
{
    public partial class Insolvence
    {
        /*
         * https://www.creditcheck.cz/SlovnicekPojmuDetail.aspx?id=23
         Stavy v insolvenčním řízení
Insolvenční řízení (věc) prochází několika stavy:
nevyřízená = c
moratorium = povoleno moratorium
úpadek = v úpadku
konkurs = prohlášený konkurs
oddlužení = povoleno oddlužení
reorganizace = povolena reorganizace
vyřízená = vyřízená věc
pravomocná = pravomocně skončená věc
odškrtnutá = odškrtnutá - skončená věc
zrušeno vrchním soudem = zrušeno vrchním soudem
konkurs po zrušení = prohlášený konkurs po zrušení vrchním soudem
obživlá = obživlá věc
mylný zápis = mylný zápis do rejstříku
postoupená věc = postoupená věc

ODSKRTNUTA = 
ODDLUŽENÍ = 
PRAVOMOCNA = 
KONKURS = 
VYRIZENA = 
NEVYRIZENA = 
MYLNÝ ZÁP. = 
OBZIVLA = 
ÚPADEK = 
REORGANIZ = 
ZRUŠENO VS = 
NEVYR-POST = 
K-PO ZRUŠ. = 
MORATORIUM = 

*/
        public class StavRizeni
        {
            public const string Nevyrizena = "NEVYRIZENA";
            public const string Moratorium = "MORATORIUM";
            public const string Upadek = "ÚPADEK";
            public const string Konkurs = "KONKURS";
            public const string Oddluzeni = "ODDLUŽENÍ";
            public const string Reorganizace = "REORGANIZ";
            public const string Vyrizena = "VYRIZENA";
            public const string Pravomocna = "PRAVOMOCNA";
            public const string Odskrtnuta = "ODSKRTNUTA";
            public const string Zruseno = "ZRUŠENO VS";
            public const string KonkursPoZruseni = "K-PO ZRUŠ.";
            public const string Obzivla = "OBZIVLA";
            public const string MylnyZapis = "MYLNÝ ZÁP.";
            public const string PostoupenaVec = "NEVYR-POST";
        }


        [ShowNiceDisplayName()]
        public enum StavInsolvence : int
        {

            [NiceDisplayName("Neznámý")]
            Neznamy = 0,

            [NiceDisplayName("odškrtnutá - skončená věc")]
            Odskrtnuta = 1,

            [NiceDisplayName("povoleno oddlužení")]
            Oddluzeni = 2,

            [NiceDisplayName("pravomocně skončená věc")]
            Pravomocna = 3,

            [NiceDisplayName("prohlášený konkurs")]
            Konkurs = 4,

            [NiceDisplayName("vyřízená věc")]
            Vyrizena = 5,

            [NiceDisplayName("vyřízená věc")]
            Nevyrizena = 6,

            [NiceDisplayName("mylný zápis do rejstříku")]
            MylnyZapis = 7,

            [NiceDisplayName("obživlá věc")]
            Obzivla = 8,

            [NiceDisplayName("v úpadku")]
            Upadek = 9,

            [NiceDisplayName("povolena reorganizace")]
            Reorganizace = 10,

            [NiceDisplayName("zrušeno vrchním soudem")]
            ZrusenoVS = 11,

            [NiceDisplayName("postoupená věc")]
            Postoupena = 12,

            [NiceDisplayName("prohlášený konkurs po zrušení vrchním soudem")]
            KonkursPoZruseni = 13,

            [NiceDisplayName("povoleno moratorium")]
            Moratorium = 14,
        }

        public static StavInsolvence StavTextToStav(string stav)
        {
            stav = stav.Trim().ToUpper();
            switch (stav)
            {
                case StavRizeni.Odskrtnuta:
                    return StavInsolvence.Odskrtnuta;
                case StavRizeni.Oddluzeni:
                    return StavInsolvence.Oddluzeni;
                case StavRizeni.Pravomocna:
                    return StavInsolvence.Pravomocna;
                case StavRizeni.Konkurs:
                    return StavInsolvence.Konkurs;
                case StavRizeni.Vyrizena:
                    return StavInsolvence.Vyrizena;
                case StavRizeni.Nevyrizena:
                    return StavInsolvence.Nevyrizena;
                case StavRizeni.MylnyZapis:
                    return StavInsolvence.MylnyZapis;
                case StavRizeni.Obzivla:
                    return StavInsolvence.Obzivla;
                case StavRizeni.Upadek:
                    return StavInsolvence.Upadek;
                case StavRizeni.Reorganizace:
                    return StavInsolvence.Reorganizace;
                case StavRizeni.Zruseno:
                    return StavInsolvence.ZrusenoVS;
                case StavRizeni.PostoupenaVec:
                    return StavInsolvence.Postoupena;
                case StavRizeni.KonkursPoZruseni:
                    return StavInsolvence.KonkursPoZruseni;
                case StavRizeni.Moratorium:
                    return StavInsolvence.Moratorium;
                default:
                    return StavInsolvence.Neznamy;
            }
        }

        public static string StavInsolvenceDescription(StavInsolvence stav)
        {
            //https://www.cesr.cz/slovnicek-pojmu/
            //http://www.duverujaleproveruj.cz/13/75-4-insolvencni-zakon-a-insolvencni-rejstrik
            switch (stav)
            {
                case StavInsolvence.Neznamy:
                    return "Stav insolvence je nejasný.";
                case StavInsolvence.Odskrtnuta:
                    return "Ukončení evidence insolvence v administrativní agendě soudu.";
                case StavInsolvence.Oddluzeni:
                    return @"Způsob řešení úpadku dlužníka, 
                            ve kterém jsou dluhy v dohodnuté výši (obvykle pouze část dluhů) placeny dohodnutým způsobem (dohoda mezi dlužníkem a věřiteli). 
                            Maximální délka oddlužení je 5 let a týká se fyzické osoby či právnické osoby, která podle zákona není považována za podnikatele.";
                case StavInsolvence.Pravomocna:
                    return "Věc je pravomocná tehdy, nelze-li již proti ní podat opravný prostředek. Je tedy závazná.";
                case StavInsolvence.Konkurs:
                    return "Řešení údaku, kdy jsou zjištěné pohledávky věřitelů poměrně uspokojeny z výnosu prodeje majetku. " +
                        "Výše poměrného uspokojení věřitelů závisí na velikosti, zajištění a pořadí pohledávky." +
                        "Neuspokojené pohledávky nebo jejich části nezanikají. ";
                case StavInsolvence.Vyrizena:
                    return "Již bylo vydáno rozhodnutí, kterým bude dané řízení ukončeno, " +
                        "toto rozhodnutí ale ještě nenabylo právní moci.";
                case StavInsolvence.Nevyrizena:
                    return "Insolvence, v níž ještě nebylo vydáno tzv. vyřizující rozhodnutí, " +
                        "tedy rozhodnutí, kterým by byla věc ukončena (typicky rozsudek).";
                case StavInsolvence.MylnyZapis:
                    return "Chybný zápis do insolvenčního rejstříku; 'mylný zápis' nevyvolává právní následky.";
                case StavInsolvence.Obzivla:
                    return "Pouze vyřízená insolvence může změnit svůj stav na obživlou. " +
                        "Typicky se tak děje např. v případě, kdy je po podání opravného prostředku (odvolací řízení) zrušeno původní rozhodnutí (označené jako vyřízené) " +
                        "- původní řízení tím takzvaně 'obživne', protože se jím soud musí znovu zabývat.";
                case StavInsolvence.Upadek:
                    return "Způsob řešení majetkové situace dlužníka, který je buď v platební neschopnosti, nebo je předlužen. " +
                        "V zásadě je možné řešit úpadek konkursem, oddlužením či reorganizací.";
                case StavInsolvence.Reorganizace:
                    return "Řešení úpadku, ve kterém je dlužníkem (podnikatelem) plněn reorganizační plán, " +
                        "jímž jsou postupně uspokojovány pohledávky věřitelů.";
                case StavInsolvence.ZrusenoVS:
                    return " Rozhodnutí Krajského soudu zrušené Vrchním soudem se vrací zpět k tomuto soudu k dalšímu projednání a rozhodnutí.";
                case StavInsolvence.Postoupena:
                    return "Insolvence je postupována dál bez vyřízení. " +
                        "Stává se často v případech, kdy byla podána nepříslušnému soudu. " +
                        "Ten ji, aniž by v ní činil jakékoli úkony, zašle soudu příslušnému.";
                case StavInsolvence.KonkursPoZruseni:
                    return "Pravomocní zrušení konkursu,  je ukončeno insolveční řízení. Mezi důvody patří: " +
                        " nebyl osvědčen dlužníkův úpadek; " +
                        "došlo-li již ke zpeněžení podstatné části majetku; " +
                        "není žádný přihlášený věřitel a všechny pohledávky jsou uspokojeny; " +
                        "pro uspokojení věřitelů je majetek dlužníka zcela nepostačující; " +
                        "všichni věřitelé a insolvenční správce vyslovili se zrušením konkursu souhlas.";
                case StavInsolvence.Moratorium:
                    return "Insolvenční řízení, ve kterém je na odůvodněný návrh dlužníka či věřitele prohlášeno soudem moratorium; " +
                        "po dobu moratoria nelze na dlužníka prohlásit úpadek. Délka moratoria je maximálně 3 měsíce + 30 dní prodloužení.";
                default:
                    return "";
            }
        }









    }
}
