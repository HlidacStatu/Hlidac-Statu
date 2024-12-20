//didnt work because of too many indexed properties in ES

// using System.Collections.Generic;
//
// namespace HlidacStatu.Entities;
//
// public partial class Subsidy
// {
//     public class RawData
//     {
//         [Nest.Keyword]
//         public string Id { get; set; }
//
//         [Nest.Object]
//         public List<Dictionary<string, object?>> Items { get; set; } = new();
//     }
// }

//follows method and another code from other classes..

// public static async Task SaveRawDataAsync(string subsidyId, Dictionary<string, object?> data, bool shouldRewrite)
// {
//     var newRawData = new Subsidy.RawData()
//     {
//         Id = subsidyId
//     };
//     newRawData.Items.Add(data);
//             
//     if (!shouldRewrite) //merge
//     {
//         // Check if raw data already exists
//         var existingRawData = await GetRawDataAsync(subsidyId);
//
//         if (existingRawData is not null && existingRawData.Items.Any())
//         {
//             newRawData.Items.AddRange(existingRawData.Items);
//         }
//     }
//
//     string updatedData = JsonSerializer.Serialize(newRawData, 
//         new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase});
//
//     // string updatedData = JsonConvert.SerializeObject(newRawData)
//     //     .Replace((char)160, ' '); //hard space to space
//     PostData pd = PostData.String(updatedData);
//
//     var tres = await SubsidyRawDataClient.LowLevel.IndexAsync<StringResponse>(SubsidyRawDataClient.ConnectionSettings.DefaultIndex, subsidyId, pd);
//
//     if (!tres.Success)
//     {
//         Logger.Error($"Problem durning {nameof(SaveRawDataAsync)} - {tres.DebugInformation}");
//     }
// }


// public static async Task<Subsidy.RawData> GetRawDataAsync(string subsidyId)
// {
//     if (string.IsNullOrEmpty(subsidyId)) 
//         throw new ArgumentNullException(nameof(subsidyId));
//
//     var response = await SubsidyRawDataClient.GetAsync<Subsidy.RawData>(subsidyId);
//
//     return response.IsValid
//         ? response.Source
//         : null;
//
// }

//await SubsidyRepo.SaveRawDataAsync(context.Subsidy.Id, context.Input, shouldRewrite: shouldRewrite);

// case IndexType.SubsidyRawData:
// res = await client.Indices
// .CreateAsync(indexName, i => i
// .InitializeUsing(idxSt)
// .Map<Entities.Subsidy.RawData>(map => map.AutoMap().DateDetection(false))
// );
// break;

// public static Task<ElasticClient> GetESClient_SubsidyRawDataAsync(int timeOut = 60000, int connectionLimit = 80)
// {
//     return GetESClientAsync(defaultIndexName_SubsidyRawData, timeOut, connectionLimit, IndexType.SubsidyRawData);
// }