using HlidacStatu.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HlidacStatu.Repositories.StretZajmu
{
    public class Data
    {

        /*
Zákon č. 159/2006 Sb.
Zákon o střetu zájmů

19.12.2024 1c) člen vlády nebo vedoucí jiného ústředního správního úřadu, v jehož čele není člen vlády1),
19.12.2024 1d) náměstek člena vlády nebo náměstek ministra vnitra pro státní službu,
01.09.2017 1e) vedoucí Kanceláře Poslanecké sněmovny, vedoucí Kanceláře Senátu nebo vedoucí Kanceláře prezidenta republiky,

01.01.2025 1f) místopředseda Úřadu pro ochranu osobních údajů,
01.09.2017 1g) předseda Úřadu pro technickou normalizaci, metrologii a státní zkušebnictví,
01.09.2017 1h) člen Rady Českého telekomunikačního úřadu,
01.09.2017 1i) člen Rady Energetického regulačního úřadu,
01.09.2017 1j) člen bankovní rady České národní banky,
01.09.2017 1k) prezident, viceprezident a člen Nejvyššího kontrolního úřadu,
01.09.2017 1l) předseda nebo člen Úřadu pro dohled nad hospodařením politických stran a politických hnutí,
01.07.2025 1m) veřejný ochránce práv, ochránce práv dětí a jejich zástupce,

01.07.2022 2b) člen statutárního orgánu, člen řídicího, dozorčího nebo kontrolního orgánu právnické osoby zřízené zákonem, státní příspěvkové organizace, příspěvkové organizace územního samosprávného celku, s výjimkou právnických osob vykonávajících činnost školy nebo školského zařízení a s výjimkou členů správních rad veřejných vysokých škol a statutárního orgánu nebo členů statutárního orgánu, členů řídicího, dozorčího nebo kontrolního orgánu samosprávných stavovských organizací zřízených zákonem,
09.10.2009 2c) vedoucí zaměstnanec 2. až 4. stupně řízení podle zvláštního právního předpisu3c) právnické osoby zřízené zákonem, státní příspěvkové organizace, příspěvkové organizace územního samosprávného celku, s výjimkou právnických osob vykonávajících činnost školy nebo školského zařízení,
01.09.2017 2d) vedoucí organizační složky státu, vedoucí zaměstnanec 2. až 4. stupně řízení podle zvláštního právního předpisu3c) v organizační složce státu, s výjimkou zpravodajské služby3b), nebo představený podle zákona o státní službě, nejde-li o vedoucího oddělení nebo o příslušníka zpravodajské služby3b),
01.09.2017 2e) vedoucí úředník územního samosprávného celku podílející se na výkonu správních činností zařazený do obecního úřadu, do úřadu městského obvodu nebo úřadu městské části územně členěného statutárního města, do krajského úřadu, do Magistrátu hlavního města Prahy nebo úřadu městské části hlavního města Prahy,
01.09.2017 2f) soudce,
01.09.2017 2g) státní zástupce,
    */

        //přidat limitní rok od roku 2017
        

        public async static Task<Role> Vlada_1c_Async()
        {

            var sql_Vlada = @"
select NameId, oe.pk
from OsobaEvent oe
	inner join Osoba o on oe.OsobaId = o.InternalId
where
 oe.[Type] in (1,6,7,9,10,11)  and
( AddInfo like N'ministr%'
  or AddInfo like N'člen vlády%'
  or AddInfo like N'předseda vlády'
)
and not (organizace like N'Vláda SR%')
and not (organizace like N'%stínová%')
and dbo.IsSomehowInInterval(datumOd,datumDo, @zakonDatumOd,null)=1
-- and nameid like 'andrej-babis'

";
            var o_1c = await Role.FillRoleAsync(sql_Vlada, new DateTime(2024, 12, 19), null);
            o_1c.Osoby = o_1c.Osoby.OrderBy(o => o.Osoba.Prijmeni).ToList();
            o_1c.ZakonDatumParagraf = new DateTime(2024, 12, 19);
            return o_1c;
        }

        public async static Task<Role> Namestci_1d_Async()
        {
            var sql_NamestekMinistra = @"select NameId, oe.pk

from OsobaEvent oe
	inner join Osoba o on oe.OsobaId = o.InternalId
where
 oe.[Type] in (1,6,7,9,10,11)  and
( AddInfo like N'náměstek%'
  or AddInfo like N'náměstky%'
  or AddInfo like N'státní tajemn'
)
and not (organizace like N'Vláda SR%')
and not (organizace like N'%stínová%')
and dbo.IsSomehowInInterval(datumOd,datumDo, '@zakonDatumOd',null)=1
and ico in (#icos#)
";


            sql_NamestekMinistra = sql_NamestekMinistra.Replace(
                "#icos#", string
                    .Join(",", (await FirmaRepo.Zatrideni.GetIcoDirectAsync(Firma.Zatrideni.SubjektyObory.Ministerstva)).Select(m => $"'{m}'")
            ));
            var o_1d = await Role.FillRoleAsync(sql_NamestekMinistra, new DateTime(2024, 12, 19), null);

            return o_1d;
        }
    }
}

/*
*§ 4
* Veřejný funkcionář uvedený v § 2 odst. 1 písm. c) až m) nesmí
01.01.2007 a) podnikat nebo provozovat jinou samostatnou výdělečnou činnost,
01.09.2017 b) být členem statutárního orgánu, členem řídícího, dozorčího nebo kontrolního orgánu právnické osoby, která podniká (dále jen „podnikající právnická osoba“), pokud zvláštní právní předpis nestanoví jinak, nebo
01.01.2007 c) být v pracovněprávním nebo obdobném vztahu nebo ve služebním poměru, nejde-li o vztah nebo poměr, v němž působí jako veřejný funkcionář.

 *
19.12.2024 c) člen vlády nebo vedoucí jiného ústředního správního úřadu, v jehož čele není člen vlády1),
19.12.2024 d) náměstek člena vlády nebo náměstek ministra vnitra pro státní službu,
01.09.2017 e) vedoucí Kanceláře Poslanecké sněmovny, vedoucí Kanceláře Senátu nebo vedoucí Kanceláře prezidenta republiky,
01.01.2025 f) místopředseda Úřadu pro ochranu osobních údajů,
01.09.2017 g) předseda Úřadu pro technickou normalizaci, metrologii a státní zkušebnictví,
01.09.2017 h) člen Rady Českého telekomunikačního úřadu,
01.09.2017 i) člen Rady Energetického regulačního úřadu,
01.09.2017 j) člen bankovní rady České národní banky,
01.09.2017 k) prezident, viceprezident a člen Nejvyššího kontrolního úřadu,
01.09.2017 l) předseda nebo člen Úřadu pro dohled nad hospodařením politických stran a politických hnutí,
01.07.2025 m) veřejný ochránce práv, ochránce práv dětí a jejich zástupce,
 */

/*
 * 19.12.2024 § 4a Veřejný funkcionář uvedený v § 2 odst. 1 nesmí být provozovatelem rozhlasového nebo televizního vysílání nebo
 * vydavatelem periodického tisku ani společníkem, členem nebo ovládající osobou právnické osoby, která je provozovatelem
 * rozhlasového nebo televizního vysílání nebo vydavatelem periodického tisku.
 *
 * § 5 (3) S funkcí poslance nebo senátora je neslučitelný výkon funkce v pracovním nebo služebním poměru k České republice, pokud jde o funkce jmenované nebo o funkce, v nichž se při výkonu státní správy rozhoduje,
01.01.2007 a) na ministerstvu nebo na jiném správním úřadu,
01.01.2007 b) na státním zastupitelství nebo soudu,
01.07.2025 c) v bezpečnostních sborech5), ozbrojených silách České republiky, Nejvyšším kontrolním úřadu, Kanceláři prezidenta republiky, Kanceláři Poslanecké sněmovny, Kanceláři Senátu, státních fondech a v Kanceláři veřejného ochránce práv a ochránce práv dětí.
 * vice mene vsichni politici
 */

/*
 * § 4b
    09.02.2017 Obchodní společnost, ve které veřejný funkcionář uvedený v § 2 odst. 1 písm. c) nebo jím ovládaná osoba vlastní podíl
    představující alespoň 25 % účasti společníka v obchodní společnosti, se nesmí účastnit zadávacích řízení podle zákona upravujícího
    zadávání veřejných zakázek jako účastník nebo poddodavatel, prostřednictvím kterého dodavatel prokazuje kvalifikaci.
    Zadavatel je povinen takovou obchodní společnost vyloučit ze zadávacího řízení. Zadavatel nesmí obchodní společnosti uvedené ve větě první zadat veřejnou zakázku malého rozsahu, takové jednání je neplatné.

    § 4c
    19.12.2024 Je zakázáno poskytnout dotaci podle právního předpisu upravujícího rozpočtová pravidla14) nebo investiční pobídku podle
    právního předpisu upravujícího investiční pobídky15) obchodní společnosti, ve které veřejný funkcionář uvedený
    v § 2 odst. 1 písm. c) nebo jím ovládaná osoba vlastní podíl představující alespoň 25 % účasti společníka v obchodní společnosti.

 * c) člen vlády nebo vedoucí jiného ústředního správního úřadu, v jehož čele není člen vlády1),

 */



/*
 * § 5
 * Poslanci nebo senátorovi, který zastupuje stát v řídících, dozorčích nebo kontrolních orgánech podnikající právnické osoby, pokud v ní má stát, jím ovládané právnické osoby, Česká národní banka,
 * nebo všechny tyto osoby společně, podíl nebo hlasovací práva, nenáleží za tuto činnost odměna,
 *
 * Veřejnému funkcionáři uvedenému v § 2 odst. 1 písm. o) a p), který je krajem, hlavním městem Prahou, obcí, městskou částí nebo městským obvodem územně členěného
 * statutárního města nebo městskou částí hlavního města Prahy určen, aby vykonával funkci člena řídícího, dozorčího nebo kontrolního orgánu podnikající právnické
 * osoby, pokud v ní kraj, hlavní město Praha, obec, městská část nebo městský obvod územně členěného statutárního města nebo městská část hlavního města Prahy
 * nebo jimi ovládaná právnická osoba má podíl nebo hlasovací práva, nenáleží za tuto činnost odměna, podíl na zisku nebo jiné plnění,
 *
 * o) člen zastupitelstva kraje nebo člen Zastupitelstva hlavního města Prahy, který je pro výkon funkce dlouhodobě uvolněn nebo který před svým zvolením do funkce člena zastupitelstva nebyl v pracovním poměru, ale vykonává funkce ve stejném rozsahu jako člen zastupitelstva, který je pro výkon funkce dlouhodobě uvolněn,
   p) člen zastupitelstva obce, městské části nebo městského obvodu územně členěného statutárního města nebo městské části hlavního města Prahy, který je pro výkon funkce dlouhodobě uvolněn nebo který před svým zvolením do funkce člena zastupitelstva nebyl v pracovním poměru, ale vykonává funkce ve stejném rozsahu jako člen zastupitelstva, který je pro výkon funkce dlouhodobě uvolněn, nebo

 */


/*
 * 01.09.2017 § 6 (1) Veřejný funkcionář uvedený v § 2 odst. 1 písm. c) až p) a odst. 2 písm. b) až g) se nesmí po dobu 1 roku od
 * skončení výkonu funkce stát společníkem anebo působit v orgánech podnikající právnické osoby, anebo uzavřít pracovněprávní
 * vztah se zaměstnavatelem vykonávajícím podnikatelskou činnost, pokud taková právnická osoba nebo zaměstnavatel v posledních 3 letech
 * přede dnem skončení funkce veřejného funkcionáře uzavřeli smlouvu se státem, územním samosprávným celkem nebo právnickou osobou
 * zřízenou zákonem nebo zřízenou či založenou státem nebo územním samosprávným celkem, jednalo-li se o nadlimitní veřejnou zakázku,
 * a pokud veřejný funkcionář nebo orgán, ve kterém veřejný funkcionář působil, o takové smlouvě rozhodoval.
    *
 (2) Omezení veřejného funkcionáře uvedená v odstavci 1 platí obdobně pro právnické osoby, které jsou podnikající právnickou osobou nebo zaměstnavatelem uvedenými v odstavci 1 zřízeny nebo ovládány.

 c) člen vlády nebo vedoucí jiného ústředního správního úřadu, v jehož čele není člen vlády1),
19.12.2024 d) náměstek člena vlády nebo náměstek ministra vnitra pro státní službu,
01.09.2017 e) vedoucí Kanceláře Poslanecké sněmovny, vedoucí Kanceláře Senátu nebo vedoucí Kanceláře prezidenta republiky,
01.01.2025 f) místopředseda Úřadu pro ochranu osobních údajů,
01.09.2017 g) předseda Úřadu pro technickou normalizaci, metrologii a státní zkušebnictví,
01.09.2017 h) člen Rady Českého telekomunikačního úřadu,
01.09.2017 i) člen Rady Energetického regulačního úřadu,
01.09.2017 j) člen bankovní rady České národní banky,
01.09.2017 k) prezident, viceprezident a člen Nejvyššího kontrolního úřadu,
01.09.2017 l) předseda nebo člen Úřadu pro dohled nad hospodařením politických stran a politických hnutí,
01.07.2025 m) veřejný ochránce práv, ochránce práv dětí a jejich zástupce,
01.09.2017 n) člen Rady pro rozhlasové a televizní vysílání,
01.09.2017 o) člen zastupitelstva kraje nebo člen Zastupitelstva hlavního města Prahy, který je pro výkon funkce dlouhodobě uvolněn nebo který před svým zvolením do funkce člena zastupitelstva nebyl v pracovním poměru, ale vykonává funkce ve stejném rozsahu jako člen zastupitelstva, který je pro výkon funkce dlouhodobě uvolněn,
01.09.2017 p) člen zastupitelstva obce, městské části nebo městského obvodu územně členěného statutárního města nebo městské části hlavního města Prahy, který je pro výkon funkce dlouhodobě uvolněn nebo který před svým zvolením do funkce člena zastupitelstva nebyl v pracovním poměru, ale vykonává funkce ve stejném rozsahu jako člen zastupitelstva, který je pro výkon funkce dlouhodobě uvolněn, nebo

01.07.2022 b) člen statutárního orgánu, člen řídicího, dozorčího nebo kontrolního orgánu právnické osoby zřízené zákonem, státní příspěvkové organizace, příspěvkové organizace územního samosprávného celku, s výjimkou právnických osob vykonávajících činnost školy nebo školského zařízení a s výjimkou členů správních rad veřejných vysokých škol a statutárního orgánu nebo členů statutárního orgánu, členů řídicího, dozorčího nebo kontrolního orgánu samosprávných stavovských organizací zřízených zákonem,
09.10.2009 c) vedoucí zaměstnanec 2. až 4. stupně řízení podle zvláštního právního předpisu 3c) právnické osoby zřízené zákonem, státní příspěvkové organizace, příspěvkové organizace územního samosprávného celku, s výjimkou právnických osob vykonávajících činnost školy nebo školského zařízení,
01.09.2017 d) vedoucí organizační složky státu, vedoucí zaměstnanec 2. až 4. stupně řízení podle zvláštního právního předpisu3c) v organizační složce státu, s výjimkou zpravodajské služby3b), nebo představený podle zákona o státní službě, nejde-li o vedoucího oddělení nebo o příslušníka zpravodajské služby3b),
01.09.2017 e) vedoucí úředník územního samosprávného celku podílející se na výkonu správních činností zařazený do obecního úřadu, do úřadu městského obvodu nebo úřadu městské části územně členěného statutárního města, do krajského úřadu, do Magistrátu hlavního města Prahy nebo úřadu městské části hlavního města Prahy,
01.09.2017 f) soudce,
01.09.2017 g) státní zástupce,
 */
