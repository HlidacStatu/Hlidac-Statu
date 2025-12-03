using HlidacStatu.Entities.Insolvence;
using InsolvencniRejstrik.ByEvents;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HlidacStatu.Connectors;

namespace InsolvencniRejstrik.Fixes
{
	class MissingDocuments
	{
		private readonly IRepository Repository;
		private const int Threshold = 10000;

		public MissingDocuments(IRepository repository)
		{
			Repository = repository;
		}

		public async Task ExecuteAsync(string cacheFile)
		{
			if (!File.Exists("_invalid-document.log"))
			{
				Console.WriteLine("Nenalezen soubor '_invalid-document.log' v aktualnim adresari");
				return;
			}

			Console.WriteLine("Nacitani chybejicich dokumentu ...");
			var missingDocuments = new Dictionary<long, string>();
			foreach (var item in await File.ReadAllLinesAsync("_invalid-document.log"))
			{
				var parts = item.Split(';');
				missingDocuments.Add(Convert.ToInt64(parts[1]), parts[0]);
			}
			Console.WriteLine($"Nacteno {missingDocuments.Count} dokumentu");

			var start = DateTime.Now;

			if (File.Exists(cacheFile))
			{
				Console.WriteLine("Cteni dat z cache ...");
				var lines = 0;
				var savedDocuments = 0;

				await foreach (var line in File.ReadLinesAsync(cacheFile))
				{
					lines++;
					var item = WsResult.From(line);
					if (!string.IsNullOrEmpty(item.DokumentUrl))
					{
						if (missingDocuments.ContainsKey(item.Id))
						{
							var spisovaZnacka = missingDocuments[item.Id];
							var rizeni = LoadRizeni(spisovaZnacka);

							if (rizeni == null)
							{
								Console.WriteLine($"Nenalezeno rizeni se spisovou znackou {spisovaZnacka}");
							}
							else
							{
								var existsDocument = rizeni.Dokumenty.FirstOrDefault(d => Convert.ToInt64(d.Id) == item.Id);
								if (existsDocument != null)
								{
									Console.WriteLine($"Rizeni se spisovou znackou {spisovaZnacka} jiz obsahuje dokument s id {item.Id}");
								}
								else
								{
									var document = new Dokument
									{
										Id = item.Id.ToString(),
									};
									rizeni.Dokumenty.Add(document);

									var typUdalosti = -1;
									if (!int.TryParse(item.TypUdalosti, out typUdalosti))
									{
										Console.WriteLine($"Nepodarilo se naparserovat typ udalosti ({item.TypUdalosti})", item.Id);
									}
									document.TypUdalosti = typUdalosti;
									document.Url = item.DokumentUrl;
									document.DatumVlozeni = item.DatumZalozeniUdalosti;
									document.Popis = item.PopisUdalosti;
									document.Oddil = item.Oddil;

									if (rizeni.PosledniZmena < item.DatumZalozeniUdalosti)
									{
										rizeni.PosledniZmena = item.DatumZalozeniUdalosti;
									}

									await Repository.SetInsolvencyProceedingAsync(rizeni);
									savedDocuments++;
								}
							}
						}
					}

					if (lines % Threshold == 0)
					{
						var ts = DateTime.Now - start;
						Console.WriteLine($"Prectenych radku: {lines}, ulozenych dokumentu: {savedDocuments}, zpracovani {Threshold} radku: {TimeSpan.FromMilliseconds(ts.TotalMilliseconds * Threshold / lines)}");
					}
				}

			}
		}

		private Rizeni LoadRizeni(string spisovaZnacka)
		{
			var res = Manager.GetESClient_Insolvence().Get<Rizeni>(spisovaZnacka);
			return res.Found ? res.Source : null;
		}

	}
}
