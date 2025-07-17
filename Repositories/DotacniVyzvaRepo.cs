using Nest;
using System;
using System.Threading.Tasks;
using HlidacStatu.Connectors;
using HlidacStatu.Entities;
using Serilog;

namespace HlidacStatu.Repositories
{
    public static partial class DotacniVyzvaRepo
    {
        private static readonly ILogger Logger = Log.ForContext(typeof(DotacniVyzvaRepo));

        public static readonly ElasticClient DotacniVyzvaClient = Manager.GetESClient_Subsidy();

        public static async Task SaveAsync(DotacniVyzva dotacniVyzva, bool shouldRewrite = true)
        {
            Logger.Debug(
                $"Saving dotacniVyzva {dotacniVyzva.Id} from {dotacniVyzva.DataSource}/{dotacniVyzva.FileName}");
            if (dotacniVyzva is null) throw new ArgumentNullException(nameof(dotacniVyzva));

            if (!shouldRewrite)
            {
                // Check if subsidy already exists
                var existing = await DotacniVyzvaClient.GetAsync<DotacniVyzva>(dotacniVyzva.Id);
                if(existing is not null)
                    return;
            }

            dotacniVyzva.ModifiedDate = DateTime.Now;

            var res = await DotacniVyzvaClient.IndexAsync(dotacniVyzva, o => o.Id(dotacniVyzva.Id));

            if (!res.IsValid)
            {
                Logger.Error($"Failed to save dotacni vyzva for {dotacniVyzva.Id}. {res.OriginalException.Message}");
                throw new ApplicationException(res.ServerError?.ToString());
            }

            Logger.Debug(
                $"DotacniVyzva {dotacniVyzva.Id} from {dotacniVyzva.DataSource}/{dotacniVyzva.FileName} saved");
        }

        public static async Task<DotacniVyzva> GetAsync(string idDotacniVyzvy)
        {
            if (idDotacniVyzvy == null) throw new ArgumentNullException(nameof(idDotacniVyzvy));

            var response = await DotacniVyzvaClient.GetAsync<DotacniVyzva>(idDotacniVyzvy);

            return response.IsValid
                ? response.Source
                : null;
        }

        
    }
}