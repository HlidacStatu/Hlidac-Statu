using HlidacStatu.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

namespace HlidacStatu.Repositories
{
    public static class FirmaVlastnenaStatemRepo
    {

        //vcetne strategickych podniku z https://www.mfcr.cz/cs/aktualne/tiskove-zpravy/2020/vlada-schvalila-strategii-vlastnicke-pol-37573
        public static string[] StatniFirmyICO = new string[]
        {
            "00000205", "00000337", "00000345", "00000370", "00000493", "00000515", "00001279", "00001481", "00001490",
            "00002321", "00002674", "00002691",
            "00002739", "00003026", "00007536", "00008141", "00008184", "00009181", "00009334", "00009393", "00009563",
            "00010669", "00011011", "00012033",
            "00012343", "00013251", "00013455", "00014079", "00014125", "00014818", "00015156", "00015270", "00015296",
            "00015415", "00015679", "00024007",
            "00076791", "00086932", "00128201", "00157287", "00157325", "00251976", "00311391", "00514152", "00565253",
            "00577880", "00659819", "00664073",
            "03630919", "13695673", "14450216", "14450241", "14867770", "14888025", "15503852", "17047234", "24272523",
            "24729035", "24821993", "24829871",
            "25059386", "25085531", "25125877", "25255843", "25291581", "25401726", "25634160", "25674285", "25702556",
            "25938924", "26051818", "26162539",
            "26175291", "26206803", "26376547", "26463318", "26470411", "26840065", "26871823", "27145573", "27146235",
            "27195872", "27232433", "27257258",
            "27257517", "27309941", "27364976", "27378225", "27772683", "27786331", "27804721", "27892646", "28196678",
            "28244532", "28255933", "28267141",
            "28707052", "28786009", "28861736", "29372259", "42196451", "43833560", "44269595", "44848943", "45144419",
            "45147965", "45193070", "45273375",
            "45273448", "45274649", "45274827", "45279314", "45534268", "45795908", "46355901", "46504818", "46708707",
            "47114983", "47115726", "47673354",
            "47677543", "48204285", "48291749", "48535591", "49241494", "49241672", "49453866", "49454561", "49710371",
            "49901982", "49973720", "60193468",
            "60193531", "60196696", "60197901", "60698101", "61459445", "61860336", "62413376", "63078333", "63080249",
            "70889953", "70889988", "70890005",
            "70890013", "70890021", "70994226", "70994234"
        };

        public static int[] StatniFirmy_BasedKodPF = new int[]
        {
            301, 302, 312, 313, 314, 325, 331, 352, 353, 361, 362, 381, 382, 521, 771, 801, 804, 805
        };

        /*
         * https://wwwinfo.mfcr.cz/ares/aresPrFor.html.cz
         * KOD_PF
         *
301	Státní podnik
302	Národní podnik
312	Banka-státní peněžní ústav
313	Česká národní banka
314	Česká konsolidační agentura
325	Organizační složka státu
331	Příspěvková organizace
352	Správa železniční dopravní cesty, státní organizace
353	Rada pro veřejný dohled nad auditem
361	Veřejnoprávní instituce (ČT,ČRo,ČTK)
362	Česká tisková kancelář
381	Fond (ze zákona)
382	Státní fond ze zákona

521	Samostatná drobná provozovna obecního úřadu
771	Svazek obcí
801	Obec nebo městská část hlavního města Prahy
804	Kraj
805	Regionální rada regionu soudržnosti

 */



        public static List<string> IcaStatnichFirem(int statniPodil)
        {
            using (DbEntities db = new DbEntities())
            {
                return db.FirmyVlastneneStatem
                    .AsNoTracking()
                    .Where(m => m.Podil >= statniPodil)
                    .Select(m => m.Ico)
                    .ToList();
            }
        }

        public static List<string> IcaStatnichFirem()
        {
            using (DbEntities db = new DbEntities())
            {
                return db.FirmyVlastneneStatem
                    .AsNoTracking()
                    .Select(m => m.Ico)
                    .ToList()
                    //.Union(
                    ;
            }
        }
        

        public static void Repopulate(IEnumerable<FirmaVlastnenaStatem> percList)
        {
            using (DbEntities db = new DbEntities())
            {
                db.Database.ExecuteSqlRaw("TRUNCATE TABLE [FirmyVlastneneStatem]");
                db.FirmyVlastneneStatem.AddRange(percList);
                db.SaveChanges();
            }
            
        }
    }
}
