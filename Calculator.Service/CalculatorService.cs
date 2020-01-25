using Calculator.Common;
using Calculator.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Calculator.Service
{
    public class CalculatorService : ICalculatorService
    {
        private static Regex customSingleCharDelimiterRegex = new Regex(@"^\/\/(?<customSigleCharDelimiter>.)\n.+", RegexOptions.Compiled);
        // Variable declartion to be set in constructor
        private const string DefaultDelimeter = ",";
        private readonly string PredefinedDelimeters = null;
        private readonly bool AllowTwoNumbersMaxConstraint;
        private readonly int MaximumValidNumbersAllowed;
        private readonly int InvalidNumberEntryDefaultValue;
        private readonly int FilteredUpperBoundValue;
        private readonly ILogger<CalculatorService> _logger;

        // Will load LoggerFactory and Configuration Root through Dependency injection (on application start up)
        public CalculatorService(ILoggerFactory loggerFactory, IConfiguration configuration)
        {
            _logger = loggerFactory.CreateLogger<CalculatorService>();

            // Getting the app settings values
            AllowTwoNumbersMaxConstraint = configuration.GetValue<bool>("AppSettings:AllowTwoNumbersMaxConstraint");
            PredefinedDelimeters = configuration.GetValue<string>("AppSettings:InputDelimeters");
            MaximumValidNumbersAllowed = AllowTwoNumbersMaxConstraint ? configuration.GetValue<int>("AppSettings:MaximumValidNumbersAllowed") : 0;
            InvalidNumberEntryDefaultValue = configuration.GetValue<int>("AppSettings:InvalidNumberEntryDefaultValue");
            FilteredUpperBoundValue = configuration.GetValue<int>("AppSettings:FilteredUpperBoundValue");
        }

        /// <summary>
        /// This method will take the input entered from the user and will apply the following business logic
        /// 1) Support a maximum of 2 numbers using a comma delimiter. Throw an exception when more than 2 numbers are provided
        ///         examples: 20 will return 20; 1,5000 will return 5001; 4,-3 will return 1
        ///         empty input or missing numbers should be converted to 0
        ///         invalid numbers should be converted to 0 e.g. 5,tytyt will return 5
        /// </summary>
        /// <param name="input">user input</param>
        /// <returns>an array of valid integers</returns>
        public List<int> ParseInput(string input)
        {
            if (string.IsNullOrEmpty(input))
                return new List<int> { 0 };

            var customSingleCharDelimiter = ParseCustomSingleCharacterDelimiter(input);

            // using the config delimeter list provided to parse the input
            string[] inputEntries = input.Split(string.Concat(PredefinedDelimeters, customSingleCharDelimiter).ToCharArray());

            List<int> numberEntries = new List<int>();

            int validNumber;
            
            // Checking for valid inputs, if not a valid integer converting to default value (ex. 0)
            foreach(var inputEntry in inputEntries)
            {
                if (int.TryParse(inputEntry.Trim(), out validNumber))
                {
                    numberEntries.Add(validNumber);
                }
                else
                {
                    numberEntries.Add(InvalidNumberEntryDefaultValue);
                }
            }

            return numberEntries;
        }

        /// <summary>
        /// Parsing custom user define single character delimiter in following format //{delimiter}\n{numbers}
        /// Please note that since there is a minimum of 4 characters needed to define a delimiter, the string
        /// obviously need to be longer than 4 characters to define a number as well. The delimiter is matched
        /// by following regex: ^\/\/(?<customSigleCharDelimiter>.)\n.+
        /// and all the 4 characters used to define a delimiter is then removed in the string because these are
        /// not needed for the further processing
        /// </summary>
        /// <param name="input">user input</param>
        /// <returns>raw input without the custom delimiter 4 characters</returns>
        public string ParseCustomSingleCharacterDelimiter(string input)
        {
            string customDelimiter = string.Empty;

            // minimum 4 characters needed to define a custom single character delimiter
            if(input.Length > 4)
            {
                //Matching the custom delimiter regex
                Match delimiterMatch = customSingleCharDelimiterRegex.Match(input);

                if(delimiterMatch.Success)
                {
                    // retreiving the delimiter and removing the 4 characters that no longer is needed
                    customDelimiter = delimiterMatch.Groups["customSigleCharDelimiter"].Value;
                    input = input.Replace(string.Concat(@"//", customDelimiter, "\n"), string.Empty);
                }
            }

            return customDelimiter;
        }

        /// <summary>
        /// Checking for negative numbers through the list and throwing an exception with those negative numbers included in the message
        /// 
        /// Exception:
        /// 
        /// NegativeNumberException: if there are negative numbers in the set, providing those values in the message
        /// </summary>
        /// <param name="numbers"></param>
        private void CheckForNegativeNumbers(List<int> numbers)
        {
            if (numbers != null)
            {
                var negativeNumbers = numbers.Where(num => num < 0);

                // if any negative number detected throwing an exception
                if(negativeNumbers.Any())
                {
                    throw new NegativeNumberException($"Constraint violation - Negative numbers are not allowed: {string.Join(",", negativeNumbers)}");
                }
            }
        }

        /// <summary>
        /// Removing values based on the predicate
        /// </summary>
        /// <param name="numbers">List of numbers to be filtered</param>
        /// <param name="match">Condition of removal</param>
        private void FilterNumbers(List<int> numbers, Predicate<int> match)
        {
            if (numbers != null)
            {
               numbers.RemoveAll(match);
            }
        }

        /// <summary>
        /// This method will take an integer array of numbers and will use linq sum extension to iterate through % add them
        /// 
        /// Exceptions:
        /// 
        /// 1) If the maximum constrains allowed is set to true in config file and there are more than two valid numbers, it will result
        ///    in an arguement exception
        /// </summary>
        /// <param name="numbers">List of integer array</param>
        /// <returns></returns>
        public int AddNumbers(List<int> numbers)
        {
            if (numbers == null)
                return 0;

            // negative number constraint check
            CheckForNegativeNumbers(numbers);

            FilterNumbers(numbers, num => num > FilteredUpperBoundValue);

            // if list of integer entries greater than maximum allowed (ex. 2)
            if (AllowTwoNumbersMaxConstraint && numbers.Count > MaximumValidNumbersAllowed)
            {
                throw new ArgumentException($"constraint violation: only maximum {MaximumValidNumbersAllowed} numbers are allowed");
            }

            return numbers.Sum();
        }

    }
}
