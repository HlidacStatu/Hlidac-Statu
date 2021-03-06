using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace FullTextSearch
{
    public class TokenTree<T> where T : IEquatable<T>
    {
        private Dictionary<char, Dictionary<string, Token<T>>> _innerStructure =
            new Dictionary<char, Dictionary<string, Token<T>>>();

        private const char NullChar = '_';

        public void AddTokens(List<Token<T>> tokens)
        {
            for (int i = 0; i < tokens.Count(); i++)
            {
                string word = tokens[i].Word;

                var subdictionary = GetSubdictionary(word);

                if (subdictionary.TryGetValue(tokens[i].Word, out Token<T> olderToken))
                {
                    olderToken.MergeWith(tokens[i]);
                }
                else
                {
                    subdictionary.Add(tokens[i].Word, tokens[i]);
                }
            }
        }

        public IEnumerable<Token<T>> FindTokens(string word)
        {
            var subdictionary = GetSubdictionary(word);

            return subdictionary
                .Where(t => t.Key.StartsWith(word, StringComparison.Ordinal))
                .Select(x => x.Value);
        }


        private Dictionary<string, Token<T>> GetSubdictionary(string word)
        {
            char key = GetKey(word);
            if (_innerStructure.TryGetValue(key, out var subdictionary))
            {
                return subdictionary;
            }

            var emptyDictionary = new Dictionary<string, Token<T>>(StringComparer.Ordinal);
            _innerStructure.Add(key, emptyDictionary);

            return emptyDictionary;
        }

        private char GetKey(string word)
        {
            return string.IsNullOrEmpty(word) ? NullChar : word.FirstOrDefault();
        }


        public byte[] Serialize()
        {
            var structure = _innerStructure
                .SelectMany(s => s.Value.SelectMany(x => x.Value.Sentences))
                .Distinct()
                .Select(v => new SerializableSentence<T>(v))
                .ToList();

            return JsonSerializer.SerializeToUtf8Bytes(structure);
            //return jsonUtf8Bytes;

            // JsonSerializerOptions serializerOptions = new JsonSerializerOptions()
            // {
            //     ReferenceHandler = ReferenceHandler.Preserve
            // };
            //
            //return JsonSerializer.Serialize(structure);


        }


    }

}