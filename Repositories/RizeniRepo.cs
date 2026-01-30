using HlidacStatu.Connectors;
using HlidacStatu.Entities;
using HlidacStatu.Entities.Insolvence;
using Microsoft.EntityFrameworkCore;
using Nest;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using Osoba = HlidacStatu.Entities.Insolvence.Osoba;

namespace HlidacStatu.Repositories
{
	public static class RizeniRepo
	{
		
		private static readonly ILogger _logger = Log.ForContext(typeof(RizeniRepo));

		public static bool ExistInDb(Lib.Db.Insolvence.Rizeni rizeni)
		{
			using (HlidacStatu.Lib.Db.Insolvence.InsolvenceEntities idb = new Lib.Db.Insolvence.InsolvenceEntities())
			{
				return idb.Rizeni.Any(m => m.SpisovaZnacka == rizeni.SpisovaZnacka);
			}
		}



		public static async Task PrepareForSaveAsync(Rizeni rizeni, bool skipOsobaIdLink = false)
		{
			if (skipOsobaIdLink == false)
				rizeni.OnRadar = false; //reset settings

			foreach (var d in rizeni.Dluznici)
			{
				if (skipOsobaIdLink == false && d.OsobaId == null && d.DatumNarozeni.HasValue)
				{
					//try to find osobaId from db
					var found = Validators.JmenoInText(d.PlneJmeno);
					if (found != null)
					{
						var osoba = await 
							OsobaRepo.Searching.GetByNameAsync(found.Jmeno, found.Prijmeni, d.DatumNarozeni.Value);
						if (osoba != null)
						{
							d.OsobaId = osoba.NameId;
							rizeni.OnRadar = rizeni.OnRadar || osoba.Status > 0;
						}
						else
							d.OsobaId = "";
					}
					else
						d.OsobaId = "";
				}
			}

			foreach (var d in rizeni.Spravci)
			{
				if (skipOsobaIdLink == false && d.OsobaId == null && d.DatumNarozeni.HasValue)
				{
					//try to find osobaId from db
					var found = Validators.JmenoInText(d.PlneJmeno);
					if (found != null)
					{
						var osoba = await 
							OsobaRepo.Searching.GetByNameAsync(found.Jmeno, found.Prijmeni, d.DatumNarozeni.Value);
						d.OsobaId = osoba != null ? osoba.NameId : "";
					}
					else
						d.OsobaId = "";
				}
			}

			foreach (var d in rizeni.Veritele)
			{
				if (skipOsobaIdLink == false && d.OsobaId == null && d.DatumNarozeni.HasValue)
				{
					//try to find osobaId from db
					var found = Validators.JmenoInText(d.PlneJmeno);
					if (found != null)
					{
						var osoba = await 
							OsobaRepo.Searching.GetByNameAsync(found.Jmeno, found.Prijmeni, d.DatumNarozeni.Value);
						d.OsobaId = osoba != null ? osoba.NameId : "";
					}
					else
						d.OsobaId = "";
				}
			}

			if (rizeni.Dluznici.Any(m => !(m.Typ == "F" || m.Typ == "PODNIKATEL")))
				rizeni.OnRadar = true;
			else
			{
				if (skipOsobaIdLink == false)
				{
					foreach (var d in rizeni.Dluznici)
					{
						if (OsobaRepo.PolitickyAktivni.Get().Any(m =>
								m.JmenoAscii == Devmasters.TextUtil.RemoveDiacritics(d.Jmeno())
								&& m.PrijmeniAscii == Devmasters.TextUtil.RemoveDiacritics(d.Prijmeni())
								&& m.Narozeni == d.GetDatumNarozeni() && d.GetDatumNarozeni().HasValue
							)
						)
							rizeni.OnRadar = true;
						break;
					}
				}
			}
		}

