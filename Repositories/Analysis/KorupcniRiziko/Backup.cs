using HlidacStatu.Repositories.ES;

using System;
using System.Threading.Tasks;

namespace HlidacStatu.Lib.Analysis.KorupcniRiziko
{
    public class Backup
    {
        [Nest.Keyword]
        public string Id { get; set; }

        public DateTime Created { get; set; }

        public string Comment { get; set; }
        public KIndexData KIndex { get; set; }

        public static async Task CreateBackupAsync(string comment, string ico, bool useTempDb = false)
        {
            KIndexData kidx = await KIndexData.GetDirectAsync((ico, useTempDb));
            if (kidx == null)
                return;
            await CreateBackupAsync(comment, kidx, useTempDb);
        }
        public static async Task CreateBackupAsync(string comment, KIndexData kidx, bool useTempDb)
        {

            if (kidx == null)
                return;
            Backup b = new Backup();
            //calculate fields before saving
            b.Created = DateTime.Now;
            b.Id = $"{kidx.Ico}_{b.Created:s}";
            b.Comment = comment;
            b.KIndex = kidx;
            var client = await Manager.GetESClient_KIndexBackupAsync();
            if (useTempDb)
                client = await Manager.GetESClient_KIndexBackupTempAsync();

            var res = await client.IndexAsync<Backup>(b, o => o.Id(b.Id)); //druhy parametr musi byt pole, ktere je unikatni
            if (!res.IsValid)
            {
                Util.Consts.Logger.Error("KIndex backup save error\n" + res.ServerError?.ToString());
                throw new ApplicationException(res.ServerError?.ToString());
            }
        }

    }
}
