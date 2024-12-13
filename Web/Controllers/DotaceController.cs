using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HlidacStatu.Entities;
using HlidacStatu.Entities.Entities;
using HlidacStatu.Repositories;

using Microsoft.AspNetCore.Mvc;
using Nest;

namespace HlidacStatu.Web.Controllers
{
    public class DotaceController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        public async Task<ActionResult> Hledat(Repositories.Searching.SubsidySearchResult model)
        {
            if (model == null || ModelState.IsValid == false)
            {
                return View(new Repositories.Searching.SubsidySearchResult());
            }

            var aggs = new Nest.AggregationContainerDescriptor<Subsidy>()
                .Sum("souhrn", s => s
                    .Field(f => f.AssumedAmount)

                );


            var res = await SubsidyRepo.Searching.SimpleSearchAsync(model, anyAggregation: aggs);

            AuditRepo.Add(
                Audit.Operations.UserSearch
                , User?.Identity?.Name
                , HlidacStatu.Util.RealIpAddress.GetIp(HttpContext)?.ToString()
                , "Subsidy"
                , res.IsValid ? "valid" : "invalid"
                , res.Q, res.OrigQuery);


            return View(res);
        }

        public async Task<ActionResult> Detail(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return Redirect("/dotace");
            }
            var dotace = await SubsidyRepo.GetAsync(id);
            if (dotace is null)
            {
                return NotFound();
            }
            return View(dotace);
        }
        
        public async Task<ActionResult> PoLetech()
        {
            AggregationContainerDescriptor<Subsidy> aggs = new AggregationContainerDescriptor<Subsidy>()
                .Terms("perYear", t => t
                    .Field(f => f.Common.ApprovedYear)
                    .Size(65500)
                    .Aggregations(yearAggs => yearAggs
                        .Terms("perSubsidyType", st => st
                            .Field(f => f.Hints.SubsidyType)
                            .Size(65500)
                            .Aggregations(typeAggs => typeAggs
                                .Sum("sumAssumedAmount", sa => sa
                                    .Field(f => f.AssumedAmount)
                                )
                            )
                        )
                    )
                );
            
            var dotace = await SubsidyRepo.Searching.SimpleSearchAsync("", 1, 0, "666", anyAggregation: aggs);
            if (dotace is null)
            {
                return NotFound();
            }

            // Initialize the results dictionary
            Dictionary<int, Dictionary<Subsidy.Hint.Type, decimal>> results = new();

            // Parse the aggregation results
            if (dotace.ElasticResults.Aggregations.Terms("perYear") is TermsAggregate<string> perYearTerms)
            {
                foreach (var yearBucket in perYearTerms.Buckets)
                {
                    // Parse the year from the bucket key
                    if (int.TryParse(yearBucket.Key, out int year))
                    {
                        // Initialize the inner dictionary
                        var subsidyTypeDict = new Dictionary<Subsidy.Hint.Type, decimal>();
                        
                        var typeBuckets = yearBucket["perSubsidyType"];
                        if (typeBuckets is BucketAggregate bucketAggregate)
                        {
                            foreach (var item in bucketAggregate.Items)
                            {
                                if (Enum.TryParse<Subsidy.Hint.Type>(item, out var subsidyType))
                                {
                                    //todo
                                
                                }
                            }
                        }
                        
                        foreach (var typeBucket in typeBuckets.Meta)
                        {
                            
                        }
                       

                        // Add to the results dictionary
                        results[year] = subsidyTypeDict;
                    }
                }
            }
            
            return View(dotace);
        }    
    }
}