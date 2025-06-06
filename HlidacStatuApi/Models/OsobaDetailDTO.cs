﻿using Devmasters.Enums;
using HlidacStatu.Entities;
using HlidacStatu.Extensions;
using Newtonsoft.Json;

namespace HlidacStatuApi.Models
{
    public class OsobaDetailDTO
    {
        public OsobaDetailDTO(Osoba o)
        {
            this.TitulPred = o.TitulPred;
            this.Jmeno = o.Jmeno;
            this.Prijmeni = o.Prijmeni;
            this.TitulPo = o.TitulPo;
            this.Narozeni = o.Narozeni;
            this.NameId = o.NameId;
            this.PolitickaStrana = o.CurrentPoliticalParty();
            this.Profile = o.GetUrl();
            this.Sponzoring = o.Sponzoring()
                .Select(ev => new OsobaEventDTO()
                {
                    Castka = ev.Hodnota,
                    DatumOd = ev.DarovanoDne,
                    DatumDo = ev.DarovanoDne,
                    Typ = "sponzor",
                    Organizace = ev.JmenoPrijemce()
                }).ToList();

            var unwantedEvents = new int[]
            {
                (int)OsobaEvent.Types.Osobni,
                (int)OsobaEvent.Types.CentralniRegistrOznameni,
                (int)OsobaEvent.Types.SocialniSite,
            };

            this.Udalosti = o.NoFilteredEvents(ev => !unwantedEvents.Contains(ev.Type))
                .Select(ev => new OsobaEventDTO()
                {
                    Castka = ev.AddInfoNum,
                    DatumOd = ev.DatumOd,
                    DatumDo = ev.DatumDo,
                    Role = ev.AddInfo,
                    Typ = ((OsobaEvent.Types)ev.Type).ToNiceDisplayName(),
                    Organizace = ev.Organizace
                }).ToList();

            this.SocialniSite = o.NoFilteredEvents(ev => ev.Type == (int)OsobaEvent.Types.SocialniSite)
                .Select(ev => new SocialNetworkDTO(ev))
                .ToList();
        }
        public string TitulPred { get; set; }
        public string Jmeno { get; set; }
        public string Prijmeni { get; set; }
        public string TitulPo { get; set; }

        public DateTime? Narozeni { get; set; }
        public string NameId { get; set; }
        public string Profile { get; set; }
        public string PolitickaStrana { get; set; }
        public List<OsobaEventDTO> Sponzoring { get; set; }
        public List<OsobaEventDTO> Udalosti { get; set; }
        public List<SocialNetworkDTO> SocialniSite { get; set; }

        public string ToJson()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}