		static SemaphoreSlim _saveLock = new (1);
		public static async Task SaveAsync(Rizeni rizeni, ElasticClient client = null, bool? forceOnRadarValue = null)
		{
			if (rizeni.IsFullRecord == false)
				throw new ApplicationException("Cannot save partial Insolvence document ");

			if (client == null)
				client = Manager.GetESClient_Insolvence();

			await PrepareForSaveAsync(rizeni);
			if (forceOnRadarValue.HasValue)
				rizeni.OnRadar = forceOnRadarValue.Value;

			//prepare SearchableDocuments
			var existing = await SearchableDocumentRepo.AllIdsAsync(rizeni.SpisovaZnacka);
			if (existing == null)
			{
				await SearchableDocumentRepo.SaveManyAsync(SearchableDocument.CreateSearchableDocuments(rizeni));
			}
			else if (existing.Length == 0)
				await SearchableDocumentRepo.SaveManyAsync(SearchableDocument.CreateSearchableDocuments(rizeni));
			else
			{
				//add missing
				if (rizeni.Dokumenty != null)
					foreach (var doc in rizeni.Dokumenty)
					{
						var sdocid = SearchableDocument.GetDocumentId(rizeni, doc);
						if (existing.Contains(sdocid) == false)
							await SearchableDocumentRepo.SaveAsync(SearchableDocument.CreateSearchableDocument(rizeni, doc,false));
					}
			};
			if (rizeni.Dokumenty != null)
				foreach (var doc in rizeni.Dokumenty)
				{
					doc.PlainText = null;
				}

			//save full rizeni do prvniho SearchableDocumentu
			if (rizeni.Dokumenty!=null && rizeni.Dokumenty.Count> 0)
			{
                var doc = await SearchableDocumentRepo.GetAsync(rizeni.SpisovaZnacka, rizeni.Dokumenty.First().Id, true);
				if (doc != null)
				{
					if (doc.Rizeni.Veritele?.Count != rizeni.Veritele?.Count)
					{
						doc.Rizeni.Veritele = rizeni.Veritele;
						await SearchableDocumentRepo.SaveAsync(doc);
					}
				}
            }

            var res = await client.IndexAsync<Rizeni>(rizeni,
			o => o.Id(rizeni.SpisovaZnacka)); //druhy parametr musi byt pole, ktere je unikatni
			if (!res.IsValid)
			{
				await _saveLock.WaitAsync();
				try
				{
					await Task.Delay(100);
					res = await client.IndexAsync<Rizeni>(rizeni, o => o.Id(rizeni.SpisovaZnacka));
				}
				finally
				{
					_saveLock.Release();
				}
				if (!res.IsValid)
				{
					throw new ApplicationException(rizeni.SpisovaZnacka + "  err :" + res.ServerError?.ToString());
				}
			}
		}


