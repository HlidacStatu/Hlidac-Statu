using System.ComponentModel;

namespace JsonRepairSharp_CLI
{
    public enum OptionTypeEnum
    {
        /// <summary>
        /// A Long Name for an Option, e.g. --opt.
        /// </summary>
        LongName,

        /// <summary>
        /// A Short Name for an Option, e.g. -o.
        /// </summary>
        ShortName,

        /// <summary>
        /// A Symbol, that is neither a switch, nor an argument.
        /// </summary>
        Symbol,

        /// <summary>
        /// Not yet defined.
        /// </summary>
        Undefined
    }

    /// <summary>
    /// An option passed by a Command Line application.
    /// </summary>
    public class CommandLineOption
    {
        /// <summary>
        /// The Name of the Option.
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// The Value associated with this Option.
        /// </summary>
        public string Value { get; set; } = "";

        /// <summary>
        /// The Type of this Option.
        /// </summary>
        public OptionTypeEnum OptionType { get; set; } = OptionTypeEnum.Undefined;
    }


    public class CommandLineArguments : Dictionary<string, CommandLineOption>
    {
        public CommandLineArguments GetSymbols()
        {
            var dict = new CommandLineArguments();
            foreach (var item in this) { if (item.Value.OptionType == OptionTypeEnum.Symbol) dict.Add(item.Key,item.Value); }
            return dict;
        }

        public CommandLineArguments GetOptions()
        {
            var dict = new CommandLineArguments();
                foreach (var item in this) { if (item.Value.OptionType == OptionTypeEnum.LongName || item.Value.OptionType == OptionTypeEnum.ShortName) dict.Add(item.Key, item.Value); }
                return dict;
        }

        public bool Value(string key1)
        {
            string[] disable = {"off","no","0", "disable", "disabled","false"};
            if (this.ContainsKey(key1))
            {
                // Switch exist and no value, so true
                var value = this[key1].Value;
                if (string.IsNullOrEmpty(value)) return true;
                // Switch exist and has disabled value, so false
                if (disable.Contains(value)) return false;
                // Switch exist and does not have disabled value, so true
                return true;
            }
            // Switch does not exist, so false
            return false;
        }

        public bool ContainsNoKey()
        {
            return this.Count==0;
        }

        public bool ContainsKey(string key1, string key2)
        {

            if (this.ContainsKey(key1)) return true;
            return this.ContainsKey(key2);
        }

        public bool ContainsKey(string key1, string key2, string key3)
        {

            if (this.ContainsKey(key1)) return true;
            if (this.ContainsKey(key2)) return true;
            return this.ContainsKey(key3);
        }

        public T? ValueOrDefault<T>(string key1, T? defaultValue)
        {
            bool succes = false;
            T? value     = defaultValue;
            if (this.ContainsKey(key1)) value = Convert<T>(this[key1].Value,out succes);
            return succes ? value : defaultValue;
        }

        public T? ValueOrDefault<T>(string key1, string key2, T? defaultValue)
        {
            bool succes = false;
            T? value = defaultValue;
            if (this.ContainsKey(key1)) value = Convert<T>(this[key1].Value, out succes); if (succes) return value;
            if (this.ContainsKey(key2)) value = Convert<T>(this[key2].Value, out succes); return succes ? value : defaultValue;
        }

        public T? ValueOrDefault<T>(string key1, string key2, string key3, T? defaultValue)
        {
            bool succes = false;
            T? value = defaultValue;
            if (this.ContainsKey(key1)) value = Convert<T>(this[key1].Value, out succes); if (succes) return value;
            if (this.ContainsKey(key2)) value = Convert<T>(this[key2].Value, out succes); if (succes) return value;
            if (this.ContainsKey(key3)) value = Convert<T>(this[key3].Value, out succes); return succes ? value : defaultValue;
        }

        public static T? Convert<T>(string input, out bool succes)
        {
            try
            {
                var converter = TypeDescriptor.GetConverter(typeof(T));
                succes = true;
                // Cast ConvertFromString(string text) : object to (T)
                return (T?)converter.ConvertFromString(input);
            }
            catch (Exception)
            {
                succes = false;
                return default(T);
            }
        }


    }

    public static class CommandLineParser
    {
        private static bool _argumentToLowerCase;
        private static bool _valueToLowerCase;

        public static CommandLineArguments ParseOptions(string[] arguments, bool argumentToLowerCase = false, bool valueToLowerCase = false)
        {
            _argumentToLowerCase = argumentToLowerCase;
            _valueToLowerCase    = valueToLowerCase;
            // Holds the Results:
            var results = new CommandLineArguments();

            CommandLineOption? lastOption = null;

            foreach (string argument in arguments)
            {
                // What should we do here? Go to the next one:
                if (string.IsNullOrWhiteSpace(argument))
                {
                    continue;
                }

                // We have found a Long-Name option:
                if (argument.StartsWith("--", StringComparison.Ordinal))
                {
                    // The previous argument was an option, too. Let's give it back:
                    if (lastOption != null)
                    {
                        results.Add(lastOption.Name, lastOption);
                    }

                    lastOption = new CommandLineOption
                    {
                        OptionType = OptionTypeEnum.LongName,
                        Name       = argument.Substring(2)
                    };
                    ToLower(lastOption);
                }
                // We have found a Short-Name option:
                else if (argument.StartsWith("-", StringComparison.Ordinal))
                {
                    // The previous argument was an option, too. Let's give it back:
                    if (lastOption != null)
                    {
                        results.Add(lastOption.Name, lastOption);
                    }

                    lastOption = new CommandLineOption
                    {
                        OptionType = OptionTypeEnum.ShortName,
                        Name = argument.Substring(1)
                    };
                    ToLower(lastOption);
                }
                // We have found a symbol:
                else if (lastOption == null)
                {
                    var option = new CommandLineOption
                    {
                        OptionType = OptionTypeEnum.Symbol,
                        Name       = argument
                    };
                    ToLower(lastOption);
                    results.Add(argument,option);
                    
                }
                // And finally this is a value:
                else
                {
                    // Set the Value and return this option:
                    lastOption.Value = argument;
                    ToLower(lastOption);
                    results.Add(lastOption.Name, lastOption);

                    // And reset it, because we do not expect multiple parameters:
                    lastOption = null;
                }

            }

            if (lastOption != null)
            {
                ToLower(lastOption);
                results.Add(lastOption.Name, lastOption);
            }

            return results;
        }

        private static void ToLower(CommandLineOption? lastOption)
        {
            if (lastOption==null) return;
            if (_valueToLowerCase)    lastOption.Value = lastOption.Value.ToLower();
            if (_argumentToLowerCase) lastOption.Name  = lastOption.Name.ToLower();
        }
    }
}
