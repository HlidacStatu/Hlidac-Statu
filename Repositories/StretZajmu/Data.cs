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
and dbo.IsSomehowInInterval(datumOd,datumDo, '@zakonDatumOd',null)=1
and nameid like '%schillerova'

";
            var o_1c = await Role.FillRoleAsync(sql_Vlada, new DateTime(2024, 12, 19), null);
            o_1c.Osoby = o_1c.Osoby.OrderBy(o => o.Osoba.Prijmeni).ToList();
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
