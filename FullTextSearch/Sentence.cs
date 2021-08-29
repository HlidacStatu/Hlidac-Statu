using System.Collections.Generic;
using System.Linq;

namespace FullTextSearch
{
    public class Sentence<T>
    {
        public string Text
        {
            get => string.Join(" ", Tokens);
        }

        //public Guid Guid { get; } = Guid.NewGuid();
        public T Original { get; private set; }
        public List<Token<T>> Tokens { get; private set; } = new List<Token<T>>();

        private readonly ITokenizer _tokenizer;

        /// <summary>
        /// Classic constructor for creation of new sentence
        /// </summary>
        /// <param name="original">Object which is going to be Indexed</param>
        /// <param name="tokenizer">ITokenizer - set of functions to tokenize fulltext in original object</param>
        public Sentence(T original, ITokenizer tokenizer)
        {
            _tokenizer = tokenizer;
            Original = original;
            ParseSentence();
        }

        /// <summary>
        /// Constructor for deserialization only!
        /// </summary>
        /// <param name="serializableSentence">Deserialized sentence</param>
        /// <param name="tokenizer">ITokenizer - set of functions to tokenize fulltext in original object</param>
        public Sentence(SerializableSentence<T> serializableSentence, ITokenizer tokenizer)
        {
            _tokenizer = tokenizer;
            Original = serializableSentence.Original;
            CreateTokens(serializableSentence.Words);
        }

        /// <summary>
        /// Takes original object, finds [SearchAttribute] properties and tokenize them
        /// </summary>
        private void ParseSentence()
        {
            //todo: nefunguje, když T je typu string
            var objectValues = Original
                    .GetType()
                    .GetProperties()
                    .Where(p => p.CustomAttributes.Any(ca => ca.AttributeType == typeof(SearchAttribute)))
                    .Select(p => p.GetValue(Original)?.ToString());


            var words = objectValues.SelectMany(x => _tokenizer.Tokenize(x)); //.Distinct();

            CreateTokens(words);
        }

        /// <summary>
        /// Create tokens from tokenized words with backreference to this object
        /// </summary>
        private void CreateTokens(IEnumerable<string> tokenizedWords)
        {
            foreach (string word in tokenizedWords)
            {
                var token = new Token<T>(this, word);
                Tokens.Add(token);
            }
        }

        public override string ToString()
        {
            return Text;
        }

    }
}
