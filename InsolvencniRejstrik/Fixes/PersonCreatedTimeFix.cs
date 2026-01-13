using HlidacStatu.Entities.Insolvence;
using InsolvencniRejstrik.ByEvents;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using HlidacStatu.Connectors;

namespace InsolvencniRejstrik.Fixes
{
	class PersonCreatedTimeFix
	{
		private DateTime Start;
		private int Lines = 0;
		private int People = 0;
		private int Saved = 0;

		private string[] PersonEventTypes = new[] { "1", "2", "625" };

		private Dictionary<string, Dictionary<PersonId, DateTime>> Processings = new Dictionary<string, Dictionary<PersonId, DateTime>>();
		private Repository Repository = new Repository(new Stats());

		class PersonId
		{
			public string IdPuvodce { get; set; }
			public string OsobaId { get; set; }

			public override string ToString() {
				return $"{IdPuvodce}###{OsobaId}";
			}

			public override bool Equals(object value)
			{
				if (ReferenceEquals(null, value)) return false;
				if (ReferenceEquals(this, value)) return true;
				if (value.GetType() != GetType()) return false;

				var id = (PersonId)value;
				return string.Equals(IdPuvodce, id.IdPuvodce) && string.Equals(OsobaId, id.OsobaId);
			}

			public override int GetHashCode()
			{
				unchecked
				{
					// Choose large primes to avoid hashing collisions
					const int HashingBase = (int)2166136261;
					const int HashingMultiplier = 16777619;

					int hash = HashingBase;
					hash = (hash * HashingMultiplier) ^ (!Object.ReferenceEquals(null, IdPuvodce) ? IdPuvodce.GetHashCode() : 0);
					hash = (hash * HashingMultiplier) ^ (!Object.ReferenceEquals(null, OsobaId) ? OsobaId.GetHashCode() : 0);
					return hash;
				}
			}
		}

		public async Task ExecuteAsync(string cacheFile)
		{
			var readingThreshold = 100000;
			var savingThreshold = 1000;

			Console.WriteLine("Doplneni casu pridani osoby k rizeni");
			Console.WriteLine();
			Start = DateTime.Now;

			if (File.Exists(cacheFile))
			{
				Console.WriteLine("Cteni dat z cache ...");

				await foreach (var line in File.ReadLinesAsync(cacheFile))
				{
					var item = WsResult.From(line);
					Lines++;

					if (PersonEventTypes.Contains(item.TypUdalosti))
					{
						ProcessEvent(item);
					}

					if (Lines % readingThreshold == 0)
					{
						var ts = DateTime.Now - Start;
						Console.WriteLine($"Radku: {Lines}, rizeni: {Processings.Count}, osob: {People}, zpracovani {readingThreshold} radku: {TimeSpan.FromMilliseconds(ts.TotalMilliseconds * readingThreshold / Lines)}");
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
						Console.WriteLine();
						Console.WriteLine($"Nepodarilo se nalezt rizeni {item.Key}");
						Console.WriteLine();
						continue;
					}

					foreach (var doc in item.Value)
					{
						var person = rizeni.Dluznici.FirstOrDefault(d => d.IdPuvodce == doc.Key.IdPuvodce && d.OsobaId == doc.Key.OsobaId)
							?? rizeni.Veritele.FirstOrDefault(v => v.IdPuvodce == doc.Key.IdPuvodce && v.OsobaId == doc.Key.OsobaId)
							?? rizeni.Spravci.FirstOrDefault(s => s.IdPuvodce == doc.Key.IdPuvodce && s.OsobaId == doc.Key.OsobaId);

						if (person == null)
						{
							Console.WriteLine();
							Console.WriteLine($"Nepodarilo se nalezt osobu s id {doc.Key} v rizeni {item.Key}");
							Console.WriteLine();
							continue;
						}

						person.Zalozen = doc.Value;
					}

					await Repository.SetInsolvencyProceedingAsync(rizeni);
                    await HlidacStatu.Repositories.RizeniRepo.SaveAsync(rizeni);
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
				Processings.Add(item.SpisovaZnacka, new Dictionary<PersonId, DateTime>());
			}
			var processing = Processings[item.SpisovaZnacka];

			var xdoc = XDocument.Parse(item.Poznamka);
			var idPuvodce = IsirWsConnector.ParseValue(xdoc, "idOsobyPuvodce");
			var o = xdoc.Descendants("osoba").FirstOrDefault();
			var osobaId = IsirWsConnector.ParseValue(o, "idOsoby");
			var id = new PersonId { IdPuvodce = idPuvodce, OsobaId = osobaId };

			if (!processing.ContainsKey(id))
			{
				processing.Add(id, item.DatumZalozeniUdalosti);
				People++;
			}
		}
	}
}
