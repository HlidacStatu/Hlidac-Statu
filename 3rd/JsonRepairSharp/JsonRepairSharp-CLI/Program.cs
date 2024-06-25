using JsonRepairSharp;

namespace JsonRepairSharp_CLI
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var commandLineOptions = CommandLineParser.ParseOptions(args,true);

            if (commandLineOptions.ContainsKey("h", "help") || commandLineOptions.ContainsNoKey())
            {
                Console.WriteLine("Usage:\njsonrepair [\"inputfilename.json\"] {OPTIONS}\n");
                Console.WriteLine("options:");
                Console.WriteLine("--version,   -v                         Show application version");
                Console.WriteLine("--help,      -h                         Show help");
                Console.WriteLine("--new,       -n {\"outputfilename.json\"} Write to new file");
                Console.WriteLine("--overwrite, -o                         Replace the input file");
                Console.WriteLine("--llm,       -l                         Parse in LLM mode");
                return;
            }

            if (commandLineOptions.ContainsKey("v", "version"))
            {
                Console.WriteLine($"jsonrepair version-build {JsonRepairSharp.JsonRepair.GetVersion()}");
                return;
            }

            // Check if file name exists
            var symbols = commandLineOptions.GetSymbols();
            if (!symbols.ContainsNoKey())
            {
                var inputFileName = symbols.First().Key;
                if (!File.Exists(inputFileName)) { Console.WriteLine($"Input file {inputFileName} does not exist"); return;}

                var inputFile      = File.ReadAllText(inputFileName);
                var outputFileName = "";
                var outputFile     = "";

                if (commandLineOptions.ContainsKey("l", "llm"))
                {
                    JsonRepairSharp.JsonRepair.Context = JsonRepair.InputType.LLM;
                }

                if (commandLineOptions.ContainsKey("n", "new"))
                {
                    outputFileName = commandLineOptions.ValueOrDefault("n", "new", "");
                    if (string.IsNullOrEmpty(outputFileName))                        { Console.WriteLine("no output file given")                                 ; return; }
                    if (!Directory.Exists(Path.GetDirectoryName(outputFileName)))    { Console.WriteLine($"Output directory of {outputFileName} does not exist"); return; }
                    if (outputFileName == inputFileName && !commandLineOptions.ContainsKey("o", "overwrite")) { Console.WriteLine($"Output file is same as input file. Add --overwrite or -o if this is intended."); return; }

                } else if (commandLineOptions.ContainsKey("o", "overwrite"))
                {
                    outputFileName = inputFileName;
                }

                try
                {
                    outputFile = JsonRepairSharp.JsonRepair.RepairJson(inputFile);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Cannot parse json file: {e.Message}");
                }

                if (string.IsNullOrEmpty(outputFileName))
                {
                    // write to output
                    Console.WriteLine(outputFile);
                }
                else
                {
                    File.WriteAllText(outputFileName, outputFile);
                }
            }
            else
            {
                Console.WriteLine($"No input file is given");
            }
        }
    }
}