using HlidacStatu.Entities.Insolvence;
using System;
using System.Collections.Concurrent;

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

		public Rizeni GetInsolvencyProceeding(string spisovaZnacka, Func<string, Rizeni> createNewInsolvencyProceeding) => 
			RizeniCache.GetOrAdd(spisovaZnacka, UnderlyingRepository.GetInsolvencyProceeding(spisovaZnacka, createNewInsolvencyProceeding) ?? createNewInsolvencyProceeding(spisovaZnacka));

		public void SetInsolvencyProceeding(Rizeni item)  => UnderlyingRepository.SetInsolvencyProceeding(item);
	}
}
