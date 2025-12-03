using HlidacStatu.Entities.Insolvence;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace InsolvencniRejstrik.ByEvents
{
	class RepositoryCache : IRepository
	{
		private readonly IRepository UnderlyingRepository;

		private ConcurrentDictionary<string, Rizeni> RizeniCache = new ConcurrentDictionary<string, Rizeni>();

		public RepositoryCache(IRepository repository)
		{
			UnderlyingRepository = repository;
		}

		public async Task<Rizeni> GetInsolvencyProceedingAsync(string spisovaZnacka,
			Func<string, Rizeni> createNewInsolvencyProceeding) => 
			RizeniCache.GetOrAdd(spisovaZnacka, await UnderlyingRepository.GetInsolvencyProceedingAsync(spisovaZnacka, createNewInsolvencyProceeding) ?? createNewInsolvencyProceeding(spisovaZnacka));

		public Task SetInsolvencyProceedingAsync(Rizeni item) => UnderlyingRepository.SetInsolvencyProceedingAsync(item);
	}
}
