using Elasticsearch.Net;
using HlidacStatu.Entities.Insolvence;
using HlidacStatu.Repositories.ES;
using System;
using System.Linq;

namespace InsolvencniRejstrik.ByEvents
{
	class Repository : IRepository
	{
		private readonly Stats Stats;

		public Repository(Stats stats)
		{
			Stats = stats;
		}

		public Rizeni GetInsolvencyProceeding(string spisovaZnacka, Func<string, Rizeni> createNewInsolvencyProceeding)
		{
			var res = Manager.GetESClient_InsolvenceAsync().Result
				.Search<Rizeni>(s => s
					.Size(1)
					.Query(q => q
					.Term(t => t.Field(f => f.SpisovaZnacka).Value(spisovaZnacka))
					)
				);
			if (res.IsValid)
			{
				Stats.InsolvencyProceedingGet++;
				return res.Hits.FirstOrDefault()?.Source ?? createNewInsolvencyProceeding(spisovaZnacka);
			}
			throw new ElasticsearchClientException(res.ServerError?.ToString());
		}

		public void SetInsolvencyProceeding(Rizeni item)
		{
			var res = Manager.GetESClient_InsolvenceAsync().Result.Index(item, o => o.Id(item.SpisovaZnacka.ToString())); //druhy parametr musi byt pole, ktere je unikatni
			if (!res.IsValid)
			{
				throw new ElasticsearchClientException(res.ServerError?.ToString());
			}
			HlidacStatu.Repositories.RizeniRepo.SaveAsync(item).RunSynchronously();
			Stats.InsolvencyProceedingSet++;
		}
	}
}
