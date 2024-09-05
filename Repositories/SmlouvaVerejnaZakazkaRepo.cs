using System.Threading.Tasks;
using HlidacStatu.Entities;
using HlidacStatu.Entities.Entities;

namespace HlidacStatu.Repositories;

public static class SmlouvaVerejnaZakazkaRepo
{
    public static async Task Upsert(SmlouvaVerejnaZakazka smlouvaVerejnaZakazka)
    {
        await using DbEntities db = new DbEntities();
        
        db.SmlouvyVerejneZakazky.Update(smlouvaVerejnaZakazka);

        // Save changes to the database
        await db.SaveChangesAsync();
        
    }
}