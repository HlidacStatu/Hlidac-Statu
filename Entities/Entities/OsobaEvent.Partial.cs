using Devmasters.Enums;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace HlidacStatu.Entities
{
    [MetadataType(typeof(OsobaEventMetadata))]
    public partial class OsobaEvent
        : IAuditable //, IValidatableObject
    {

        public OsobaEvent()
        {
            Created = DateTime.Now;
        }

        //Event k osobě
        public OsobaEvent(int osobaId, string title, string note, Types type)
        {
            OsobaId = osobaId;
            Title = title;
            Note = note;
            Type = (int)type;
            Created = DateTime.Now;
        }

        //Event k firmě
        public OsobaEvent(string ICO, string title, string note, Types type)
        {
            Ico = ICO;
            Title = title;
            Note = note;
            Type = (int)type;
            Created = DateTime.Now;
        }

        [ShowNiceDisplayName()]
        public enum Types
        {
            [NiceDisplayName("Speciální")]
            Specialni = 0,
            [NiceDisplayName("Volená funkce")]
            VolenaFunkce = 1,
            [NiceDisplayName("Soukromá pracovní")]
            SoukromaPracovni = 2,
            [NiceDisplayName("Osobní")]
            Osobni = 4,
            [NiceDisplayName("Veřejná správa pracovní")]
            VerejnaSpravaPracovni = 6,
            [NiceDisplayName("Politická")]
            Politicka = 7,
            [NiceDisplayName("Politická pracovní")]
            PolitickaPracovni = 9,
            [NiceDisplayName("Veřejná správa jiné")]
            VerejnaSpravaJine = 10,
            [NiceDisplayName("Jiné")]
            Jine = 11,
            [NiceDisplayName("Sociální sítě")]
            SocialniSite = 12,
            [NiceDisplayName("Centrální registr oznámení")]
            CentralniRegistrOznameni = 13
        }

        [JsonConverter(typeof(StringEnumConverter))]
        public enum SocialNetwork
        {
            Twitter = 0,
            Facebook_page = 1,
            Facebook_profile = 2,
            Instagram = 3,
            WWW =4,
            Youtube = 5,
            Zaznam_zastupitelstva = 6
        }

        [Flags]
        [ShowNiceDisplayName()]
        public enum Statuses
        {
            [NiceDisplayName("Skryto NP")]
            NasiPoliticiSkryte = 1,
            //[NiceDisplayName("Jen pro tesst")]
            //HlidacSkryte = 2
        }


        public void SetYearInterval(int year)
        {
            DatumOd = new DateTime(year, 1, 1);
            DatumDo = new DateTime(year, 12, 31);
        }

        // není nejrychlejší, ale asi stačí
        [NotMapped]
        public string TypeName
        {
            get
            {
                using (DbEntities db = new DbEntities())
                {
                    string result = db.EventType.AsQueryable()
                    .Where(type =>
                        type.Id == Type
                    )
                    .Select(type => type.Name)
                    .FirstOrDefault();

                    return result;
                }
            }
        }



        public OsobaEvent ShallowCopy()
        {
            return (OsobaEvent)MemberwiseClone();
        }





        public string ToAuditJson()
        {
            return JsonConvert.SerializeObject(this);
        }

        public string ToAuditObjectTypeName()
        {
            return "OsobaEvent";
        }

        public string ToAuditObjectId()
        {
            return Pk.ToString();
        }

        public static bool Compare(OsobaEvent a, OsobaEvent b)
        {
            return a.AddInfo == b.AddInfo
                && a.AddInfoNum == b.AddInfoNum
                && a.DatumDo == b.DatumDo
                && a.DatumOd == b.DatumOd
                && a.Organizace == b.Organizace
                && a.OsobaId == b.OsobaId
                && a.Status == b.Status
                && a.Title == b.Title
                && a.Type == b.Type;
        }



    }
}


