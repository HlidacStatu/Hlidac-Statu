using HlidacStatu.Entities;

using System.Linq;

namespace HlidacStatu.Repositories
{
    public static class OsobaExternalRepo
    {
        public static void Add(OsobaExternalId externalId)
        {
            if (externalId == null)
                return;
            Add(externalId.OsobaId, externalId.ExternalId, (OsobaExternalId.Source)externalId.ExternalSource);
        }
        public static void Add(int osobaId, string externalId, OsobaExternalId.Source externalsource)
        {
            using (DbEntities db = new DbEntities())
            {
                var exist = db.OsobaExternalId
                    .AsQueryable()
                    .FirstOrDefault(m => m.OsobaId == osobaId && m.ExternalId == externalId && m.ExternalSource == (int)externalsource);
                if (exist == null)
                {
                    OsobaExternalId oei = new OsobaExternalId(osobaId, externalId, externalsource);
                    db.OsobaExternalId.Add(oei);
                    db.SaveChanges();
                }
            }
        }
    }
}