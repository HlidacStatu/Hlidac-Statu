using HlidacStatu.Entities;

using Microsoft.EntityFrameworkCore;

using System;
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

    }
}