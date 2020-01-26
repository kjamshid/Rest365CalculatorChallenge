using CommandLine;
using System.Text;

namespace Calculator.Common
{
    public class CmdOptions
    {
        [Option('d', "delimiter", Required = false, HelpText = "Delimiter to be used to parse the input (ex: -d \";\")")]
        public string Delimiter { get; set; }

        [Option('n', "negative", Required = false, HelpText = "Negative number allowed (ex: -n")]
        public bool AllowNegative { get; set; }

        [Option('h', "help", Required = false, HelpText = "Display help menu")]
        public bool Help { get; set; }

        [Option('u', "negative", Required = false, HelpText = "Upper bound  to filter (ex: -u 1000)")]
        public int UpperBound { get; set; }

        public string GetUsage
        {
            get
            {
                var customText = new StringBuilder();
                customText.AppendLine("Example Usage:");
                customText.AppendLine("Calculator.ConsoleApp -d \",\"");
                customText.AppendLine("Calculator.ConsoleApp -n");
                customText.AppendLine("Calculator.ConsoleApp -u 1000");
                customText.AppendLine("Calculator.ConsoleApp -d \",\" -n -u 1000");

                return customText.ToString();
            }

        }
    }
}
