using HlidacStatu.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

namespace HlidacStatu.Repositories
{
    public class OvmRepo
    {
        public static List<OrganVerejneMoci> UradyOvm()
        {
            using var dbContext = new DbEntities();
            return dbContext.OrganVerejneMoci.AsNoTracking()
                .Where(o => o.TypDS.StartsWith("OVM") && o.ICO != null)
                .ToList();
        }

        public static List<OrganVerejneMoci> Ministerstva()
        {
            using var dbContext = new DbEntities();
            return dbContext.OrganVerejneMoci.AsNoTracking()
                .Where(o => o.TypOvmId == 4)
                .ToList();
        }

        public static List<OrganVerejneMoci> KrajskeUrady()
        {
            using var dbContext = new DbEntities();
            return dbContext.OrganVerejneMoci.AsNoTracking()
                .Where(o => o.TypOvmId == 3)
                .ToList();
        }

        public static List<OrganVerejneMoci> ObceIII()
        {
            using var dbContext = new DbEntities();
            return dbContext.OrganVerejneMoci.AsNoTracking()
                .Where(o => o.TypOvmId == 7 && o.PrimarniOvm == "Ano")
                .ToList();
        }

        public static List<OrganVerejneMoci> StatutarniMesta()
        {
            using var dbContext = new DbEntities();
            return dbContext.OrganVerejneMoci.AsNoTracking()
                .Where(o => o.TypOvmId == 8 && o.PrimarniOvm == "Ano")
                .ToList();
        }

        public static List<OrganVerejneMoci> OrganizacniSlozkyStatu()
        {
            using var dbContext = new DbEntities();
            return dbContext.OrganVerejneMoci.AsNoTracking()
                .Where(o => o.TypOvmId == 11
                            && o.PravniFormaOvmId == "325"
                            && o.PrimarniOvm == "Ano")
                .ToList();
        }

        public static List<OrganVerejneMoci> VysokeSkoly()
        {
            using var dbContext = new DbEntities();
            return dbContext.OrganVerejneMoci.AsNoTracking()
                .Where(o => o.PravniFormaOvmId == "601"
                            && o.PrimarniOvm == "Ano")
                .ToList();
        }

        public static List<OrganVerejneMoci> ObceSRozsirenouPusobnosti()
        {
            using var dbContext = new DbEntities();
            return dbContext.OrganVerejneMoci.AsNoTracking()
                .Where(o => o.TypOvmId == 8 || o.TypOvmId == 7)
                .Where(o => o.PrimarniOvm == "Ano")
                .ToList();
        }
        
        public static Dictionary<string, string[]> ObceSRozsirenouPusobnostiPodleKraju()
        {
            using var dbContext = new DbEntities();
            {
                var deb2 = dbContext.OrganVerejneMoci.
                    Include(adr => adr.AdresaOvm)
                    .AsNoTracking()
                    .Where(m=>m != null)
                    ;
                var deb3 = deb2
                    .Where(o => o.TypOvmId == 8 || o.TypOvmId == 7);

                var deb4 = deb3
                    .Where(o => o.PrimarniOvm == "Ano")
                    .ToList();
                if (deb4.Any(m => m.IdDS == "mgjbetz")) // Dobruska nema kraj
                {
                    var rec = deb4.FirstOrDefault(m => m.IdDS == "mgjbetz");
                    rec.AdresaOvm.KrajNazev = "Královéhradecký";
                }
                var deb5 = deb4
                    .GroupBy(o => o.AdresaOvm.KrajNazev, o => o.IdDS ?? "");
                var deb6 = deb5
                    .Where(g=>g.Key != null)
                    .ToDictionary(g => g.Key, g => g.ToArray());
                return deb6;
            }
        }
        public static List<string> AllIcosStatniOVM()
        {
            using var dbContext = new DbEntities();
            return dbContext.OrganVerejneMoci.AsNoTracking()
                .Where(o=>
                    o.TypOvmId == 3 ||
                    o.TypOvmId == 4 ||
                    o.TypOvmId == 5 ||
                    o.TypOvmId == 6 ||
                    o.TypOvmId == 7 ||
                    o.TypOvmId == 8 ||
                    o.TypOvmId == 11 ||
                    o.TypOvmId == 17 ||
                    o.TypOvmId == 18 ||
                    o.TypOvmId == 22 ||
                    o.TypOvmId == 23 
                    )
                .Select(o => o.ICO)
                .ToList();
        }

        public static List<string> AllIcos()
        {
            using var dbContext = new DbEntities();
            return dbContext.OrganVerejneMoci.AsNoTracking().Select(o => o.ICO).ToList();
        }
        
        
    }
}