		private static void SaveToDb(Rizeni rizeni, bool rewrite, bool skipOsobaIdLink = false)
		{
			using (HlidacStatu.Lib.Db.Insolvence.InsolvenceEntities idb = new Lib.Db.Insolvence.InsolvenceEntities())
			{
				var exists = idb.Rizeni.Where(m => m.SpisovaZnacka == rizeni.SpisovaZnacka)?.ToList()?.FirstOrDefault();
				bool addNew = exists == null;

				if (exists != null && rewrite == true)
				{
					foreach (var d in idb.Dokumenty.Where(m => m.RizeniId == exists.SpisovaZnacka).ToList())
						idb.Dokumenty.Remove(d);

					foreach (var d in idb.Dluznici.Where(m => m.RizeniId == exists.SpisovaZnacka).ToList())
						idb.Dluznici.Remove(d);
					foreach (var d in idb.Veritele.Where(m => m.RizeniId == exists.SpisovaZnacka).ToList())
						idb.Veritele.Remove(d);
					foreach (var d in idb.Spravci.Where(m => m.RizeniId == exists.SpisovaZnacka).ToList())
						idb.Spravci.Remove(d);

					idb.Rizeni.Remove(exists);

					idb.SaveChanges();
					addNew = true;
				}

				if (addNew)
				{
					var r = new HlidacStatu.Lib.Db.Insolvence.Rizeni();
					r.DatumZalozeni = rizeni.DatumZalozeni ?? new DateTime(1990, 1, 1);
					r.SpisovaZnacka = rizeni.SpisovaZnacka;
					r.OnRadar = rizeni.OnRadar;
					r.PosledniZmena = rizeni.PosledniZmena;
					r.Soud = rizeni.Soud ?? "";
					r.Stav = rizeni.Stav ?? "";

					idb.Rizeni.Add(r);

					foreach (var td in rizeni.Dluznici)
					{
						var d = ToIOsoba<Lib.Db.Insolvence.Dluznici>(rizeni.SpisovaZnacka, td);
						idb.Dluznici.Add(d);
					}
					foreach (var td in rizeni.Veritele)
					{
						var d = ToIOsoba<Lib.Db.Insolvence.Veritele>(rizeni.SpisovaZnacka, td);
						idb.Veritele.Add(d);
					}
					foreach (var td in rizeni.Spravci)
					{
						var d = ToIOsoba<Lib.Db.Insolvence.Spravci>(rizeni.SpisovaZnacka, td);
						idb.Spravci.Add(d);
					}
					foreach (var td in rizeni.Dokumenty)
					{
						var d = ToDbDokument(rizeni.SpisovaZnacka, td);
						idb.Dokumenty.Add(d);
					}
				}
				else // update existing
				{
					var sameR = exists.DatumZalozeni == (rizeni.DatumZalozeni ?? new DateTime(1990, 1, 1))
								&& exists.SpisovaZnacka == rizeni.SpisovaZnacka
								&& exists.OnRadar == rizeni.OnRadar
								&& exists.PosledniZmena == rizeni.PosledniZmena
								&& exists.Soud == (rizeni.Soud ?? "")
								&& exists.Stav == (rizeni.Stav ?? "");

					if (sameR == false)
					{
						exists.DatumZalozeni = (rizeni.DatumZalozeni ?? new DateTime(1990, 1, 1));
						exists.SpisovaZnacka = rizeni.SpisovaZnacka;
						exists.OnRadar = rizeni.OnRadar;
						exists.PosledniZmena = rizeni.PosledniZmena;
						exists.Soud = (rizeni.Soud ?? "");
						exists.Stav = (rizeni.Stav ?? "");
					}

					#region Dluznici
					var dbDluznici = idb.Dluznici.Where(m => m.RizeniId == exists.SpisovaZnacka).ToList();
					//update existing
					foreach (var d in rizeni.Dluznici)
					{
						for (int i = 0; i < dbDluznici.Count(); i++)
						{
							var dd = dbDluznici[i];
							if (d.IdOsoby == dd.IdOsoby && d.IdPuvodce == dd.IdPuvodce)
							{
								//same
								if (!Validators.AreObjectsEqual(ToIOsoba<Lib.Db.Insolvence.Dluznici>(rizeni.SpisovaZnacka, d), dd, false, "pk"))
									dd = (Lib.Db.Insolvence.Dluznici)UpdateIOsoba(d, dd);
							}
						}
					}

					if (rizeni.Dluznici.Count() > dbDluznici.Count())
					{
						foreach (var d in rizeni.Dluznici)
							if (!dbDluznici.Any(m => m.IdOsoby == d.IdOsoby && m.IdPuvodce == d.IdPuvodce))
								idb.Dluznici.Add(ToIOsoba<Lib.Db.Insolvence.Dluznici>(rizeni.SpisovaZnacka, d));
					}

					if (rizeni.Dluznici.Count() < dbDluznici.Count())
					{
						//remove all and add orig
						foreach (var d in dbDluznici)
							idb.Dluznici.Remove(d);
						foreach (var d in rizeni.Dluznici)
							idb.Dluznici.Add(ToIOsoba<Lib.Db.Insolvence.Dluznici>(rizeni.SpisovaZnacka, d));
					}
					#endregion

					#region Veritele
					var dbVeritele = idb.Veritele.Where(m => m.RizeniId == exists.SpisovaZnacka).ToList();
					//update existing
					foreach (var d in rizeni.Veritele)
					{
						for (int i = 0; i < dbVeritele.Count(); i++)
						{
							var dd = dbVeritele[i];
							if (d.IdOsoby == dd.IdOsoby && d.IdPuvodce == dd.IdPuvodce)
							{
								//same
								if (!Validators.AreObjectsEqual(ToIOsoba<Lib.Db.Insolvence.Veritele>(rizeni.SpisovaZnacka, d), dd, false, "pk"))
									dd = (Lib.Db.Insolvence.Veritele)UpdateIOsoba(d, dd);
							}
						}
					}

					if (rizeni.Veritele.Count() > dbVeritele.Count())
					{
						foreach (var d in rizeni.Veritele)
							if (!dbVeritele.Any(m => m.IdOsoby == d.IdOsoby && m.IdPuvodce == d.IdPuvodce))
								idb.Veritele.Add(ToIOsoba<Lib.Db.Insolvence.Veritele>(rizeni.SpisovaZnacka, d));
					}

					if (rizeni.Veritele.Count() < dbVeritele.Count())
					{
						//remove all and add orig
						foreach (var d in dbVeritele)
							idb.Veritele.Remove(d);
						foreach (var d in rizeni.Veritele)
							idb.Veritele.Add(ToIOsoba<Lib.Db.Insolvence.Veritele>(rizeni.SpisovaZnacka, d));
					}
					#endregion

					#region Spravci
					var dbSpravci = idb.Spravci.Where(m => m.RizeniId == exists.SpisovaZnacka).ToList();
					//update existing
					foreach (var d in rizeni.Spravci)
					{
						for (int i = 0; i < dbSpravci.Count(); i++)
						{
							var dd = dbSpravci[i];
							if (d.IdOsoby == dd.IdOsoby && d.IdPuvodce == dd.IdPuvodce)
							{
								//same
								if (!Validators.AreObjectsEqual(ToIOsoba<Lib.Db.Insolvence.Spravci>(rizeni.SpisovaZnacka, d), dd, false, "pk"))
									dd = (Lib.Db.Insolvence.Spravci)UpdateIOsoba(d, dd);
							}
						}
					}

					if (rizeni.Spravci.Count() > dbSpravci.Count())
					{
						foreach (var d in rizeni.Spravci)
							if (!dbSpravci.Any(m => m.IdOsoby == d.IdOsoby && m.IdPuvodce == d.IdPuvodce))
								idb.Spravci.Add(ToIOsoba<Lib.Db.Insolvence.Spravci>(rizeni.SpisovaZnacka, d));
					}

					if (rizeni.Spravci.Count() < dbSpravci.Count())
					{
						//remove all and add orig
						foreach (var d in dbSpravci)
							idb.Spravci.Remove(d);
						foreach (var d in rizeni.Spravci)
							idb.Spravci.Add(ToIOsoba<Lib.Db.Insolvence.Spravci>(rizeni.SpisovaZnacka, d));
					}
					#endregion

					#region Dokumenty
					//if (System.Diagnostics.Debugger.IsAttached)
					//    idb.Database.Log = Console.WriteLine;

					var dbDokumenty = idb.Dokumenty.Where(m => m.RizeniId == exists.SpisovaZnacka).ToList();
					//update existing
					foreach (var d in rizeni.Dokumenty)
					{
						for (int i = 0; i < dbDokumenty.Count(); i++)
						{
							var dd = dbDokumenty[i];
							if (d.Id == dd.DokumentId)
							{
								//same
								if (!Validators.AreObjectsEqual(ToDbDokument(rizeni.SpisovaZnacka, d), dd, false))
								{
									dd.DokumentId = d.Id;
									dd.Length = (int)d.Lenght;
									dd.Oddil = d.Oddil;
									dd.Popis = d.Popis;
									dd.TypUdalosti = d.TypUdalosti;
									dd.Url = d.Url;
									dd.WordCount = (int)d.WordCount;
								}
							}
						}
					}

					if (rizeni.Dokumenty.Count() > dbDokumenty.Count())
					{
						foreach (var d in rizeni.Dokumenty)
							if (!dbDokumenty.Any(m => m.DokumentId == d.Id))
								idb.Dokumenty.Add(ToDbDokument(rizeni.SpisovaZnacka, d));
					}

					if (rizeni.Dokumenty.Count() < dbDokumenty.Count())
					{
						foreach (var d in dbDokumenty)
							if (!rizeni.Dokumenty.Any(m => m.Id == d.DokumentId))
								idb.Dokumenty.Remove(d);
					}
					#endregion

				}



				try
				{
					if (idb.ChangeTracker.HasChanges())
					{
						_logger.Information($"Updating Rizeni into DB {rizeni.SpisovaZnacka}, {idb.ChangeTracker.Entries().Count(m => m.State != EntityState.Unchanged)} changes.");
					}
					//idb.CommandTimeout = 120;
					idb.SaveChanges();

				}

				catch (DbUpdateException)
				{
					//Add your code to inspect the inner exception and/or
					//e.Entries here.
					//Or just use the debugger.
					//Added this catch (after the comments below) to make it more obvious 
					//how this code might help this specific problem
					throw;
				}
				catch (Exception e)
				{
					//Debug.WriteLine(e.Message);
					throw;
				}
			}
		}

