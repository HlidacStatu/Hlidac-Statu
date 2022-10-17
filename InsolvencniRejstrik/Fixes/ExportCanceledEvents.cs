using HlidacStatu.Entities.Insolvence;
using HlidacStatu.Repositories.ES;
using InsolvencniRejstrik.ByEvents;
using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace InsolvencniRejstrik.Fixes
{
	class ExportCanceledEvents
	{
		private int Lines = 0;
		private int ExportedEvents = 0;
		private int Added = 0;

		public void Execute(int skipLines, string cacheFile)
		{
			Console.WriteLine("Export zrusenych udalosti");
			if (skipLines > 0)
			{
				Console.WriteLine($"{skipLines} radek bude preskoceno");
			}
			Console.WriteLine();
			Console.WriteLine();

			File.WriteAllLines("_ExportCanceledeEvents.csv", new[] { $"EventId;SpisovaZnacka;IdOsoby;IdPuvodce;Role;Odstranen" });

			if (File.Exists(cacheFile))
			{
				foreach (var line in File.ReadLines(cacheFile))
				{
					var item = WsResult.From(line);
					Lines++;
					if (Lines < skipLines) continue;

					if (item.TypUdalosti == "1" || item.TypUdalosti == "2" || item.TypUdalosti == "625")
					{
						ExportedEvents++;
						ProcessEvent(item);
					}

					if (Lines % 100 == 0)
					{
						Console.CursorTop = Console.CursorTop - 1;
						Console.WriteLine($"Precteno {Lines} radku z toho {ExportedEvents} exportovanych udalosti (id udalosti: {item.Id})");
					}
				}
			}
		}

		private void ProcessEvent(WsResult item)
		{
			var osoba = ParseOsoba(XDocument.Parse(item.Poznamka));
			File.AppendAllLines("_ExportCanceledeEvents.csv", new[] { $"{item.Id};{item.SpisovaZnacka};{osoba.IdOsoby};{osoba.IdPuvodce};{osoba.Role};{osoba.Odstranen}" });
		}

		private Osoba ParseOsoba(XDocument doc)
		{
			var idPuvodce = IsirWsConnector.ParseValue(doc, "idOsobyPuvodce");
			var o = doc.Descendants("osoba").FirstOrDefault();
			var osobaId = IsirWsConnector.ParseValue(o, "idOsoby");
			var role = IsirWsConnector.ParseValue(o, "druhRoleVRizeni");
			var zrusena = IsirWsConnector.ParseValue(o, "datumOsobaVeVeciZrusena");
			DateTime odstranen = DateTime.MinValue;
			if (!DateTime.TryParse(zrusena, out odstranen))
			{
				Console.WriteLine("Nepodarilo se prevezt datum: " + zrusena);
			}
			
			return new Osoba { IdOsoby = osobaId, IdPuvodce = idPuvodce, Role = role, Odstranen = odstranen };
		}
	}
}
