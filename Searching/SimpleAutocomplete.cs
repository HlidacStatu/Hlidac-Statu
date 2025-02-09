using Devmasters;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HlidacStatu.Searching
{
    public class SimpleAutocomplete
    {
        public interface IGetHits
        {
            decimal GetHits(string[] querywords);
        }

        public class LocalItem(string text, string id) : IGetHits
        {
            public string Text => text;
            public string Id => id;

            string[] _nameInWords = null;
            protected string[] NameInWords
            {
                get
                {
                    if (_nameInWords == null)
                        _nameInWords = ConvertToWords(text);
                    return _nameInWords;
                }
            }
            protected string[] ConvertToWords(string value)
            {
                return value.RemoveDiacritics().Split(' ', '.', ',');
            }
            public virtual decimal GetHits(string[] querywords)
            {
                decimal hits = 0m;
                foreach (var word in querywords)
                {

                    var startWithCount = NameInWords.Count(m => m.StartsWith(word, StringComparison.InvariantCultureIgnoreCase)) * 1.2m;
                    if (startWithCount > 0)
                        hits += startWithCount;
                    else
                        hits += NameInWords.Count(m => m.Contains(word, StringComparison.InvariantCultureIgnoreCase)) * 0.8m;
                }
                return hits;
            }
        }
    }
}
