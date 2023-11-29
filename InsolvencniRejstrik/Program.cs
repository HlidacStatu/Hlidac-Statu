﻿using InsolvencniRejstrik.ByEvents;
using NDesk.Options;
using System;
using System.IO;
using Newtonsoft.Json;
using InsolvencniRejstrik.Fixes;
using HlidacStatu.Entities.Insolvence;
 using HlidacStatu.LibCore.Extensions;

 namespace InsolvencniRejstrik
{
	class Program
	{
		static void Main(string[] args)
		{
            var cfg = HlidacConfigExtensions.InitializeConsoleConfiguration(args, "downloader");
            Devmasters.Config.Init(cfg);

            
			System.Globalization.CultureInfo.DefaultThreadCurrentCulture = HlidacStatu.Util.Consts.czCulture;
			System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = HlidacStatu.Util.Consts.czCulture;
			

			
			Console.WriteLine("HlidacStatu - Insolvencni rejstrik  v." + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString());
			Console.WriteLine("----------------------------------------------");
			Console.WriteLine();

			var events = false;
			var noCache = false;
			var initCache = false;
			var help = false;
			var toEventId = -1;
			var toFiles = false;
			var fromFiles = false;
			var only625fix = false;
            var only636fix = false;
            var onlyEventTypeFix = false;
			var onlyMissingDocumentsFix = false;
			var onlyCacheFix = false;
			var skipLines = 0;
			var reloadFailed = false;
			var personCreatedTimeFix = false;
			var eventTypeToStatus = false;
			var cacheFile = "ws_client_cache.csv";
			var export = "";
			var exportCanceledEvents = false;

			var options = new OptionSet() {
				{ "e|events", "cte a zpracovava udalosti insolvencniho rejstriku", v => events = true },
				{ "no-cache", "vypina ukladani event do cache a jejich nasledne pouziti", v => noCache = true },
				{ "to-event-id=", "nastavuje id udalosti, po ktere dojde k ukonceni zpracovani", v => toEventId = Convert.ToInt32(v) },
				{ "init-link-cache", "nacte seznam vsech rizeni a linku na jejich detail a ulozi je do souboru, ktery je pouzit pri naplneni cache linku na detail rizeni", v => initCache = true },
				{ "to-files", "uklada data do souboru namisto do databaze", v => toFiles = true},
				{ "from-files", "cte data ze souboru a uklada je do databaze", v => fromFiles = true},
				{ "625-fix", "oprava udalosti 625", v => only625fix = true},
                { "636-fix", "znepristupneni odstranenych", v => only636fix= true},
                { "event-type-fix", "doplneni chybejicich typu udalosti u dokumentu", v => onlyEventTypeFix = true},
				{ "missing-documents-fix", "doplneni chybejicich dokumentu", v => onlyMissingDocumentsFix = true },
				{ "cache-fix", "doplneni chybejicich zaznamy v cache", v => onlyCacheFix = true },
				{ "reload-failed", "zkusi znovu vlozit udalosti, ktere se nepodarilo ulozit", v => reloadFailed = true },
				{ "person-created-fix", "k osobam doplni datum pridani k rizeni", v => personCreatedTimeFix = true },
				{ "event-type-to-status", "vytvori mapovani mezi typem udalosti a stavem insolvence", v => eventTypeToStatus = true },
				{ "skip-lines=", "preskoci prvnich X radku", v => skipLines = Convert.ToInt32(v)},
				{ "cache-file=", "nastavi cestu k souboru cache udalosti", v => cacheFile = v},
				{ "export=", "vyexportuje vsechny udalosti pro zadana rizeni do samostatnych souboru", v => export = v},
				{ "export-canceled-events", "vyexportuje vsechny udalosti, ktere rusi predchozi udalost", v => exportCanceledEvents = true },
				{ "h|?|help", "zobrazi napovedu", v => help = true },
			};
			options.Parse(args);

			if (help)
			{
				PrintHelp(options);
			}
			else if (initCache)
			{
				Console.WriteLine("Spousti se prednacteni cache (vypsany datum znaci stazene obdobi)");
				new IsirClientCache(null, null).PrepareCache(new DateTime(2008, 1, 1));
			}
			else if (events)
			{
				var stats = new Stats();
				IRepository repository = null;
				IEventsRepository eventsRepository = null;
				IWsClient wsClient = null;

				if (toFiles)
				{
					repository = new FileRepository();
				}
				else
				{
					repository = noCache ? (IRepository)new Repository(stats) : new RepositoryCache(new Repository(stats));
				}
				if (reloadFailed)
				{
					wsClient = new FailedResultsClient();
					eventsRepository = new NoOpEventRepository();
				}
				else
				{
					wsClient = new WsClientCache(new Lazy<IWsClient>(() => new WsClient()), cacheFile);
					eventsRepository = new EventsRepository();
				}

				var connector = new IsirWsConnector(noCache, toEventId, stats, repository, eventsRepository, wsClient);
				connector.Handle();
			}
			else if (fromFiles)
			{
				var stats = new Stats();
				var repository = new Repository(stats);

				foreach (var dir in Directory.EnumerateDirectories("data"))
				{
					Console.WriteLine($"Zpracovava se slozka {dir} ...");
					Console.WriteLine();
					var count = 0;

					foreach (var file in Directory.EnumerateFiles(dir))
					{
						var rizeni = JsonConvert.DeserializeObject<Rizeni>(File.ReadAllText(file));
						repository.SetInsolvencyProceeding(rizeni);
						if (++count % 100 == 0)
						{
							Console.CursorTop = Console.CursorTop - 1;
							Console.WriteLine($"  {count} rizeni ulozeno");
						}
					}

					Console.CursorTop = Console.CursorTop - 1;
					Console.WriteLine($"  {count} rizeni ulozeno");
					Console.WriteLine();
				}
			}
			else if (only625fix)
			{
				new Event625().Execute(skipLines, cacheFile);
			}
            else if (only636fix)
            {
                new Event636().Execute(skipLines, cacheFile);
            }
            else if (onlyEventTypeFix)
			{
				new AddMissingEventTypes().Execute(cacheFile);
			}
			else if (onlyMissingDocumentsFix)
			{
				var stats = new Stats();
				var repository = new Repository(stats);

				new MissingDocuments(repository).Execute(cacheFile);
			}
			else if (onlyCacheFix)
			{
				new FillInCache(new WsClientCache(new Lazy<IWsClient>(() => new WsClient()), cacheFile)).Execute();
			}
			else if (personCreatedTimeFix)
			{
				new PersonCreatedTimeFix().Execute(cacheFile);
			}
			else if (eventTypeToStatus)
			{
				new EventTypeToStatus().Execute(cacheFile);
			}
			else if (export.Length > 0)
			{
				new Export().Execute(export, cacheFile);
			}
			else if (exportCanceledEvents)
			{
				new ExportCanceledEvents().Execute(skipLines, cacheFile);
			}
			else
			{
				PrintHelp(options);
			}
		}

