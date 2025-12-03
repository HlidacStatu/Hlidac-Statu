using Elasticsearch.Net;
using HlidacStatu.Entities.Insolvence;
using HlidacStatu.Repositories;
using System;
using System.Linq;
using System.Threading.Tasks;
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

		public async Task<Rizeni> GetInsolvencyProceedingAsync(string spisovaZnacka,
			Func<string, Rizeni> createNewInsolvencyProceeding)
		{
			var insDet = await InsolvenceRepo.LoadFromEsAsync(spisovaZnacka, true, false);
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

		public async Task SetInsolvencyProceedingAsync(Rizeni item)
		{
			await HlidacStatu.Repositories.RizeniRepo.SaveAsync(item);
			Stats.InsolvencyProceedingSet++;
		}
	}
}
