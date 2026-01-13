using HlidacStatu.Entities;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace HlidacStatu.Repositories;

public class ObecStatsRepo
{

    public static async Task<ObecStats> LoadAsync(string obecIco)
    {

        var obecStats = new ObecStats();
        await using (DbEntities db = new DbEntities())
        {
            obecStats.Obec = await db.ObceZUJ.FirstOrDefaultAsync(m => m.Ico == obecIco);
            if (obecStats.Obec == null)
                return null;

            obecStats.Stats = db.ObceZUJAttr
                                .Where(m => m.Ico == obecIco)
                                .Join(db.ObceZUJAttrName,
                                attr => attr.Key, attrn => attrn.Key,
                                (attr, attrn) => new ObecStats.Attr()
                                {
                                    AttrName = attrn,
                                    Ico = attr.Ico,
                                    Key = attrn.Key,
                                    Year = attr.Year,
                                    Value = attr.Value
                                }
                                )
                                .ToArray();
                                //.ToQueryString();

        }

        return obecStats;
    }


}