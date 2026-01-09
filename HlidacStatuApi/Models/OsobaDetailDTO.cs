using Devmasters.Enums;
using HlidacStatu.Entities;
using HlidacStatu.Extensions;
using Newtonsoft.Json;

namespace HlidacStatuApi.Models
{
    public class OsobaDetailDTO
    {
        public static async Task<OsobaDetailDTO> CreateOsobaDetailDTOAsync(Osoba o)
        {
            var unwantedEvents = new int[]
            {
                (int)OsobaEvent.Types.Osobni,
                (int)OsobaEvent.Types.CentralniRegistrOznameni,
                (int)OsobaEvent.Types.SocialniSite,
            };

            
            var dto = new OsobaDetailDTO()
            {
                TitulPred = o.TitulPred,
                Jmeno = o.Jmeno,
                Prijmeni = o.Prijmeni,
                TitulPo = o.TitulPo,
                Narozeni = o.Narozeni,
                NameId = o.NameId,
                PolitickaStrana = await o.CurrentPoliticalPartyAsync(),
                Profile = o.GetUrl(),
                Udalosti = o.NoFilteredEvents(ev => !unwantedEvents.Contains(ev.Type))
                    .Select(ev => new OsobaEventDTO()
                    {
                        Castka = ev.AddInfoNum,
                        DatumOd = ev.DatumOd,
                        DatumDo = ev.DatumDo,
                        Role = ev.AddInfo,
                        Typ = ((OsobaEvent.Types)ev.Type).ToNiceDisplayName(),
                        Organizace = ev.Organizace
                    }).ToList(),
                SocialniSite = o.NoFilteredEvents(ev => ev.Type == (int)OsobaEvent.Types.SocialniSite)
                    .Select(ev => new SocialNetworkDTO(ev))
                    .ToList()
            };

            List<OsobaEventDTO> sponzoringList = [];
            foreach (var sponzoring in await o.SponzoringAsync())
            {
                sponzoringList.Add(new OsobaEventDTO()
                {
                    Castka = sponzoring.Hodnota,
                    DatumOd = sponzoring.DarovanoDne,
                    DatumDo = sponzoring.DarovanoDne,
                    Typ = "sponzor",
                    Organizace = await sponzoring.JmenoPrijemceAsync()
                });
            }
            
            dto.Sponzoring = sponzoringList;


            return dto;
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