using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HlidacStatu.Connectors;
using HlidacStatu.Entities.Entities;
using HlidacStatu.Repositories.Searching;
using Serilog;

namespace HlidacStatu.Repositories
{
    public static partial class SubsidyRepo
    {
        private static readonly ILogger _logger = Log.ForContext(typeof(SubsidyRepo));

        private static readonly ElasticClient _subsidyClient = Manager.GetESClient_SubsidyAsync()
            .ConfigureAwait(false).GetAwaiter().GetResult();
        

        //do not delete - it is used by another project
        public static async Task SaveAsync(Subsidy subsidy, bool shouldRewrite)
        {
            _logger.Debug($"Saving subsidy {subsidy.RecordNumber} from {subsidy.DataSource}/{subsidy.FileName}");
            if (subsidy is null) throw new ArgumentNullException(nameof(subsidy));

            if (!shouldRewrite)
            {
                // Check if subsidy already exists
                var existingSubsidy = await _subsidyClient.GetAsync<Subsidy>(subsidy.Id);

                //do not merge hidden one - they are to be replaced
                if (existingSubsidy.Found)
                {
                    subsidy = MergeSubsidy(existingSubsidy.Source, subsidy);
                }
            }

            var res = await _subsidyClient.IndexAsync(subsidy, o => o.Id(subsidy.Id));

            if (!res.IsValid)
            {
                _logger.Error($"Failed to save subsidy for {subsidy.Id}. {res.OriginalException.Message}");
                throw new ApplicationException(res.ServerError?.ToString());
            }
            _logger.Debug($"Subsidy {subsidy.RecordNumber} from {subsidy.DataSource}/{subsidy.FileName} saved");
            
            //todo: uncomment once ready for statistic recalculation
            // if(subsidy.Common.Recipient.Ico is not null)
            //     RecalculateItemRepo.AddFirmaToProcessingQueue(subsidy.Common.Recipient.Ico, Entities.RecalculateItem.StatisticsTypeEnum.Dotace, $"VZ {subsidy.Id}");
        }

        private static Subsidy MergeSubsidy(Subsidy oldRecord, Subsidy newRecord)
        {
            _logger.Information($"Merging subsidy for {oldRecord.Id}, from {oldRecord.DataSource}/{oldRecord.FileName}, records [{newRecord.RecordNumber}] and [{oldRecord.RecordNumber}]");
            newRecord.RawData += ",\n" + oldRecord.RawData;
            newRecord.Common.SubsidyAmount += oldRecord.Common.SubsidyAmount;
            newRecord.Common.PayedAmount += oldRecord.Common.PayedAmount;
            newRecord.Common.ReturnedAmount += oldRecord.Common.ReturnedAmount;

            return newRecord;
        }

        public static async Task SetHiddenSubsidies(HashSet<string> subsidiesToHideId)
        {
            if(!subsidiesToHideId.Any())
                return;

            foreach (var subsidyToHideId in subsidiesToHideId)
            {
                var updateByQueryResponse = await _subsidyClient.UpdateByQueryAsync<Subsidy>(u => u
                    .Query(q => q
                        .Bool(b => b
                            .Must(
                                m => m.Term(t => t.Id, subsidyToHideId)
                            )
                        )
                    )
                    .Script(s => s
                        .Source("ctx._source.isHidden = true")
                    )
                );
    
                if (!updateByQueryResponse.IsValid)
                {
                    _logger.Error(updateByQueryResponse.OriginalException, $"Problem during setting hidden subsidies for {subsidiesToHideId},\n Debug info:{updateByQueryResponse.DebugInformation} ");
                }
            }
            
        }

        public static HashSet<string> GetAllIds(string datasource, string fileName)
        {
            var query = new QueryContainerDescriptor<Subsidy>()
                .Bool(b => b
                    .Must(
                        m => m.Term(t => t.DataSource, datasource),
                        m => m.Term(t => t.FileName, fileName)
                    )
                );

            try
            {
                var ids = _subsidyClient.SimpleGetAllIds(5, query);
                return ids.ToHashSet();
            }
            catch (Exception ex)
            {
                _logger.Fatal(ex, "Problem when loading ids for {datasource}/{fileName}. Aborting load", datasource, fileName);
                throw;
            }
        }
        
        public static async Task<Subsidy> GetAsync(string idDotace)
        {
            if (idDotace == null) throw new ArgumentNullException(nameof(idDotace));

            var response = await _subsidyClient.GetAsync<Subsidy>(idDotace);

            return response.IsValid
                ? response.Source
                : null;
        }

    }
}