		private static void PrintHelp(OptionSet p) {
			Console.WriteLine($"Program primarne slouzi ke cteni udalosti z insolvencniho rejstriku");
			Console.WriteLine($"a jejich zapis a aktualizace v interni databazi");
			Console.WriteLine($"");
			Console.WriteLine($"");
			Console.WriteLine($"Parametry spusteni:");
			p.WriteOptionDescriptions(Console.Out);
			Console.WriteLine($"");
			Console.WriteLine($"");
			Console.WriteLine($"Priklady pouziti:");
			Console.WriteLine($" - zpracovani a ulozeni udalosti od posledni zpracovane");
			Console.WriteLine($"   InsolvencniRejstrik.exe  -e");
			Console.WriteLine($"");
			Console.WriteLine($" - zpracovani udalosti od posledni zpracovena a ulozeni dat do souboru");
			Console.WriteLine($"   InsolvencniRejstrik.exe -e --to-files");
			Console.WriteLine($"");
			Console.WriteLine($" - ulozeni dat ze souboru do databaze");
			Console.WriteLine($"   InsolvencniRejstrik.exe --from-files");
			Console.WriteLine($"");
			Console.WriteLine($" - oprava zpracovani udalosti 625 s preskocenim prvnich 2M radku cache");
			Console.WriteLine($"   InsolvencniRejstrik.exe --625-fix --skip-lines=2000000");
		}
	}
}
