using HlidacStatu.Entities;
using HlidacStatu.Lib.Data.External.NespolehlivyPlatceDPH;

using Microsoft.EntityFrameworkCore;

using System;
using System.Collections.Generic;
using System.Linq;

namespace HlidacStatu.Repositories
{
    public static class NespolehlivyPlatceDphRepo
    {
        public static Dictionary<string, NespolehlivyPlatceDPH> GetAllFromDb()
        {
            using (DbEntities db = new DbEntities())
            {
                return db.NespolehlivyPlatceDPH.AsNoTracking().ToDictionary(k => k.Ico, k => k);
            }
        }

        public static Dictionary<string, NespolehlivyPlatceDPH> GetDataFromGFR()
        {
            var client = new rozhraniCRPDPHClient();
            var result = client.getSeznamNespolehlivyPlatce(out var firmy);
            if (result.statusCode == 0)
            {
                return firmy
                    .Select(f =>
                        new NespolehlivyPlatceDPH()
                        {
                            Ico = f.dic.Trim(),
                            FromDate = f.datumZverejneniNespolehlivostiSpecified
                                ? f.datumZverejneniNespolehlivosti
                                : (DateTime?)null
                        })
                    .ToDictionary(k => k.Ico, v => v);
            }
            else
                return null;
        }

        public static void UpdateData()
        {
            var newData = GetDataFromGFR();
            if (newData != null)
            {
                using (DbEntities db = new DbEntities())
                {
                    foreach (var key in newData.Keys)
                    {
                        var exist = db.NespolehlivyPlatceDPH.AsQueryable().FirstOrDefault(i => i.Ico == key);
                        if (exist != null)
                        {
                            if (exist.FromDate.HasValue && newData[key].FromDate.HasValue &&
                                exist.FromDate != newData[key].FromDate)
                                exist.FromDate = newData[key].FromDate;

                            if (exist.ToDate.HasValue) //is back on the list, remove end
                                exist.ToDate = null;
                        }
                        else
                        {
                            var newItem = new NespolehlivyPlatceDPH()
                            {
                                Ico = newData[key].Ico,
                                FromDate = newData[key].FromDate
                            };
                            db.NespolehlivyPlatceDPH.Add(newItem);
                        }
                    }

                    db.SaveChanges();
                    //check ico removed from newData
                    var inDb = db.NespolehlivyPlatceDPH.AsQueryable().Where(m => m.ToDate == null).Select(m => m.Ico)
                        .ToArray();
                    var missingInNewData = inDb.Except(newData.Keys);
                    foreach (var ico in missingInNewData)
                    {
                        var exist = db.NespolehlivyPlatceDPH.AsQueryable().FirstOrDefault(i => i.Ico == ico);
                        if (exist != null && exist.ToDate == null)
                        {
                            exist.ToDate = DateTime.Now.Date;
                        }
                    }

                    db.SaveChanges();
                }
            }
        }
    }
}