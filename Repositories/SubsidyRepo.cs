using Nest;
using System;
using System.Threading.Tasks;
using HlidacStatu.Connectors;
using HlidacStatu.Entities.Entities;
using Serilog;

namespace HlidacStatu.Repositories
{
    public static partial class SubsidyRepo
    {
        private static readonly ILogger _logger = Log.ForContext(typeof(SubsidyRepo));

        private static readonly ElasticClient _subsidyClient = Manager.GetESClient_DotaceAsync()
            .ConfigureAwait(false).GetAwaiter().GetResult();
        

        //do not delete - it is used by another project
        public static async Task SaveAsync(Subsidy subsidy)
        {
            if (subsidy is null) throw new ArgumentNullException(nameof(subsidy));

            // Check if subsidy already exists
            var existingSubsidy = await _subsidyClient.GetAsync<Subsidy>(subsidy.Id);

            if (existingSubsidy.Found)
            {
                // If the existing subsidy is not hidden and trying to overwrite, throw an exception
                if (existingSubsidy.Source.IsHidden == false)
                {
                    var errorMessage = $"Attempt to overwrite non-hidden subsidy with Id: {subsidy.Id}. " +
                        $"ExistingSubsidy: DataSource [{existingSubsidy.Source.DataSource}] " +
                        $"FileName [{existingSubsidy.Source.FileName}] " +
                        $"RecordNumber [{existingSubsidy.Source.RecordNumber}];" +
                        $"NewSubsidy: DataSource [{subsidy.DataSource}] " +
                        $"FileName [{subsidy.FileName}] " +
                        $"RecordNumber [{subsidy.RecordNumber}];";
                    _logger.Error(errorMessage);
                    throw new InvalidOperationException(errorMessage);
                }
            }

            var res = await _subsidyClient.IndexAsync(subsidy, o => o.Id(subsidy.Id));

            if (!res.IsValid)
            {
                _logger.Error($"Failed to save subsidy for {subsidy.Id}. {res.OriginalException.Message}");
                throw new ApplicationException(res.ServerError?.ToString());
            }

            RecalculateItemRepo.AddFirmaToProcessingQueue(subsidy.Common.Recipient.Ico, Entities.RecalculateItem.StatisticsTypeEnum.Dotace, $"VZ {subsidy.Id}");
        }
        
        public static async Task SetHiddenSubsidies(string fileName, string source)
        {
            var updateByQueryResponse = await _subsidyClient.UpdateByQueryAsync<Subsidy>(u => u
                .Query(q => q
                    .Bool(b => b
                        .Must(
                            m => m.Term(t => t.FileName, fileName),
                            m => m.Term(t => t.DataSource, source)
                        )
                    )
                )
                .Script(s => s
                    .Source("ctx._source.isHidden = true")
                )
            );
    
            if (!updateByQueryResponse.IsValid)
            {
                throw new Exception(updateByQueryResponse.OriginalException.Message);
            }
        }
        

    }
}