using InsolvencniRejstrik.ByEvents;
using System;

namespace InsolvencniRejstrik.Fixes
{
	class FillInCache
	{
		private readonly WsClientCache Cache;

		public FillInCache(WsClientCache cache)
		{
			Cache = cache;
		}

		public void Execute()
		{
			var loaded = 0;
			var started = DateTime.Now;

			var lastId = Cache.GetLastIdInCache();
			foreach (var item in Cache.Get(lastId))
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
