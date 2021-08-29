using System;
using System.Collections.Generic;

namespace FullTextSearch
{
    public class Token<T> : IEquatable<Token<T>>
    {
        private readonly int _wordHash;
        public string Word { get; }
        public List<Sentence<T>> Sentences { get; set; } = new List<Sentence<T>>();

        public Token(Sentence<T> sentence, string word)
        {
            Word = word;
            Sentences.Add(sentence);
            _wordHash = Word.GetHashCode();
        }

        public void MergeWith(Token<T> merged)
        {
            if (this == merged) return; //fuse to not merge myself with myself

            foreach (var sentence in merged.Sentences)
            {
                Sentences.Add(sentence);
                for (int i = 0; i < sentence.Tokens.Count; i++)
                {
                    if (sentence.Tokens[i].Equals(merged))
                    {
                        sentence.Tokens[i] = this;
                    }
                }
            }
        }

        /// <summary>
        /// Shortcut for Token.Word.StartsWith
        /// </summary>
        public bool StartsWith(string value)
        {
            //return Word.StartsWith(value);
            return Word.StartsWith(value, StringComparison.Ordinal);
        }

        public override string ToString()
        {
            return Word;
        }

        public override int GetHashCode()
        {
            return _wordHash;
        }

        public bool Equals(Token<T> other)
        {
            return _wordHash == other?.GetHashCode();
            //return Word.Equals(other.Word, StringComparison.Ordinal);
        }
    }
}
