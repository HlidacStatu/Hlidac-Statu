using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace FullTextSearch
{
    public class SerializableSentence<T>
    {
        public T Original { get; set; }
        public List<string> Words { get; set; }

        [JsonConstructor]
        public SerializableSentence()
        {
        }

        public SerializableSentence(Sentence<T> sentence)
        {
            Original = sentence.Original;
            Words = sentence.Tokens.Select(t => t.Word).ToList();
        }
    }
}