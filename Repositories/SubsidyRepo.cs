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
        private static readonly ILogger Logger = Log.ForContext(typeof(SubsidyRepo));

        public static readonly ElasticClient SubsidyClient = Manager.GetESClient_SubsidyAsync()
            .ConfigureAwait(false).GetAwaiter().GetResult();
        

        //do not delete - it is used by another project
        public static async Task SaveAsync(Subsidy subsidy, bool shouldRewrite)
        {
            Logger.Debug($"Saving subsidy {subsidy.RecordNumber} from {subsidy.DataSource}/{subsidy.FileName}");
            if (subsidy is null) throw new ArgumentNullException(nameof(subsidy));

            if (!shouldRewrite)
            {
                // Check if subsidy already exists
                var existingSubsidy = await SubsidyClient.GetAsync<Subsidy>(subsidy.Id);

                //do not merge hidden one - they are to be replaced
                if (existingSubsidy.Found)
                {
                    subsidy = MergeSubsidy(existingSubsidy.Source, subsidy);
                }
            }

            var res = await SubsidyClient.IndexAsync(subsidy, o => o.Id(subsidy.Id));

            if (!res.IsValid)
            {
                Logger.Error($"Failed to save subsidy for {subsidy.Id}. {res.OriginalException.Message}");
                throw new ApplicationException(res.ServerError?.ToString());
            }
            Logger.Debug($"Subsidy {subsidy.RecordNumber} from {subsidy.DataSource}/{subsidy.FileName} saved");
            
            //todo: uncomment once ready for statistic recalculation
            // if(subsidy.Common.Recipient.Ico is not null)
            //     RecalculateItemRepo.AddFirmaToProcessingQueue(subsidy.Common.Recipient.Ico, Entities.RecalculateItem.StatisticsTypeEnum.Dotace, $"VZ {subsidy.Id}");
        }

        private static Subsidy MergeSubsidy(Subsidy oldRecord, Subsidy newRecord)
        {
            Logger.Information($"Merging subsidy for {oldRecord.Id}, from {oldRecord.DataSource}/{oldRecord.FileName}, records [{newRecord.RecordNumber}] and [{oldRecord.RecordNumber}]");
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
                var updateByQueryResponse = await SubsidyClient.UpdateByQueryAsync<Subsidy>(u => u
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
                    Logger.Error(updateByQueryResponse.OriginalException, $"Problem during setting hidden subsidies for {subsidiesToHideId},\n Debug info:{updateByQueryResponse.DebugInformation} ");
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
                var ids = SubsidyClient.SimpleGetAllIds(5, query);
                return ids.ToHashSet();
            }
            catch (Exception ex)
            {
                Logger.Fatal(ex, "Problem when loading ids for {datasource}/{fileName}. Aborting load", datasource, fileName);
                throw;
            }
        }
        
        public static async Task<List<Subsidy>> FindDuplicatesAsync(Subsidy subsidy)
        {
            
            // Build the conditional query for ProjectCode OR ProgramCode OR ProjectName
            QueryContainer projectQuery = null;

            if (!string.IsNullOrWhiteSpace(subsidy.Common.ProjectCode))
            {
                projectQuery |= new QueryContainerDescriptor<Subsidy>().Term(t => t.Common.ProjectCode, subsidy.Common.ProjectCode);
            }

            if (!string.IsNullOrWhiteSpace(subsidy.Common.ProgramCode))
            {
                projectQuery |= new QueryContainerDescriptor<Subsidy>().Term(t => t.Common.ProgramCode, subsidy.Common.ProgramCode);
            }

            if (!string.IsNullOrWhiteSpace(subsidy.Common.ProjectName))
            {
                projectQuery |= new QueryContainerDescriptor<Subsidy>().Term(t => t.Common.ProjectName, subsidy.Common.ProjectName);
            }

            // Build the main query
            var query = new QueryContainerDescriptor<Subsidy>()
                .Bool(b => b
                    .Must(
                        m => m.Term(t => t.Common.Recipient.Ico, subsidy.Common.Recipient.Ico),
                        m => m.Term(t => t.Common.ApprovedYear, subsidy.Common.ApprovedYear),
                        m => m.Term(t => t.AssumedAmount, subsidy.AssumedAmount),
                        m => projectQuery // Add the conditional query
                    )
                );

            try
            {
                var res = await SubsidyClient.SearchAsync<Subsidy>(s => s
                        .Size(5000)
                        .Query(q => query)
                );

                if (!res.IsValid) // try again
                {
                    await Task.Delay(500);
                    res = await SubsidyClient.SearchAsync<Subsidy>(s => s
                        .Size(5000)
                        .Query(q => query)
                    );  
                }
                
                return res.Documents.ToList();
                
            }
            catch (Exception ex)
            {
                Logger.Fatal(ex, $"Cant load duplicate subsidies for subsidyId={subsidy.Id}");
                throw;
            }
        }
        
        public static async Task<Subsidy> GetAsync(string idDotace)
        {
            if (idDotace == null) throw new ArgumentNullException(nameof(idDotace));

            var response = await SubsidyClient.GetAsync<Subsidy>(idDotace);

            return response.IsValid
                ? response.Source
                : null;
        }

    }
}