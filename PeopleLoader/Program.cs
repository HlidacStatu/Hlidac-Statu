using Devmasters.Collections;

using HlidacStatu.Entities;
using HlidacStatu.Entities.OsobyES;
using HlidacStatu.Extensions;
using HlidacStatu.Repositories;

using System.Collections.Generic;
using System.Linq;

namespace PeopleLoader
{
    class Program
    {

        public static Devmasters.Batch.ActionProgressWriter progressWriter =
                new Devmasters.Batch.ActionProgressWriter(0.1f, HlidacStatu.XLib.RenderTools.ProgressWriter_OutputFunc_EndIn);

        static void Main(string[] args)
        {
            //List<OsobaES> osoby = null;
            //OsobyEsService.Get("aaa");
            System.Globalization.CultureInfo.DefaultThreadCurrentCulture = HlidacStatu.Util.Consts.czCulture;
            System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = HlidacStatu.Util.Consts.czCulture;

            using (DbEntities db = new DbEntities())
            {
                System.Console.WriteLine("Loading all records from db");

                var osoby = db.Osoba.AsQueryable()
                    .Where(m => m.Status > 0)
                    .ToList();

                //first fix people where is missing osoba.nameid
                foreach (var osoba in osoby.Where(o => o.NameId == null || o.NameId.Length < 1))
                {
                    OsobaRepo.Save(osoba);
                }

                System.Console.WriteLine("Converting all records");
                List<OsobaES> osobyES = new List<OsobaES>();
                Devmasters.Batch.Manager.DoActionForAll<Osoba>(osoby,
                os =>
                {
                    var o = new OsobaES()
                    {
                        NameId = os.NameId,
                        BirthYear = os.Narozeni.HasValue ? (int?)os.Narozeni.Value.Year : null,
                        DeathYear = os.Umrti.HasValue ? (int?)os.Umrti.Value.Year : null,
                        ShortName = os.Jmeno + " " + os.Prijmeni,
                        FullName = os.FullName(false),
                        PoliticalParty = os.CurrentPoliticalParty(),
                        StatusText = os.StatusOsoby().ToString("G"),
                        Status = os.Status,
                        PoliticalFunctions = os.Events(ev => ev.Type == (int)OsobaEvent.Types.VolenaFunkce)
                            .Select(ev => ev.AddInfo).ToArray(),
                        PhotoUrl = os.HasPhoto() ? os.GetPhotoUrl() : null

                    };
                    osobyES.Add(o);

                    return new Devmasters.Batch.ActionOutputData();
                }, null, progressWriter.Writer, true, maxDegreeOfParallelism: 10);

                System.Console.WriteLine("Deleting all records");
                OsobyEsRepo.DeleteAll();
                foreach (var osoba in osobyES.Chunk(1000))
                {
                    System.Console.WriteLine($"Adding {osoba.Count()} records");
                    OsobyEsRepo.BulkSave(osoba);
                }
            }
        }
    }
}
