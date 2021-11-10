using System.Collections.Generic;

namespace HlidacStatu.Util
{
    public class CZ_Nuts
    {

        public static Dictionary<string, string> Kraje = new Dictionary<string, string>()
        {
{"CZ010","Hlavní město Praha"},
{"CZ031","Jihočeský kraj"},
{"CZ064","Jihomoravský kraj"},
{"CZ041","Karlovarský kraj"},
{"CZ063","Kraj Vysočina"},
{"CZ052","Královéhradecký kraj"},
{"CZ051","Liberecký kraj"},
{"CZ080","Moravskoslezský kraj"},
{"CZ071","Olomoucký kraj"},
{"CZ053","Pardubický kraj"},
{"CZ032","Plzeňský kraj"},
{"CZ020","Středočeský kraj"},
{"CZ042","Ústecký kraj"},
{"CZ072","Zlínský kraj"}
        };

        public static Dictionary<string, string> Okresy = new Dictionary<string, string>()
        {
        {"CZ0100","Praha"},
{"CZ0201","Benešov"},
{"CZ0202","Beroun"},
{"CZ0203","Kladno"},
{"CZ0204","Kolín"},
{"CZ0205","Kutná Hora"},
{"CZ0206","Mělník"},
{"CZ0207","Mladá Boleslav"},
{"CZ0208","Nymburk"},
{"CZ0209","Praha-východ"},
{"CZ020A","Praha-západ"},
{"CZ020B","Příbram"},
{"CZ020C","Rakovník"},
{"CZ0311","České Budějovice"},
{"CZ0312","Český Krumlov"},
{"CZ0313","Jindřichův Hradec"},
{"CZ0314","Písek"},
{"CZ0315","Prachatice"},
{"CZ0316","Strakonice"},
{"CZ0317","Tábor"},
{"CZ0321","Domažlice"},
{"CZ0322","Klatovy"},
{"CZ0323","Plzeň-město"},
{"CZ0324","Plzeň-jih"},
{"CZ0325","Plzeň-sever"},
{"CZ0326","Rokycany"},
{"CZ0327","Tachov"},
{"CZ0411","Cheb"},
{"CZ0412","Karlovy Vary"},
{"CZ0413","Sokolov"},
{"CZ0421","Děčín"},
{"CZ0422","Chomutov"},
{"CZ0423","Litoměřice"},
{"CZ0424","Louny"},
{"CZ0425","Most"},
{"CZ0426","Teplice"},
{"CZ0427","Ústí nad Labem"},
{"CZ0511","Česká Lípa"},
{"CZ0512","Jablonec nad Nisou"},
{"CZ0513","Liberec"},
{"CZ0514","Semily"},
{"CZ0521","Hradec Králové"},
{"CZ0522","Jičín"},
{"CZ0523","Náchod"},
{"CZ0524","Rychnov nad Kněžnou"},
{"CZ0525","Trutnov"},
{"CZ0531","Chrudim"},
{"CZ0532","Pardubice"},
{"CZ0533","Svitavy"},
{"CZ0534","Ústí nad Orlicí"},
{"CZ0631","Havlíčkův Brod"},
{"CZ0632","Jihlava"},
{"CZ0633","Pelhřimov"},
{"CZ0634","Třebíč"},
{"CZ0635","Žďár nad Sázavou"},
{"CZ0641","Blansko"},
{"CZ0642","Brno-město"},
{"CZ0643","Brno-venkov"},
{"CZ0644","Břeclav"},
{"CZ0645","Hodonín"},
{"CZ0646","Vyškov"},
{"CZ0647","Znojmo"},
{"CZ0711","Jeseník"},
{"CZ0712","Olomouc"},
{"CZ0713","Prostějov"},
{"CZ0714","Přerov"},
{"CZ0715","Šumperk"},
{"CZ0721","Kroměříž"},
{"CZ0722","Uherské Hradiště"},
{"CZ0723","Vsetín"},
{"CZ0724","Zlín"},
{"CZ0801","Bruntál"},
{"CZ0802","Frýdek-Místek"},
{"CZ0803","Karviná"},
{"CZ0804","Nový Jičín"},
{"CZ0805","Opava"},
{"CZ0806","Ostrava-město"},
{"CZZZZZ","Extra-Regio"},
        };

        public static string Nace2Kraj(string nace, string ifUnknown = "")
        {
            nace = nace.ToUpper();
            if (Kraje.ContainsKey(nace))
                return Kraje[nace];
            else
                return ifUnknown;

        }
        public static string Nace2Okres(string nace, string ifUnknown = "")
        {
            nace = nace.ToUpper();
            if (Okresy.ContainsKey(nace))
                return Okresy[nace];
            else
                return ifUnknown;

        }
    }
}
