using InsolvencniRejstrik.ByEvents;
using System;
using System.Threading.Tasks;

namespace InsolvencniRejstrik.Fixes
{
	class FillInCache
	{
		private readonly WsClientCache Cache;

		public FillInCache(WsClientCache cache)
		{
			Cache = cache;
		}

		public async Task ExecuteAsync()
		{
			var loaded = 0;
			var started = DateTime.Now;

			var lastId = Cache.GetLastIdInCache();
			await foreach (var item in Cache.GetAsync(lastId))
			{
				loaded++;

				if (loaded % 1000 == 0)
				{
					Console.WriteLine($"Doplneno {loaded} udalosti v case {DateTime.Now - started} (zacatek: {lastId}, posledni: {item.Id})");
					started = DateTime.Now;
				}
			}
		}
	}
}
