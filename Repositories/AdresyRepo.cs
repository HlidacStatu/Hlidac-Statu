using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HlidacStatu.Entities;
using HlidacStatu.Entities.Views;
using Microsoft.EntityFrameworkCore;

namespace HlidacStatu.Repositories;

public class AdresyRepo
{
    public static async Task<List<AdresyKVolbam>> GetAdresyKVolbamAsync()
    {
        await using (DbEntities db = new DbEntities())
        {
            db.Database.SetCommandTimeout(TimeSpan.FromMinutes(5));
            // TypSO se filtruje kvůli číslům evidenčním, na kterých nemůže být trvalý pobyt, proto je nepotřebujeme
            // ovm.zkratka se filtruje kvůli:
            // a) vojenským újezdům,
            // b) statutárním městům, kde voličský průkaz vyřizují městské obvody
            var adresy = await db.AdresyKVolbam.FromSqlRaw(
                @"select ovm.Nazev NazevUradu, ovm.TypOvmId TypOvm, ovm.IdDS DatovkaUradu, amo.oneliner AdresaUradu, 
                         am.KodAdm Id, am.oneliner Adresa, am.NazevObce Obec
                    from dbo.OrganVerejneMoci ovm
                    join dbo.AdresniMisto amo on ovm.AdresaOvmId = amo.KodAdm
                    join dbo.AdresniMisto am on amo.KodObce = am.KodObce 
                            and ISNULL(am.KodMomc, '') = ISNULL(amo.KodMomc,'') and am.TypSO = N'č.p.'
                   where typovmid in (5,6,7,8)
                     and stavsubjektu = 1
                     and ovm.Zkratka not in ('LIBAVA','BOLETICE','KPRAHA','Plzen','UstinL','Ostrava',
                                             'HRADISTE','Pardubice','Brno','BREZINA')")
                .ToListAsync();

            return adresy;
        }
    }
    
}