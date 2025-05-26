using Elasticsearch.Net;
using HlidacStatu.Entities.Insolvence;
using HlidacStatu.Repositories;
using System;
using System.Linq;
using HlidacStatu.Connectors;

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
			var insDet = InsolvenceRepo.LoadFromEsAsync(spisovaZnacka, true, false).ConfigureAwait(false).GetAwaiter().GetResult();
			if (insDet == null || insDet?.Rizeni == null)
				return createNewInsolvencyProceeding(spisovaZnacka);
			else
				return insDet.Rizeni;
        }


        public Rizeni GetInsolvencyProceeding_old(string spisovaZnacka, Func<string, Rizeni> createNewInsolvencyProceeding)
		{
			var res = Manager.GetESClient_Insolvence()
				.Search<Rizeni>(s => s
					.Size(1)
					.Query(q => q
					.Term(t => t.Field(f => f.SpisovaZnacka).Value(spisovaZnacka))
					)
				);
			if (res.IsValid)
			{
				Stats.InsolvencyProceedingGet++;
				var riz = res.Hits.FirstOrDefault()?.Source;
				if (riz == null)
					return createNewInsolvencyProceeding(spisovaZnacka);
				riz.IsFullRecord = true;
				return riz;
			}
			throw new ElasticsearchClientException(res.ServerError?.ToString());
		}

		public void SetInsolvencyProceeding(Rizeni item)
		{
			//var res = Manager.GetESClient_InsolvenceAsync().ConfigureAwait(false).GetAwaiter().GetResult().Index(item, o => o.Id(item.SpisovaZnacka.ToString())); //druhy parametr musi byt pole, ktere je unikatni
			//if (!res.IsValid)
			//{
			//	throw new ElasticsearchClientException(res.ServerError?.ToString());
			//}
			HlidacStatu.Repositories.RizeniRepo.SaveAsync(item).ConfigureAwait(false).GetAwaiter().GetResult();
			Stats.InsolvencyProceedingSet++;
		}
	}
}
