using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Components;

namespace HlidacStatu.Entities
{
    
    public class Errors : IEnumerable<Errors.ErrorMessage>
    {
        public record ErrorMessage(string Message, MessageSeverity Severity);
        public enum MessageSeverity
        {
            Info,
            Warning,
            Error
        }

        private List<ErrorMessage> _errors = new();
        
        public void Add(string message, MessageSeverity severity) => _errors.Add(new ErrorMessage(message, severity));
        public void Add(ErrorMessage message)
        {
            if(message != null) 
                _errors.Add(message);
        }
        public void Add(Errors errors)
        {
            if (errors != null && errors.Count > 0)
                _errors.AddRange(errors._errors);
            
        }


        public bool HasErrors => _errors.Any(e => e.Severity == MessageSeverity.Error);
        public bool HasWarnings => _errors.Any(e => e.Severity == MessageSeverity.Warning);
        public bool HasInfo => _errors.Any(e => e.Severity == MessageSeverity.Info);

        public int Count => _errors.Count;
        public IReadOnlyCollection<ErrorMessage> GetAll() => _errors.AsReadOnly();


        public override string ToString() => ToString("\n");
        public string ToString(string separator)
        {
            if (_errors.Count == 0)
                return "";
            
            return string.Join(separator, _errors.Select(e => e.Message));
        }
        // to markup string
        public MarkupString ToMarkupString(string separator = "<br />")
        {
            if (_errors.Count == 0)
                return new MarkupString("");
            
            
            return new MarkupString(ToString(separator));
        }
        
        
        // Indexer
        public ErrorMessage this[int index] => _errors[index];

        // Enumerator
        public IEnumerator<ErrorMessage> GetEnumerator() => _errors.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        
    }

}