using HtmlAgilityPack;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Text;

namespace InsolvencniRejstrik.ByEvents
{
	class IsirClientCache : IIsirClient
	{
		private readonly Stats GlobalStats;
		private readonly IIsirClient UnderlyingClient;
		private const string CacheFile = "isir_client_cache.csv";
		private readonly ConcurrentDictionary<string, string> CachedUrls = new ConcurrentDictionary<string, string>();
		private readonly ConcurrentDictionary<string, string> CachedSoudy = new ConcurrentDictionary<string, string>();

		public IsirClientCache(IIsirClient underlyingClient, Stats globalStats)
		{
			GlobalStats = globalStats;
			UnderlyingClient = underlyingClient;

			if (File.Exists(CacheFile))
			{
				foreach (var item in File.ReadLines(CacheFile))
				{
					var parts = item.Split(';');
					CachedUrls.TryAdd($"{parts[2]} {parts[3]}/{parts[4]}", "https://isir.justice.cz/isir/ueu/evidence_upadcu_detail.do?id=" + parts[8]);
					CachedSoudy.TryAdd($"{parts[2]} {parts[3]}/{parts[4]}", parts[0]);
				}
			}
		}

		public string GetUrl(string spisovaZnacka)
		{
			if (CachedUrls.TryGetValue(spisovaZnacka, out var url))
			{
				GlobalStats.LinkCacheCount++;
				return url;
			}
			else
			{
				var value = UnderlyingClient.GetUrl(spisovaZnacka);
				CachedUrls.TryAdd(spisovaZnacka, url);
				return value;
			}
		}

		public void PrepareCache(DateTime initialDate)
		{
			var client = new HtmlWeb();
			client.OverrideEncoding = Encoding.GetEncoding("Windows-1250");

			do
			{
				HtmlNode table;

				// retrying until get correct answer :)
				while (true)
				{
					try
					{
						table = MakeSearchRequest(client, initialDate);
						break;
					}
					catch (Exception e)
					{
						Console.WriteLine(e.Message);
					}
				}

				foreach (var row in table.Descendants("tr"))
				{
					var items = row.Descendants("td").ToArray();
					if (items.Any())
					{
						var data = new[] {
						/* 0 Soud */ items[0].InnerText.Trim(),
						/* 1 SoudniOddeleni */ items[1].InnerText.Trim(),
						/* 2 RejstrikovaZnacka */ items[2].InnerText.Trim(),
						/* 3 Cislo */ items[3].InnerText.Replace("/", "").Trim(),
						/* 4 Rocnik */ items[4].InnerText.Trim(),
						/* 5 Nazev */ items[7].InnerText.Trim(),
						/* 6 ICO */ items[8].InnerText.Trim(),
						/* 7 Rc */ items[9].InnerText.Trim(),
						/* 8 Id */ items[7].ChildNodes[1].Attributes["href"]?.Value?.Split(';')?.Skip(1)?.FirstOrDefault() };

						File.AppendAllLines(CacheFile, new[] { string.Join(";", data) });
					}
				}

				initialDate = initialDate.AddDays(14);
				Console.WriteLine(initialDate);
			} while (initialDate < DateTime.Now.Date);
			Console.WriteLine("Cache prednactena");
		}

		private HtmlNode MakeSearchRequest(HtmlWeb client, DateTime fromEndOfPeriodDate)
		{
			var content = client.Load($"https://isir.justice.cz/isir/ueu/vysledek_lustrace.do?aktualnost=AKTUALNI_I_UKONCENA&spis_znacky_datum={fromEndOfPeriodDate.ToString("dd.MM.yyyy")}&spis_znacky_obdobi=14DNI");
			return content.DocumentNode.Descendants("table").Where(t => t.Attributes["class"]?.Value == "vysledekLustrace").Skip(1).Single();
		}

	}
}
