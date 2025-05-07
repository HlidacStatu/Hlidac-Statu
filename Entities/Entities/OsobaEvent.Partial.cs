using Devmasters.Enums;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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
            [NiceDisplayName("Vazby")]
            [Display(Name="Vazby", Description = "Speciální vazby na firmy, nebo osoby")]
            Vazby = 0,
            [NiceDisplayName("Volená funkce")]
            [Display(Name="Volená funkce", Description = "funkce do které musela být osoba zvolena")]
            VolenaFunkce = 1,
            [NiceDisplayName("Soukromá pracovní")]
            [Display(Name="Soukromá pracovní", Description = "Povolání - ne nutně spojené s politikou")]
            SoukromaPracovni = 2,
            [NiceDisplayName("Osobní")]
            [Display(Name="Osobní", Description = "Další věci týkající se osoby (vzdělání, koníčky, ...)")]
            Osobni = 4,
            [NiceDisplayName("Veřejná správa exekutivní")]
            [Display(Name="Veřejná správa exekutivní", Description = "Funkce kam jsou lidi jmenováni nebo vybráni (úřady, st. podniky, p.o. atp)")]
            VerejnaSpravaExekutivni = 6,
            [NiceDisplayName("Politická")]
            [Display(Name="Politická", Description = "Události spojené s politickou stranou/hnutím a kandidaturou")]
            Politicka = 7,
            [NiceDisplayName("Politická exekutivní")]
            [Display(Name="Politická exekutivní", Description = "Funkce spojené s politickou exekutivní rolí")]
            PolitickaExekutivni = 9,
            [NiceDisplayName("Veřejná správa jiné")]
            [Display(Name="Veřejná správa jiné", Description = "Členové dozorčích rad, správních rad a jiných orgánů v úřadech a státních organizacích")]
            VerejnaSpravaJine = 10,
            [NiceDisplayName("Jiné")]
            [Display(Name="Jiné", Description = "Vše, co by nešlo zařadit do jiné kategorie")]
            Jine = 11,
            [NiceDisplayName("Sociální sítě")]
            [Display(Name="Sociální sítě", Description = "Odkazy na profil na sociálních sítích")]
            SocialniSite = 12,
            [NiceDisplayName("Centrální registr oznámení")]
            [Display(Name="Centrální registr oznámení", Description = "Události z CRO - aktuálně neaktualizované")]
            CentralniRegistrOznameni = 13,
            [NiceDisplayName("Politická strana")]
            [Display(Name="Politická strana", Description = "Události spojené s politickou stranou")]
            PolitickaStrana = 14,
            
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
            // Nulovou hodnotu nepřidávat do flagů, mohlo by to rozbít některé komponenty
            [NiceDisplayName("Skryto NP")]
            NasiPoliticiSkryte = 1,
            // [NiceDisplayName("Jen pro tesst")]
            // HlidacSkryte = 2,
            // [NiceDisplayName("test3")]
            // WhateverElse = 4
        }

        [NotMapped]
        public string TypeName => ((OsobaEvent.Types)Type).ToNiceDisplayName();


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

        public bool IsOverlaping(OsobaEvent other, out OsobaEvent mergedEvent)
        {
            int tolerance = 5; //days
            mergedEvent = default;

            if (Type == other.Type
                && string.Equals(AddInfo?.Trim() ?? "", other.AddInfo?.Trim() ?? "", StringComparison.InvariantCultureIgnoreCase)
                && string.Equals(Organizace?.Trim() ?? "", other.Organizace?.Trim() ?? "", StringComparison.InvariantCultureIgnoreCase)
                && string.Equals(AddInfo?.Trim() ?? "", other.AddInfo?.Trim() ?? "", StringComparison.InvariantCultureIgnoreCase)
                && string.Equals(Ico?.Trim() ?? "", other.Ico?.Trim() ?? "", StringComparison.InvariantCultureIgnoreCase))
            {
                var origDatumDo = DatumDo?.AddDays(tolerance) ?? DateTime.MaxValue;
                var origDatumOd = DatumOd?.AddDays(-tolerance) ?? DateTime.MinValue;
                var otherDatumDo = other.DatumDo?.AddDays(tolerance) ?? DateTime.MaxValue;
                var otherDatumOd = other.DatumOd?.AddDays(-tolerance) ?? DateTime.MinValue;
                
                if (origDatumDo >= otherDatumOd && origDatumDo <= otherDatumDo)
                {
                    mergedEvent = this.ShallowCopy();
                    mergedEvent.DatumDo = other.DatumDo;
                    return true;
                }

                if (origDatumOd >= otherDatumOd && origDatumOd <= otherDatumDo)
                {
                    mergedEvent = this.ShallowCopy();
                    mergedEvent.DatumOd = other.DatumOd;
                    return true;
                }

            }

            return false;
        }



    }
}


