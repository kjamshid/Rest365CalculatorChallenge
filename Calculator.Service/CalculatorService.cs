﻿using Calculator.Common;
using Calculator.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Calculator.Service
{
    public class CalculatorService : ICalculatorService
    {
        private static Regex customSingleCharDelimiterRegex = new Regex(@"^\/\/(?<customSigleCharDelimiter>.)\n.+", RegexOptions.Compiled);
        private static Regex customMultiCharDelimitersRegex = new Regex(@"^\/\/(?<customMultiCharDelimiters>\[.+\])+\n.+", RegexOptions.Singleline | RegexOptions.Compiled);
        // Variable declartion to be set in constructor
        private const string DefaultDelimeter = ",";
        private readonly string PredefinedDelimeters = null;
        private readonly bool AllowTwoNumbersMaxConstraint;
        private readonly int MaximumValidNumbersAllowed;
        private readonly int InvalidNumberEntryDefaultValue;
        private readonly int FilterUpperBoundValue;
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
            FilterUpperBoundValue = configuration.GetValue<int>("AppSettings:FilteredUpperBoundValue");
        }

        /// <summary>
        /// This method will take the input entered from the user and will apply the following business logic
        /// 1) Try to parse user custom defined single charachter delimeter
        /// 2) Try to parse user custom defined multi charachter delimeters
        /// 3) Combine all custom delimiters above with predefined delimiters
        /// 4) Parse the input and grab the valid integer numbers
        /// 5) Remove/Filter the upper bound entry ex: n > 1000 
        /// </summary>
        /// <param name="input">user raw input</param>
        /// <returns>an array of valid integers</returns>
        public List<int> ParseValidNumbersFromInput(string input, CmdOptions options = null)
        {
            if (string.IsNullOrEmpty(input))
                return new List<int> { 0 };

            List<string> inputEntries = new List<string>();

            List<string> DelimiterList = new List<string>();
            if (!string.IsNullOrEmpty(input))
            {
                DelimiterList.AddRange(PredefinedDelimeters.Split('|'));

                // combining all the delimiters into one string array list
                var customSingleCharDelimiter = ParseSingleCharCustomDelimiter(input);
                
                if (customSingleCharDelimiter != string.Empty)
                {
                    DelimiterList.Add(customSingleCharDelimiter);
                }
                else
                {
                    DelimiterList.AddRange(ParseMultiCharCustomDelimiters(input));
                }

                
                if(options?.Delimiter != null && !DelimiterList.Contains(options.Delimiter))
                {
                    DelimiterList.Add(options.Delimiter);
                }

                // parse the input using the config delimeter list along with custom user defined ones above
                inputEntries.AddRange(input.Split(DelimiterList.ToArray(), StringSplitOptions.None));
            }

            List<int> validNumberEntries = new List<int>();
            int validNumber;
            
            // Checking for valid inputs, if not a valid integer converting to default value (ex. 0)
            foreach(var inputEntry in inputEntries)
            {
                if (int.TryParse(inputEntry.Trim(), out validNumber))
                {
                    validNumberEntries.Add(validNumber);
                }
                else
                {
                    validNumberEntries.Add(InvalidNumberEntryDefaultValue);
                }
            }

            int upperBoundValue = (options == null || options.UpperBound <= 0) ? FilterUpperBoundValue : options.UpperBound;
            ReplaceInvalidUpperBoundNumbers(validNumberEntries, upperBoundValue);

            if (options == null || !options.AllowNegative)
            {
                // negative number constraint check
                CheckForNegativeNumbers(validNumberEntries);
            }

            return validNumberEntries;
        }

        /// <summary>
        ///  Try to detect Single character delimiter in following format //{delimiter}\n{numbers}
        /// 
        ///     examples: //#\n2#5 will return 7; //,\n2,ff,100 will return 102
        /// 
        ///   Please note that since there is a minimum of 4 characters needed to define a delimiter, the string
        ///   obviously need to be longer than 4 characters to define a number as well. The delimiter is matched
        ///   by following regex: ^\/\/(?<customSigleCharDelimiter>.)\n.+
        ///
        ///   All the characters used to define a delimiter is then removed in the string because these are
        ///   not needed for the further processing
        /// </summary>
        /// <param name="input">user input (the custom delimiter is removed if regex is matched) </param>
        /// <returns>single or multi character(s)</returns>
        public string ParseSingleCharCustomDelimiter(string input)
        {
            string customDelimiter = string.Empty;

            // minimum 4 characters needed to define a custom single character delimiter
            if((input ?? string.Empty).Length > 4)
            {
                //Matching the custom delimiter regex
                Match delimiterMatch = customSingleCharDelimiterRegex.Match(input);

                if (delimiterMatch.Success)
                {
                    // retreiving the delimiter and removing the 4 characters that no longer is needed
                    customDelimiter = delimiterMatch.Groups["customSigleCharDelimiter"].Value;
                    input = input.Replace(string.Concat(@"//", customDelimiter, "\n"), string.Empty);
                }
            }

            return customDelimiter;
        }


        /// <summary>
        /// Parsing custom user define custom delimiter
        /// try to detect user defined custom delimiter(s) of any length in following formats:
        ///                     //[{delimiter}]\n{numbers}
        ///                     //[{delimiter1}][{delimiter2}]...\n{numbers}
        /// 
        ///     example: //[***]\n11***22***33 will return 66
        ///     example: //[*][!!][r9r]\n11r9r22*hh*33!!44 will return 110
        /// 
        ///  Please note that since there is a minimum of 6 characters needed to define a delimiter, the string
        ///  obviously need to be longer than 6 characters to define a number as well. 
        ///  in order to acheive this The delimiter is matched against the following Regex and the value is extracted
        ///  from the matching group:
        ///  
        ///         regex: ^\/\/(?<customMultiCharDelimiters>\[.+\])+\n.+
        ///     
        ///   All the characters used to define a delimiter is then removed in the string because these are
        ///   not needed for the further calculation processing
        /// </summary>
        /// <param name="input">user input (the custom delimiter(s) is removed if regex is matched)</param>
        /// <returns>list of multi characters custom delimeter(s)</returns>
        public List<string> ParseMultiCharCustomDelimiters(string input)
        {
            List<string> customDelimiters = new List<string>();

            // minimum 6 characters needed to define a custom single character delimiter
            if ((input ?? string.Empty).Length > 6)
            {
                Match delimiterMatches = customMultiCharDelimitersRegex.Match(input);
                if (delimiterMatches.Success)
                {
                    List<char> delimChars = new List<char>();
                    input = input.Replace(string.Concat(@"//", delimiterMatches.Groups["customMultiCharDelimiters"].Value, "\n"), string.Empty);
                    customDelimiters = (delimiterMatches.Groups["customMultiCharDelimiters"].Value ?? string.Empty).TrimStart('[').TrimEnd(']').Split("][", StringSplitOptions.RemoveEmptyEntries).ToList();
                }
            }


            return customDelimiters;
        }

        /// <summary>
        /// Checking for negative numbers through the list and throwing an exception with those negative numbers included in the message
        /// 
        /// Exception:
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
        /// <param name="upperBoudValue">Upper bound value to be replaced by default (ex: 0)</param>
        private void ReplaceInvalidUpperBoundNumbers(List<int> numbers, int upperBoudValue)
        {
            if (numbers != null)
            {
                for (int i = 0; i < numbers.Count; i++)
                {
                    if (numbers[i] > upperBoudValue)
                    {
                        numbers[i] = InvalidNumberEntryDefaultValue;
                    }
                }
            }
        }

        /// <summary>
        /// This method will take an integer array of numbers and based on operation parameter passed will perform addition, subtraction, division
        /// or multipication on the numbers, the default operation is addition
        /// 
        /// Exceptions:
        /// 
        /// 1) ArguementException: If two numbers maximum constrains allowed is set to true in config file and there are more than two valid numbers, it will result
        ///    in an arguement exception
        /// 2) NegativeNumberException: Any negative number will result in an exception containing the negative number entries
        /// </summary>
        /// <param name="numbers">List of positive integer array</param>
        /// <returns></returns>
        public int PerformOperationOnNumbers(List<int> numbers, char operation = '+')
        {
            if (numbers == null)
                return 0;
            
            // if list of integer entries greater than maximum allowed (ex. 2)
            if (AllowTwoNumbersMaxConstraint && numbers.Count > MaximumValidNumbersAllowed)
            {
                throw new ArgumentException($"constraint violation: only maximum {MaximumValidNumbersAllowed} numbers are allowed");
            }

            int total = 0;
            
            for(int i = 0; i < numbers.Count; i++)
            {
                if (operation != '*' && numbers[i] == 0) continue;

                if (i == 0)
                {
                    total = numbers[i];
                    continue;
                }

                switch(operation)
                {
                    case '+':
                        total += numbers[i];
                        break;
                    case '-':
                        total -= numbers[i];
                        break;
                    case '*':
                        total *= numbers[i];
                        break;
                    case '/':
                        total /= numbers[i];
                        break;
                    default:
                        total += numbers[i];
                        break;
                }
            }

            return total;
        }

    }
}
