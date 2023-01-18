using HlidacStatu.Entities.Insolvence;
using log4net;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace InsolvencniRejstrik.ByEvents
{
	partial class IsirWsConnector
	{
		private readonly IRepository Repository;
		private readonly IEventsRepository EventsRepository;
		private readonly IIsirClient IsirClient;
		private readonly IWsClient WsClient;
		private readonly int ToEventId;
		private readonly TaskFactory TaskFactory;

		private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		public IsirWsConnector(bool noCache, int toEventId, Stats stats, IRepository repository, IEventsRepository eventRepository, IWsClient wsClient)
		{
			GlobalStats = stats;
			Repository = repository;
			EventsRepository = eventRepository;
			TaskFactory = new TaskFactory();

			IsirClient = noCache ? (IIsirClient)new IsirClient(GlobalStats) : new IsirClientCache(new IsirClient(GlobalStats), GlobalStats);
			WsClient = wsClient;
			ToEventId = toEventId;
		}

		public void Handle()
		{
			Console.WriteLine("Spousti se zpracovani ...");

			WsProcessorTask = RunTask(() => WsProcessor(EventsRepository.GetLastEventId()));
			MessageProcessorTask = RunTask(MessageProcessor);
			LinkProcessorTask = RunTask(LinkProcessor);
			var StatsInfo = RunTask(StatsInfoCallback);

			while (MessageProcessorTask.Status == TaskStatus.Running || LinkProcessorTask.Status == TaskStatus.Running)
			{
				Thread.Sleep(500);
			}
		}

		private Stats GlobalStats;
		private Task WsProcessorTask;
		private Task LinkProcessorTask;
		private Task MessageProcessorTask;
		private ConcurrentQueue<WsResult> WsResultsQueue = new ConcurrentQueue<WsResult>();
		private ConcurrentQueue<Rizeni> LinkRequestsQueue = new ConcurrentQueue<Rizeni>();

		private void WsProcessor(long id)
		{
			var lastId = id;
			while (true)
			{
				try
				{
					foreach (var item in WsClient.Get(id))
					{
						if (ToEventId > 0 && item.Id > ToEventId)
						{
							return;
						}

						WsResultsQueue.Enqueue(item);
						lastId = item.Id;

						while (WsResultsQueue.Count > 5_000)
						{
							Thread.Sleep(1_000);
						}
					}

					return;
				}
				catch (Exception e)
				{
					GlobalStats.WriteError("WS processor - " + e.Message, lastId);
					Log.Error($"WS processor - {e.Message} (eventId: {lastId})", e);
				}
			}
		}

		private void LinkProcessor()
		{
			while (!LinkRequestsQueue.IsEmpty || WsProcessorTask.Status == TaskStatus.Running || MessageProcessorTask.Status == TaskStatus.Running)
			{
				LinkRequestsQueue.TryDequeue(out var item);
				if (item != null)
				{
					item.Url = IsirClient.GetUrl(item.SpisovaZnacka);
				}
				else
				{
					Thread.Sleep(100);
				}
			}
		}

		private void MessageProcessor()
		{
			while (!WsResultsQueue.IsEmpty || WsProcessorTask.Status == TaskStatus.Running)
			{
				WsResultsQueue.TryDequeue(out var item);
				if (item != null)
				{
					GlobalStats.EventsCount++;
					GlobalStats.LastEventId = item.Id;
					GlobalStats.LastEventTime = item.DatumZalozeniUdalosti;
					try
					{
						var rizeni = Repository.GetInsolvencyProceeding(item.SpisovaZnacka, CreateNewInsolvencyProceeding);
						var lastChanged = rizeni?.PosledniZmena ?? DateTime.MinValue;

						ProcessDocument(item, rizeni);

						if (!string.IsNullOrEmpty(item.Poznamka))
						{
							var xdoc = XDocument.Parse(item.Poznamka);

							if (string.IsNullOrEmpty(rizeni.Soud))
							{
								var puvodce = ParseValue(xdoc, "idOsobyPuvodce");

								if (puvodce.Length > 0)
								{
									rizeni.Soud = puvodce;
									rizeni.PosledniZmena = lastChanged;
								}
							}

							var datumVyskrtnuti = ParseValue(xdoc, "datumVyskrtnuti");
							if (!rizeni.Vyskrtnuto.HasValue && !string.IsNullOrEmpty(datumVyskrtnuti))
							{
								var date = Devmasters.DT.Util.ToDateTime(datumVyskrtnuti, "MM/dd/yyyy h:m:s tt");
								if (date.HasValue == false)
									date = Devmasters.DT.Util.ToDateTime(datumVyskrtnuti);
                                if (date.HasValue == false)
                                    date = Devmasters.DT.Util.ToDateTime(datumVyskrtnuti, "yyyy-MM-dd K");
                                if (date.HasValue == false)
                                    date = Devmasters.DT.Util.ToDateTime(datumVyskrtnuti, "yyyy-MM-ddK");
                                if (date.HasValue == false)
									Console.WriteLine("MessageProcessor datumVyskrtnuti:" + datumVyskrtnuti);
								rizeni.Vyskrtnuto = date;
								rizeni.PosledniZmena = item.DatumZalozeniUdalosti;
							}

							var updated = false;

							switch (item.TypUdalosti)
							{
								case "1": // zmena osoby
								case "625": // Zasilani udaju o zmene osoby veritele v prihlasce
									GlobalStats.OsobaChangedEvent++;
									var osoba = GetOsoba(xdoc, rizeni, item.Id, item.DatumZalozeniUdalosti);
									if (osoba == null || UpdatePerson(osoba, xdoc.Descendants("osoba").FirstOrDefault()))
									{
										rizeni.PosledniZmena = item.DatumZalozeniUdalosti;
										updated = true;
									}
									break;
								case "2": // zmena adresy osoby
									GlobalStats.AdresaChangedEvent++;
									var osoba2 = GetOsoba(xdoc, rizeni, item.Id, item.DatumZalozeniUdalosti);
									var el = xdoc.Descendants("osoba").FirstOrDefault();
									if (osoba2 == null || UpdatePerson(osoba2, el) || UpdateAddress(osoba2, el))
									{
										rizeni.PosledniZmena = item.DatumZalozeniUdalosti;
										updated = true;
									}
									break;
								case "5": // insolvencni navrh
								case "6": // insolvencni navrh s oddluzenim
								case "185": // Vyhlaska o zahajeni insolvencniho rizeni
									if (!rizeni.DatumZalozeni.HasValue || rizeni.DatumZalozeni.Value > item.DatumZalozeniUdalosti)
									{
										rizeni.DatumZalozeni = item.DatumZalozeniUdalosti;
										rizeni.PosledniZmena = item.DatumZalozeniUdalosti;
										updated = true;
									}
									break;
								default:
									var state = ParseValue(xdoc.Descendants("vec").FirstOrDefault(), "druhStavRizeni");
									if (!string.IsNullOrEmpty(state) && rizeni.Stav != state)
									{
										rizeni.Stav = state;
										rizeni.PosledniZmena = item.DatumZalozeniUdalosti;
										GlobalStats.StateChangedCount++;
										updated = true;
									}
									break;
							}

							if (lastChanged != rizeni.PosledniZmena || updated)
							{
								Repository.SetInsolvencyProceeding(rizeni);
							}
						}
						EventsRepository.SetLastEventId(item.Id);
					}
					catch (Exception e)
					{
						GlobalStats.WriteError($"Message task - {e.Message}", item.Id);
						File.AppendAllText("errors.log", $@"""
[{DateTime.Now}]
{item.Id} - {item.SpisovaZnacka} - {item.PopisUdalosti}
{e.ToString()}
{e.StackTrace}
""");
						Log.Error($"Message processor - {e.Message} (eventId: {item.Id})", e);
						File.AppendAllLines("failed_messages.dat", new[] { item.ToStringLine() });
					}
				}
				else
				{
					Thread.Sleep(100);
				}
			}
		}

		private Osoba GetOsoba(XDocument xdoc, Rizeni rizeni, long eventId, DateTime eventTime)
		{
			try
			{
				var idPuvodce = ParseValue(xdoc, "idOsobyPuvodce");
				var o = xdoc.Descendants("osoba").FirstOrDefault();
				var osobaId = ParseValue(o, "idOsoby");
				var role = ParseValue(o, "druhRoleVRizeni");
				var zrusena = ParseValue(o, "datumOsobaVeVeciZrusena");

				List<Osoba> osoby = null;
				switch (role)
				{
					case "DLUŽNÍK":
						osoby = rizeni.Dluznici;
						break;
					case "VĚŘITEL":
					case "VĚŘIT-NAVR":
						osoby = rizeni.Veritele;
						break;
					case "SPRÁVCE":
						osoby = rizeni.Spravci;
						break;
					default:
						throw new UnknownRoleException($"Unknown role '{role}'");
				}

				var osoba = osoby.FirstOrDefault(d => d.IdPuvodce == idPuvodce && d.IdOsoby == osobaId);

				if (!string.IsNullOrEmpty(zrusena))
				{
					if (osoba != null)
					{ 
						osoby.Remove(osoba);
					}
					return null;
				}

				if (osoba == null)
				{
					osoba = new Osoba { IdOsoby = osobaId, IdPuvodce = idPuvodce, Role = role, Zalozen = eventTime };
					osoby.Add(osoba);
				}
				else if (osoby.Count(d => d.IdPuvodce == idPuvodce && d.IdOsoby == osobaId) > 1)
				{
					GlobalStats.WriteError($"Existuje vice osob se stejnym identifikatorem! (spis.znacka: {rizeni.SpisovaZnacka}, IdPuvodce: {idPuvodce}, IdOsoby: {osobaId})", eventId);
					Log.Warn($"Existuje vice osob se stejnym identifikatorem! (spis.znacka: {rizeni.SpisovaZnacka}, IdPuvodce: {idPuvodce}, IdOsoby: {osobaId}) (eventId: {eventId}");
				}
				return osoba;
			}
			catch (Exception e)
			{
				GlobalStats.WriteError(e.Message, eventId);
				throw;
			}
		}

		private void ProcessDocument(WsResult item, Rizeni rizeni)
		{
			if (!string.IsNullOrEmpty(item.DokumentUrl))
			{
				var document = rizeni.Dokumenty.SingleOrDefault(d => d.Id == item.Id.ToString());
				if (document == null)
				{
					document = new Dokument
					{
						Id = item.Id.ToString(),
					};
					rizeni.Dokumenty.Add(document);
				}

				var typUdalosti = -1;
				if (!int.TryParse(item.TypUdalosti, out typUdalosti))
				{
					GlobalStats.WriteError($"Nepodarilo se naparserovat typ udalosti ({item.TypUdalosti})", item.Id);
					Log.Warn($"Nepodarilo se naparserovat typ udalosti '{item.TypUdalosti}' (eventId: {item.Id})");
				}
				document.TypUdalosti = typUdalosti;
				document.Url = item.DokumentUrl;
				document.DatumVlozeni = item.DatumZalozeniUdalosti;
				document.Popis = item.PopisUdalosti;
				document.Oddil = item.Oddil;

				rizeni.PosledniZmena = item.DatumZalozeniUdalosti;
				GlobalStats.DocumentCount++;
			}
		}

		private Rizeni CreateNewInsolvencyProceeding(string spisovaZnacka)
		{
			GlobalStats.RizeniCount++;
			var r = new Rizeni { SpisovaZnacka = spisovaZnacka, IsFullRecord = true };
			LinkRequestsQueue.Enqueue(r);
			return r;
		}

		private static HashSet<string> FyzickeOsoby = new HashSet<string> { "F", "SPRÁV_INS", "PAT_ZAST", "DAN_PORAD", "U", "SPRÁVCE_KP", "Z", "MEDIÁTOR" };
		private static HashSet<string> PravnickeOsoby = new HashSet<string> { "P", "PODNIKATEL", "OST_OVM", "SPR_ORGAN", "POLICIE", "O", "S", "ADVOKÁT", "EXEKUTOR", "ZNAL_TLUM" };

		private static bool Update<T, U>(T target, Expression<Func<T, U>> item, U value)
		{
			var expr = (MemberExpression)item.Body;
			var prop = (PropertyInfo)expr.Member;
			if (!(((U)prop.GetValue(target))?.Equals(value) ?? false))
			{
				prop.SetValue(target, value, null);
				return true;
			}
			return false;
		}

		internal static bool UpdatePerson(Osoba person, XElement element)
		{
			var changed = false;
			changed |= Update(person, p => p.Typ, ParseValue(element, "druhOsoby"));
			changed |= Update(person, p => p.Role, ParseValue(element, "druhRoleVRizeni"));
			changed |= Update(person, p => p.PlneJmeno, ParseName(element));

			if (FyzickeOsoby.Contains(person.Typ))
			{
				changed |= Update(person, p => p.Rc, ParseValue(element, "rc"));
				var sdate = ParseValue(element, "datumNarozeni");

				if (!string.IsNullOrEmpty(sdate))
				{
					var date = Devmasters.DT.Util.ToDateTime(sdate, "yyyy-MM-ddK");
					if (date.HasValue == false)
						date = Devmasters.DT.Util.ToDateTime(sdate);
					if (date.HasValue == false)
						Console.WriteLine("UpdatePerson:" + sdate);

					changed |= Update(person, p => p.DatumNarozeni, date.Value);
				}
			}
			else if (PravnickeOsoby.Contains(person.Typ))
			{
				changed |= Update(person, p => p.ICO, ParseValue(element, "ic"));
				var sdate = ParseValue(element, "datumNarozeni");
				if (!string.IsNullOrEmpty(sdate))
				{
					var date = Devmasters.DT.Util.ToDateTime(sdate, "yyyy-MM-ddK");
					if (date.HasValue == false)
						date = Devmasters.DT.Util.ToDateTime(sdate);
					if (date.HasValue == false)
						Console.WriteLine("UpdatePerson prav:" + sdate);
					changed |= Update(person, p => p.DatumNarozeni, date.Value);
				}
			}
			else
			{
				throw new UnknownPersonException($"Unknown type of Osoba - {person.Typ}");
			}

			return changed;
		}

		private bool UpdateAddress(Osoba osoba, XElement element)
		{
			var a = element.Descendants("adresa").FirstOrDefault();

			if (a != null)
			{
				var druhAdresy = ParseValue(a, "druhAdresy");
				if (druhAdresy == "TRVALÁ" || druhAdresy == "SÍDLO FY")
				{
					var changed = Update(osoba, p => p.Mesto, ParseValue(a, "mesto"));
					changed |= Update(osoba, p => p.Okres, ParseValue(a, "okres"));
					changed |= Update(osoba, p => p.Zeme, ParseValue(a, "zeme"));
					changed |= Update(osoba, p => p.Psc, ParseValue(a, "psc"));

					return changed;
				}
			}

			return false;
		}

		private static string ParseName(XElement o)
		{
			return string.Join(" ", new[] {
											ParseValue(o, "titulPred"),
											ParseValue(o, "jmeno"),
											ParseValue(o, "nazevOsoby"),
											 ParseValue(o, "titulZa"),
										}.Where(i => !string.IsNullOrEmpty(i)));
		}

		private void StatsInfoCallback()
		{
			while (true)
			{
				try
				{
					PrintHeader();
					var speed = GlobalStats.EventsCount / GlobalStats.Duration().TotalSeconds;
					var remains = speed > 0 && GlobalStats.LastEventId < ToEventId
						? $" => {TimeSpan.FromSeconds((ToEventId - GlobalStats.EventsCount) / speed)}"
						: string.Empty;
					Console.WriteLine($"   Zpracovano udalosti: {GlobalStats.EventsCount} ({speed:0.00} udalost/s{remains})");
					Console.WriteLine($"   Doba behu: {GlobalStats.RunningTime()}");
					Console.WriteLine();
					Console.WriteLine($"   Nacteno rizeni: {GlobalStats.RizeniCount}");
					Console.WriteLine($"   Nacteno dokumentu: {GlobalStats.DocumentCount}");
					Console.WriteLine($"   Nacteno linku: {GlobalStats.LinkCount} ({GlobalStats.LinkCacheCount})");
					Console.WriteLine();
					Console.WriteLine($"   Fronta zprav: {WsResultsQueue.Count}");
					Console.WriteLine($"   Fronta linku: {LinkRequestsQueue.Count}");
					Console.WriteLine();
					Console.WriteLine($"   Pocet udalosti zmeny osoby: {GlobalStats.OsobaChangedEvent}");
					Console.WriteLine($"   Pocet udalosti zmeny adresy: {GlobalStats.AdresaChangedEvent}");
					Console.WriteLine($"   Pocet novych osob: {GlobalStats.NewOsobaCount}");
					Console.WriteLine($"   Pocet zmen stavu rizeni: {GlobalStats.StateChangedCount}");
					Console.WriteLine();
					Console.WriteLine($"   Posledni zpracovavane id udalosti: {GlobalStats.LastEventId}");
					Console.WriteLine($"   Datum posledni zpravovavane udalosti: {GlobalStats.LastEventTime.ToShortDateString()}");
					Console.WriteLine();
					Console.WriteLine($"   Vlakno WS: {WsProcessorTask.Status}");
					Console.WriteLine($"   Vlakno zprav: {MessageProcessorTask.Status}");
					Console.WriteLine($"   Vlakno linku: {LinkProcessorTask.Status}");
					Console.WriteLine();
					Console.WriteLine($"   Data rizeni: R{GlobalStats.InsolvencyProceedingGet}/W{GlobalStats.InsolvencyProceedingSet}");
					Console.WriteLine();
					Console.WriteLine($"   Errors (total: {GlobalStats.TotalErrors}):");
					foreach (var error in GlobalStats.Errors.ToArray())
					{
						Console.WriteLine($"    - {error}");
					}
					Thread.Sleep(1000);
				}
				catch (Exception e)
				{
					Console.Clear();
					Console.WriteLine("Vlakno pro vykreslovani UI selhalo");
					Console.WriteLine(e.Message);
					Console.WriteLine(e.StackTrace);
					Thread.Sleep(1000);
				}
			}
		}

		internal static string ParseValue(XElement xel, string element)
		{
			return xel?.Element(XName.Get(element))?.Value ?? "";
		}

		internal static string ParseValue(XDocument xdoc, string element)
		{
			return xdoc.Descendants(element).FirstOrDefault()?.Value ?? "";
		}

		private Task RunTask(Action action)
		{
			var task = TaskFactory.StartNew(action);
			while (task.Status != TaskStatus.Running) Thread.Sleep(10);
			return task;
		}

		private void PrintHeader()
		{
			Console.Clear();
			Console.WriteLine("HlidacStatu - Insolvencni rejstrik");
			Console.WriteLine("----------------------------------");
			Console.WriteLine();
		}
	}
}
