using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace HlidacStatu.RegistrVozidel.Models
{
    public class CSVConverters
    {
        public class AnoNeBooleanConverter : DefaultTypeConverter
        {
            public override object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
            {
                if (string.IsNullOrEmpty(text))
                    return null;
                if (text?.ToLower() == "ano")
                    return true;
                if (text?.ToLower() == "ne")
                    return false;
                else return null;
            }
        }
        public class IntConverter : DefaultTypeConverter
        {
            public override object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
            {
                return Devmasters.ParseText.ToInt(text.Trim());
            }
        }
        public class DecimalConverter : DefaultTypeConverter
        {
            public override object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
            {
                try
                {
                    return Devmasters.ParseText.ToDecimal(text.Trim());

                }
                catch (Exception)
                {

                    return null;
                }
            }
        }
    }
}
