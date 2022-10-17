using InsolvencniRejstrik.ByEvents;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace InsolvencniRejstrik.Fixes
{
	class Data
	{
		public string Status { get; set; }
		public DateTime FirstSeen { get; set; }
		public DateTime LastSeen { get; set; }
	}

	class EventTypeToStatus
	{
		private int Lines = 0;
		private Dictionary<int, Data> Documents = new Dictionary<int, Data>();

		public void Execute(string cacheFile)
		{
			if (File.Exists(cacheFile))
			{
				foreach (var line in File.ReadLines(cacheFile))
				{
					try
					{
						var item = WsResult.From(line);
						var eventType = Convert.ToInt32(item.TypUdalosti);
						Lines++;

							if (!string.IsNullOrEmpty(item.Poznamka))
							{
								var xdoc = XDocument.Parse(item.Poznamka);
								var state = IsirWsConnector.ParseValue(xdoc.Descendants("vec").FirstOrDefault(), "druhStavRizeni");

							if (!Documents.ContainsKey(eventType))
							{
								Documents.Add(eventType, new Data { Status = state, FirstSeen = item.DatumZalozeniUdalosti, LastSeen = item.DatumZalozeniUdalosti });
							}
							else
							{
								Documents[eventType].LastSeen = item.DatumZalozeniUdalosti;
							}

						}

						if (Lines % 100_000 == 0)
						{
							Console.CursorTop = Console.CursorTop - 1;
							Console.WriteLine($"Precteno {Lines} radku a navazano {Documents.Count} udalosti (id udalosti: {item.Id})");
						}
					}
					catch (Exception e)
					{
						using (var stream = File.AppendText("_eventTypeToStatusErrors.log"))
						{
							stream.WriteLine(line);
							stream.Flush();
						}
						Console.WriteLine();
						Console.WriteLine($"ERROR: {e.Message}");
						Console.WriteLine();
						Console.WriteLine();
					}
				}

				File.WriteAllLines("event-status.csv", new[] { "Typ udalosti;Stav rizeni;Prvni vyskyt;Posledni vyskyt" }.Concat(Documents.OrderBy(k => k.Key).Select(d => $"{d.Key};{d.Value.Status};{d.Value.FirstSeen.ToShortDateString()};{d.Value.LastSeen.ToShortDateString()}")));
			}
		}
	}
}