		public static Lib.Db.Insolvence.IOsoba UpdateIOsoba(Osoba d, Lib.Db.Insolvence.IOsoba dd)
		{
			dd.DatumNarozeni = d.DatumNarozeni < Entities.Insolvence.Rizeni.MinSqlDate ? Entities.Insolvence.Rizeni.MinSqlDate : d.DatumNarozeni;
			dd.ICO = d.ICO;
			dd.Mesto = d.Mesto;
			dd.Okres = d.Okres;
			dd.PlneJmeno = d.PlneJmeno;
			dd.PSC = d.Psc;
			dd.RC = d.Rc;
			dd.Role = d.Role;
			dd.Typ = d.Typ;
			dd.Zeme = d.Zeme;
			dd.OsobaId = d.OsobaId;
			dd.Zalozen = d.Zalozen;
			dd.Odstranen = d.Odstranen;
			return dd;
		}

		private static T ToIOsoba<T>(string rizeniSpisovaZnacka, Osoba td)
			where T : Lib.Db.Insolvence.IOsoba, new()
		{
			Lib.Db.Insolvence.IOsoba d = new T();
			d.DatumNarozeni = td.DatumNarozeni < Entities.Insolvence.Rizeni.MinSqlDate ? Entities.Insolvence.Rizeni.MinSqlDate : td.DatumNarozeni;
			d.ICO = td.ICO;
			d.IdOsoby = td.IdOsoby;
			d.IdPuvodce = td.IdPuvodce;
			d.Mesto = Devmasters.TextUtil.ShortenText(td.Mesto, 150);
			d.Okres = td.Okres;
			d.PlneJmeno = Devmasters.TextUtil.ShortenText(td.PlneJmeno, 250);
			d.PSC = td.Psc;
			d.RC = td.Rc;
			d.RizeniId = rizeniSpisovaZnacka;
			d.Role = td.Role;
			d.Typ = td.Typ;
			d.Zeme = td.Zeme;
			d.OsobaId = td.OsobaId;
			d.Zalozen = td.Zalozen;
			d.Odstranen = td.Odstranen;

			return (T)d;
		}

		private static Lib.Db.Insolvence.Dokumenty ToDbDokument(string rizeniSpisovaZnacka, Dokument td)
		{
			Lib.Db.Insolvence.Dokumenty d = new Lib.Db.Insolvence.Dokumenty();
			d.DatumVlozeni = td.DatumVlozeni < Entities.Insolvence.Rizeni.MinSqlDate
								? Entities.Insolvence.Rizeni.MinSqlDate : td.DatumVlozeni;
			d.DokumentId = td.Id;
			d.Length = (int)td.Lenght;
			d.Oddil = td.Oddil;
			d.Popis = td.Popis;
			d.RizeniId = rizeniSpisovaZnacka;
			d.TypUdalosti = td.TypUdalosti;
			d.Url = td.Url;
			d.WordCount = (int)td.WordCount;
			return d;
		}


	}
}