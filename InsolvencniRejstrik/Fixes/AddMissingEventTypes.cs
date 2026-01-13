using HlidacStatu.Entities.Insolvence;
using InsolvencniRejstrik.ByEvents;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HlidacStatu.Connectors;

namespace InsolvencniRejstrik.Fixes
{
	class AddMissingEventTypes
	{
		private int Lines = 0;
		private int Saved = 0;
		private int Documents = 0;
		private int InvalidEventType = 0;
		private Dictionary<string, Dictionary<long, int>> Processings = new Dictionary<string, Dictionary<long, int>>();
		private Repository Repository = new Repository(new Stats());
		private DateTime Start;


		public async Task ExecuteAsync(string cacheFile)
		{
			var readingThreshold = 100000;
			var savingThreshold = 1000;
			Console.WriteLine("Doplneni chybejicich typu udalosti u dokumentu");
			Console.WriteLine();
			Start = DateTime.Now;

			if (File.Exists(cacheFile))
			{
				Console.WriteLine("Cteni dat z cache ...");

				await foreach (var line in File.ReadLinesAsync(cacheFile))
				{
					var item = WsResult.From(line);
					Lines++;
					if (!string.IsNullOrEmpty(item.DokumentUrl))
					{
						Documents++;
						ProcessEvent(item);
					}

					if (Lines % readingThreshold == 0)
					{
						var ts = DateTime.Now - Start;
						Console.WriteLine($"Radku: {Lines}, rizeni: {Processings.Count}, dokumentu: {Documents}, nevalidnich typu {InvalidEventType}, zpracovani {readingThreshold} radku: {TimeSpan.FromMilliseconds(ts.TotalMilliseconds * readingThreshold / Lines)}");
					}
				}

				Console.WriteLine("Generovani a ukladani vystupniho json souboru ...");
				await File.WriteAllTextAsync("result.json", JsonConvert.SerializeObject(Processings));

				Console.WriteLine("Ukladani ...");
				Start = DateTime.Now;
				foreach (var item in Processings)
				{
					if (Saved > 0 && Saved % savingThreshold == 0)
					{
						var ts = DateTime.Now - Start;
						Console.WriteLine($"Ulozeno {Saved} z {Processings.Count} ({TimeSpan.FromMilliseconds(ts.TotalMilliseconds * (Processings.Count - Saved))})");
					}

					Saved++;
					var rizeni = LoadRizeni(item.Key);
					if (rizeni == null)
					{
						using (var stream = File.AppendText("_missing-proceeding.log"))
						{
							await stream.WriteLineAsync(item.Key);
							await stream.FlushAsync();
						}

						Console.WriteLine();
						Console.WriteLine($"Nepodarilo se nalezt rizeni {item.Key}");
						Console.WriteLine();
						continue;
					}

					foreach (var doc in item.Value)
					{
						var document = rizeni.Dokumenty.FirstOrDefault(d => d.Id == doc.Key.ToString());
						if (document == null)
						{
							using (var stream = File.AppendText("_invalid-document.log"))
							{
								await stream.WriteLineAsync($"{item.Key};{doc.Key}");
								await stream.FlushAsync();
							}

							Console.WriteLine();
							Console.WriteLine($"Nepodarilo se nalezt dokument s id {doc.Key} v rizeni {item.Key}");
							Console.WriteLine();
							continue;
						}

						document.TypUdalosti = doc.Value;
					}

					await Repository.SetInsolvencyProceedingAsync(rizeni);
				}

				Console.WriteLine("Hotovo");
			}
		}

		private Rizeni LoadRizeni(string spisovaZnacka)
		{
			var res = Manager.GetESClient_Insolvence().Get<Rizeni>(spisovaZnacka);
			return res.Found ? res.Source : null;
		}

		private void ProcessEvent(WsResult item)
		{
			if (!Processings.ContainsKey(item.SpisovaZnacka))
			{
				Processings.Add(item.SpisovaZnacka, new Dictionary<long, int>());
			}
			var processing = Processings[item.SpisovaZnacka];

			var eventType = -1;
			if (!int.TryParse(item.TypUdalosti, out eventType))
			{
				using (var stream = File.AppendText("_invalid-event-type.log"))
				{
					stream.WriteLine($"{item.Id};{item.TypUdalosti};{item.SpisovaZnacka}");
					stream.Flush();
				}

				Console.WriteLine();
				Console.WriteLine($"Nepodarilo se naparserovat typ udalosti ({item.TypUdalosti}) - {item.Id}");
				Console.WriteLine();
			}

			if (eventType >= 0)
			{
				if (!processing.ContainsKey(item.Id))
				{
					processing.Add(item.Id, eventType);
				}
				else
				{
					Console.WriteLine();
					Console.WriteLine($"Prepisuji typ udalosti u dokumentu {item.Id} z {processing[item.Id]} na {item.TypUdalosti}");
					Console.WriteLine();

					processing[item.Id] = eventType;
				}
			}
		}
	}
}
