using HlidacStatu.RegistrVozidel.Models;
using KellermanSoftware.CompareNetObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace HlidacStatu.RegistrVozidel
{
    public static class Util
    {
        static string NormalizeValue(object? value)
        {
            if (value is null) return "";

            return value switch
            {
                DateTime dt => dt.ToString("yyyy-MM-dd HH:mm:ss.ffffff", System.Globalization.CultureInfo.InvariantCulture),
                DateOnly date => date.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
                bool b => b ? "1" : "0",
                decimal m => m.ToString("G29", System.Globalization.CultureInfo.InvariantCulture),
                double d => d.ToString("G17", System.Globalization.CultureInfo.InvariantCulture),
                float f => f.ToString("G9", System.Globalization.CultureInfo.InvariantCulture),
                int i => i.ToString(System.Globalization.CultureInfo.InvariantCulture),
                long l => l.ToString(System.Globalization.CultureInfo.InvariantCulture),
                _ => value.ToString() ?? ""
            };
        }

        static string Canonicalize(string s)
            => s.Trim().Replace("\r\n", "\n").Replace("\r", "\n").Replace("\t", " ");

        public static string ComputeCheckSum(ICheckDuplicate obj)
        {


            var props = obj.GetType()
                .GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public)
                .Select(p => new
                {
                    Prop = p,
                    NameAttr = p.GetCustomAttribute<CsvHelper.Configuration.Attributes.NameAttribute>(inherit: true)
                })
                .Where(x => x.NameAttr is not null)
                .OrderBy(x => x.NameAttr!.Names?.FirstOrDefault() ?? "", System.StringComparer.Ordinal)
                .ThenBy(x => x.Prop.Name, System.StringComparer.Ordinal);

            var sb = new System.Text.StringBuilder(capacity: 2048);
            foreach (var x in props)
            {
                var headerName = x.NameAttr!.Names?.FirstOrDefault() ?? x.Prop.Name;
                var val = Canonicalize(NormalizeValue(x.Prop.GetValue(obj)));
                sb.Append(headerName).Append('=').Append(val).Append('\u001F'); // unit-separator
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
            var hash = System.Security.Cryptography.SHA256.HashData(bytes);
            return System.Convert.ToHexString(hash); // uppercase hex
        }
    }

}
