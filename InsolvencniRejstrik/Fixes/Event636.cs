using HlidacStatu.Entities.Insolvence;
using InsolvencniRejstrik.ByEvents;
using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using HlidacStatu.Connectors;

namespace InsolvencniRejstrik.Fixes
{
	class Event636
	{
		private int Lines = 0;
		private int FixingEvents = 0;
		private int Added = 0;

		public void Execute(int skipLines, string cacheFile)
		{
			Console.WriteLine("Oprava zpracovani udalosti typu 636");
			if (skipLines > 0)
			{
				Console.WriteLine($"{skipLines} radek bude preskoceno");
			}
			Console.WriteLine();
			Console.WriteLine();

			if (File.Exists(cacheFile))
			{
				foreach (var line in File.ReadLines(cacheFile))
				{
					try
					{
						var item = WsResult.From(line);
						Lines++;
						if (Lines < skipLines) continue;

						if (item.TypUdalosti == "636")
						{
							FixingEvents++;
							ProcessEvent(item);
						}

						if (Lines % 100 == 0)
						{
							Console.CursorTop = Console.CursorTop - 1;
							Console.WriteLine($"Precteno {Lines} radku z toho {FixingEvents} opravovanych udalosti (id udalosti: {item.Id})");
						}
					}
					catch (UnknownPersonException e)
					{
						using (var stream = File.AppendText("unknown-person-type.log"))
						{
							stream.WriteLine(line);
							stream.Flush();
						}
						Console.WriteLine();
						Console.WriteLine($"ERROR: {e.Message}");
						Console.WriteLine();
						Console.WriteLine();
					}
					catch (UnknownRoleException e)
					{
						using (var stream = File.AppendText("unknown-role-type.log"))
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
			}
		}

		private void ProcessEvent(WsResult item)
		{
			var rizeni = LoadRizeni(item.SpisovaZnacka);
			if (rizeni == null)
			{
				Console.WriteLine($"Rizeni {item.SpisovaZnacka} nebylo nalezeno");
				Console.WriteLine();
				return;
			}
			if (rizeni.Odstraneny == false)
			{
				rizeni.Odstraneny = true;
				SaveRizeni(rizeni);
			}

		}

		private Rizeni LoadRizeni(string spisovaZnacka)
		{
			var res = Manager.GetESClient_InsolvenceAsync().ConfigureAwait(false).GetAwaiter().GetResult().Get<Rizeni>(spisovaZnacka, s => s.SourceExcludes("dokumenty"));
			return res.Found ? res.Source : null;
		}

		private void SaveRizeni(Rizeni rizeni)
		{
			var esUrl = Manager.GetESClient_InsolvenceAsync().ConfigureAwait(false).GetAwaiter().GetResult().ConnectionSettings.ConnectionPool.Nodes.First().Uri.ToString();
			esUrl += $"insolvencnirestrik/_update/{System.Net.WebUtility.UrlEncode(rizeni.SpisovaZnacka).Replace("INS+","INS%20")}";
			try
			{
				using (var url = new Devmasters.Net.HttpClient.URLContent(esUrl.ToString()))
				{
					url.Method = Devmasters.Net.HttpClient.MethodEnum.POST;
					url.Tries = 3;
					url.TimeInMsBetweenTries = 1500;
					url.Timeout = url.Timeout * 5;
					//url.Proxy = new Devmasters.Net.Proxies.SimpleProxy("127.0.0.1", 8888);
					var postContent = "{\"doc\" : {\"odstraneny\" : true }}";
					url.RequestParams.RawContent = postContent;
                    url.ContentType = "application/json; charset=UTF-8";
                    var esres = url.GetContent();
				}
			}
			catch (Devmasters.Net.HttpClient.UrlContentException ex)
			{
				Console.WriteLine($"Rizeni {rizeni.SpisovaZnacka} - chyba ukladani (radek {Lines})");
			}
			catch (Exception e)
			{
				Console.WriteLine($"Rizeni {rizeni.SpisovaZnacka} - chyba ukladani " + e.ToString());
			}
		}

		private Osoba ParseOsoba(XDocument doc)
		{
			var idPuvodce = IsirWsConnector.ParseValue(doc, "idOsobyPuvodce");
			var o = doc.Descendants("osoba").FirstOrDefault();
			var osobaId = IsirWsConnector.ParseValue(o, "idOsoby");
			var role = IsirWsConnector.ParseValue(o, "druhRoleVRizeni");
			var osoba = new Osoba { IdOsoby = osobaId, IdPuvodce = idPuvodce, Role = role };

			IsirWsConnector.UpdatePerson(osoba, doc.Descendants("osoba").FirstOrDefault());

			return osoba;
		}
	}
}
