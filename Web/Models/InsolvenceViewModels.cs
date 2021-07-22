using HlidacStatu.Entities.Insolvence;
using System.Collections.Generic;


namespace HlidacStatu.Web.Models
{

	public class InsolvenceIndexViewModel
	{
		public Repositories.Searching.InsolvenceSearchResult NoveFirmyVInsolvenci { get; set; }
		public Repositories.Searching.InsolvenceSearchResult NoveOsobyVInsolvenci { get; set; }

		public InsolvenceIndexViewModel()
		{
			NoveFirmyVInsolvenci = new Repositories.Searching.InsolvenceSearchResult();
			NoveOsobyVInsolvenci = new Repositories.Searching.InsolvenceSearchResult();
		}
	}

	public class OsobaViewModel
	{
		public Osoba Osoba { get; set; }
		public string SpisovaZnacka { get; set; }
		public string UrlId { get; set; }
		public bool DisplayLinkToRizeni { get; set; }
	}

	public class PeopleListViewModel
	{
		public IEnumerable<OsobaViewModel> Osoby { get; set; }
		public bool ShowAsDataTable { get; set; }
		public string Typ { get; set; }
		public bool OnRadar { get; set; }

		public PeopleListViewModel()
		{
			
		}

		public PeopleListViewModel(IEnumerable<OsobaViewModel> osoby, string typ, bool showAsDataTable, bool onRadar)
		{
			Osoby = osoby;
			Typ = typ;
			ShowAsDataTable = showAsDataTable;
			OnRadar = onRadar;
		}
	}

	public class DokumentListViewModel
	{
		public string Oddil { get; set; }
		public Dokument[] Dokumenty { get; set; }
        public IReadOnlyDictionary<string, IReadOnlyCollection<string>> HighlightingData { get; set; }
	}

	public class SoudViewModel
	{
		public string Soud { get; set; }
	}

	public class StavRizeniViewModel
	{
		public string Stav { get; set; }
	}

	public class DokumentyViewModel
	{
		public string SpisovaZnacka { get; set; }
		public string UrlId { get; set; }
		public Dokument[] Dokumenty { get; set; }
        public IReadOnlyDictionary<string, IReadOnlyCollection<string>> HighlightingData { get; set; }

        public DokumentyViewModel()
		{
			Dokumenty = new Dokument[0];
		}
	}
}