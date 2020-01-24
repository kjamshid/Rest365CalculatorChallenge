using Calculator.Core.Interfaces;
using Calculator.Service;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System;

namespace Calculator.UnitTests
{
    public class Tests
    {
        ICalculatorService calculatorService = null;
        [SetUp]
        public void Setup()
        {
            var serviceProvider = new ServiceCollection()
                 .AddLogging()
                .AddSingleton<ICalculatorService, CalculatorService>()
                .BuildServiceProvider();

            calculatorService = serviceProvider.GetService<ICalculatorService>();

        }

        //[Test]
        [TestCase("1,2", 2, 3)]
        [TestCase("1,3", 2, 4)]
        [TestCase("1, ", 2, 1)]
        [TestCase("1,", 2, 1)]
        [TestCase("1 ", 1, 1)]
        [TestCase("1", 1, 1)]
        [TestCase("-1", 1, -1)]
        [TestCase("-1,-4", 2, -5)]
        [TestCase("10,-4", 2, 6)]
        [TestCase("10 , -4", 2, 6)]
        [TestCase(" 10 , -4 ", 2, 6)]
        [TestCase("", 1, 0)]
        [TestCase(" ", 1, 0)]
        [TestCase(",", 2, 0)]
        [TestCase(" ,", 2, 0)]
        [TestCase(" , ", 2, 0)]
        [TestCase(" , fdfddf", 2, 0)]
        public void AddNumbers_TwoNumbersMaxLimit_PositiveTests(string input, int length, int result)
        {
            int[] numbers = calculatorService.processInput(input);

            Assert.IsTrue(numbers.Length == length);

            var total = calculatorService.AddNumbers(numbers);
            
            Assert.IsTrue(total == result);
        }


        [TestCase("1000,1111,11111")]
        [TestCase("1000, ,11111")]
        [TestCase("1000,,11111")]
        [TestCase("1000,1111,fdfdfd")]
        [TestCase(null)]
        public void AddNumbers_TwoNumbersMaxLimit_ExceptionTests(string input)
        {
            if (input == null)
            {
                Assert.Throws<ArgumentNullException>(() => calculatorService.processInput(input));
            }
            else
            {
                Assert.Throws<ArgumentException>(() => calculatorService.processInput(input));
            }
        }

    }
}