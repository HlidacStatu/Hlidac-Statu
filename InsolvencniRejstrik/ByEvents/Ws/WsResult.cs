using System;

namespace InsolvencniRejstrik.ByEvents
{
	class WsResult
	{
		public long Id { get; set; }
		public string DokumentUrl { get; set; }
		public string TypUdalosti { get; set; }
		public string PopisUdalosti { get; set; }
		public string SpisovaZnacka { get; set; }
		public string Oddil { get; set; }
		public string Poznamka { get; set; }
		public DateTime DatumZalozeniUdalosti { get; set; }

		public static WsResult From(IsirWs.isirWsPublicData item)
		{
			return new WsResult
			{
				Id = item.id,
				SpisovaZnacka = item.spisovaZnacka,
				TypUdalosti = item.typUdalosti,
				PopisUdalosti = item.popisUdalosti,
				DatumZalozeniUdalosti = item.datumZalozeniUdalosti,
				DokumentUrl = item.dokumentUrl,
				Oddil = item.oddil,
				Poznamka = item.poznamka,
			};
		}

		private const char Separator = '#';

		public static WsResult From(string item)
		{
			var idIndex = item.IndexOf(Separator, 0) + 1;
			var spisovaZnackaIndex = item.IndexOf(Separator, idIndex) + 1;
			var typUdalostiIndex = item.IndexOf(Separator, spisovaZnackaIndex) + 1;
			var popisUdalostiIndex = item.IndexOf(Separator, typUdalostiIndex) + 1;
			var datumZalozeniUdalostiIndex = item.IndexOf(Separator, popisUdalostiIndex) + 1;
			var dokumentUrlIndex = item.IndexOf(Separator, datumZalozeniUdalostiIndex) + 1;
			var oddilIndex = item.IndexOf(Separator, dokumentUrlIndex) + 1;

			if (oddilIndex < 0)
			{
				return null;
			}
			string sDatZalozeniUdalosti = item.Substring(popisUdalostiIndex, datumZalozeniUdalostiIndex - popisUdalostiIndex - 1);
			DateTime? dat = Devmasters.DT.Util.ToDateTime(sDatZalozeniUdalosti);
			if (dat.HasValue == false)
				dat = Devmasters.DT.Util.ToDateTime(sDatZalozeniUdalosti, "MM/dd/yyyy h:m:s tt");
			if (dat.HasValue==false)
				Console.WriteLine("sDatZalozeniUdalosti:" + sDatZalozeniUdalosti);
			return new WsResult
			{
				Id = Convert.ToInt64(item.Substring(0, idIndex - 1)),
				SpisovaZnacka = item.Substring(idIndex, spisovaZnackaIndex - idIndex - 1),
				TypUdalosti = item.Substring(spisovaZnackaIndex, typUdalostiIndex - spisovaZnackaIndex - 1),
				PopisUdalosti = item.Substring(typUdalostiIndex, popisUdalostiIndex - typUdalostiIndex - 1),
				DatumZalozeniUdalosti = dat.Value,
				DokumentUrl = item.Substring(datumZalozeniUdalostiIndex, dokumentUrlIndex - datumZalozeniUdalostiIndex - 1),
				Oddil = item.Substring(dokumentUrlIndex, oddilIndex - dokumentUrlIndex - 1),
				Poznamka = item.Substring(oddilIndex)?.Replace("@<fl>", "\n")?.Replace("@<cr>", "\r"),
			};
		}

		public string ToStringLine()
		{
			return $"{Id}#{SpisovaZnacka}#{TypUdalosti}#{PopisUdalosti}#{DatumZalozeniUdalosti}#{DokumentUrl}#{Oddil}#{Poznamka?.Replace("\n", "@<fl>")?.Replace("\r", "@<cr>")}";
		}

		protected bool Equals(WsResult other)
		{
			return Equals(Id, other.Id)
				&& Equals(DokumentUrl, other.DokumentUrl)
				&& Equals(TypUdalosti, other.TypUdalosti)
				&& Equals(PopisUdalosti, other.PopisUdalosti)
				&& Equals(SpisovaZnacka, other.SpisovaZnacka)
				&& Equals(Oddil, other.Oddil)
				&& Equals(Poznamka, other.Poznamka)
				&& Equals(DatumZalozeniUdalosti, other.DatumZalozeniUdalosti);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj))
			{
				return false;
			}
			if (ReferenceEquals(this, obj))
			{
				return true;
			}
			if (obj.GetType() != GetType())
			{
				return false;
			}
			return Equals((WsResult)obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				var result = Id.GetHashCode();
				result = (result * 397) ^ (DokumentUrl?.GetHashCode() ?? 0);
				result = (result * 397) ^ (TypUdalosti?.GetHashCode() ?? 0);
				result = (result * 397) ^ (PopisUdalosti?.GetHashCode() ?? 0);
				result = (result * 397) ^ SpisovaZnacka.GetHashCode();
				result = (result * 397) ^ (Oddil?.GetHashCode() ?? 0);
				result = (result * 397) ^ (Poznamka?.GetHashCode() ?? 0);
				result = (result * 397) ^ DatumZalozeniUdalosti.GetHashCode();
				return result;
			}
		}

	}
}
