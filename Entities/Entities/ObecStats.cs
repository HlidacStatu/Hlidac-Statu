#nullable disable

using System.Collections.Generic;
using System.Linq;

namespace HlidacStatu.Entities
{
    public class ObecStats
    {
        public enum Attrs
        {
            Bankomat,
            Brownfield,
            COV,
            Dum_pro_seniory,
            Dum_s_pecovatelskou_sluzbou,
            Hasicska_zbrojnice,
            Herna,
            Hospic,
            Hrbitov,
            Infocentrum,
            Kulturni_objekt,
            LDN,
            Lekarna,
            Most,
            Obchod_s_potravinami,
            Obecni_urad,
            Ordinace_lekare,
            Parkoviste,
            Policejni_stanice,
            Posta,
            Restaurace,
            Rybniky_nadrze,
            Sber_recyklovatelneho_odpadu,
            Sberny_dvur,
            Skladovy_areal,
            Skola,
            Sportoviste,
            Trafostanice,
            Vodojem,

        }
        static Dictionary<Attrs, string> AttrsToStr = new Dictionary<Attrs, string>()
        {
            { Attrs.Bankomat, "sms_1"},
            { Attrs.Brownfield, "sms_2"},
            { Attrs.COV, "sms_3"},
            { Attrs.Dum_pro_seniory, "sms_4"},
            { Attrs.Dum_s_pecovatelskou_sluzbou, "sms_5"},
            { Attrs.Hasicska_zbrojnice, "sms_6"},
            { Attrs.Herna, "sms_7"},
            { Attrs.Hospic, "sms_8"},
            { Attrs.Hrbitov, "sms_9"},
            { Attrs.Infocentrum, "sms_10"},
            { Attrs.Kulturni_objekt, "sms_11"},
            { Attrs.LDN, "sms_12"},
            { Attrs.Lekarna, "sms_13"},
            { Attrs.Most, "sms_14"},
            { Attrs.Obchod_s_potravinami, "sms_15"},
            { Attrs.Obecni_urad, "sms_16"},
            { Attrs.Ordinace_lekare, "sms_17"},
            { Attrs.Parkoviste, "sms_18"},
            { Attrs.Policejni_stanice, "sms_19"},
            { Attrs.Posta, "sms_20"},
            { Attrs.Restaurace, "sms_21"},
            { Attrs.Rybniky_nadrze, "sms_22"},
            { Attrs.Sber_recyklovatelneho_odpadu, "sms_23"},
            { Attrs.Sberny_dvur, "sms_24"},
            { Attrs.Skladovy_areal, "sms_25"},
            { Attrs.Skola, "sms_26"},
            { Attrs.Sportoviste, "sms_27"},
            { Attrs.Trafostanice, "sms_28"},
            { Attrs.Vodojem, "sms_29"},
        };

        public class ValueYear
        {
            public int? Year { get; set; }
            public decimal? Value { get; set; }
        }
        public class Attr : ObceZUJAttr
        {
            public ObceZUJAttrName AttrName { get; set; }
        }

        public ObceZUJ Obec { get; set; }
        public Attr[] Stats { get; set; }

        private decimal? GetVal(string attrKey)
        {
            return GetValYear(attrKey)?.Value;
        }
        private ValueYear GetValYear(string attrKey)
        {
            return GetValYears(attrKey).FirstOrDefault() ?? null;
        }
        private IEnumerable<ValueYear> GetValYears(string attrKey)
        {
            if (Stats == null)
                return new ValueYear[] { };

            return Stats
                .Where(m => m.Key == attrKey)
                .Select(m => new ValueYear() { Year = m.Year, Value = m.Value })
                .OrderByDescending(m => m.Year);
        }
    }
}
