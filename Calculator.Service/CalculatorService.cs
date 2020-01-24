using Calculator.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Calculator.Service
{
    public class CalculatorService : ICalculatorService
    {
        private readonly ILogger<CalculatorService> _logger;

        string[] separator = new string[] { "," };

        List<char> delimeters = new List<char>() { ',' }; 
        public CalculatorService(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<CalculatorService>();
        }

        public int [] processInput(string input)
        {
            if (input == null)
                throw new ArgumentNullException("please enter a valid input!");
                    
            string[] inputEntries = input.Split(delimeters.ToArray());

            List<int> validNumbers = new List<int>();

            int validNumber;
            foreach(var inputEntry in inputEntries)
            {
                if (int.TryParse(inputEntry.Trim(), out validNumber))
                {
                    validNumbers.Add(validNumber);
                }
                else
                {
                    validNumbers.Add(0);
                }
            }

            if(validNumbers.Count > 2)
            {
                throw new ArgumentException("Your input contains more than two numbers");
            }

            return validNumbers.ToArray();
        }

        public int AddNumbers(int [] numbers)
        {
            return numbers.Sum();
        }

    }
}
