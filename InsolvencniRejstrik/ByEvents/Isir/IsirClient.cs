using HtmlAgilityPack;
using System;
using System.Linq;
using System.Net;
using System.Text;

namespace InsolvencniRejstrik.ByEvents
{
	class IsirClient : IIsirClient
	{
		private readonly HtmlWeb Client;
		private readonly Stats GlobalStats;

		public IsirClient(Stats globalStats)
		{
			GlobalStats = globalStats;
			Client = new HtmlWeb();
			Client.OverrideEncoding = Encoding.GetEncoding("Windows-1250");
		}

		public string GetUrl(string spisovaZnacka)
		{
			var parts = spisovaZnacka.Split(new[] { '/', ' ' });
			var content = Client.Load($"https://isir.justice.cz/isir/ueu/vysledek_lustrace.do?bc_vec={parts[1]}&rocnik={parts[2]}&aktualnost=AKTUALNI_I_UKONCENA&rowsAtOnce=50");
			var linkElement = content.DocumentNode.Descendants("a").Where(l => l.InnerHtml.Trim() == "Detail").SingleOrDefault();
			if (linkElement != null)
			{
				GlobalStats.LinkCount++;
				return "https://isir.justice.cz/isir/ueu/evidence_upadcu_detail.do?id=" + WebUtility.HtmlDecode(linkElement.Attributes["href"].Value);
			}
			return null;
		}
	}
}
