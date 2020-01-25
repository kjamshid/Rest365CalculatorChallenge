using Calculator.Common;
using Calculator.Core.Interfaces;
using Calculator.Service;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;
using System;
using System.IO;

namespace Calculator.UnitTests
{
    public class CalculatorUnitTests
    {
        ICalculatorService _calculatorService = null;
        IServiceProvider _serviceProvider = null;
        Mock<IConfiguration> _mockConfigurationRoot = null;

        private void SetServiceProvider(IConfiguration configuration = null)
        {
            if(configuration == null)
            {
                // Logging the appsettings.json file
                var builder = new ConfigurationBuilder()
                                .SetBasePath(Directory.GetCurrentDirectory())
                                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

                configuration = builder.Build();
            }

            _serviceProvider = new ServiceCollection()
                        .AddLogging()
                        .AddSingleton<IConfiguration>(configuration)
                        .AddSingleton<ICalculatorService, CalculatorService>()
                        .BuildServiceProvider();

            _calculatorService = _serviceProvider.GetService<ICalculatorService>();
        }

        private void MockConfigurationsValues()
        {
            _mockConfigurationRoot = new Mock<IConfiguration>();

            //Overriding the two number max constraint and setting it to true and reseting the service provider
            var configurationSectionAllowMaxConstraint = new Mock<IConfigurationSection>();
            configurationSectionAllowMaxConstraint.Setup(a => a.Value).Returns("true");
            _mockConfigurationRoot.Setup(a => a.GetSection(It.Is<string>(s => s == "AppSettings:AllowTwoNumbersMaxConstraint"))).Returns(configurationSectionAllowMaxConstraint.Object);

            var configurationSectionMaximumValidNumbersAllowed = new Mock<IConfigurationSection>();
            configurationSectionMaximumValidNumbersAllowed.Setup(a => a.Value).Returns("2");
            _mockConfigurationRoot.Setup(a => a.GetSection(It.Is<string>(s => s == "AppSettings:MaximumValidNumbersAllowed"))).Returns(configurationSectionMaximumValidNumbersAllowed.Object);

            var configurationSectionInputDelimeters = new Mock<IConfigurationSection>();
            configurationSectionInputDelimeters.Setup(a => a.Value).Returns(",\n");
            _mockConfigurationRoot.Setup(a => a.GetSection(It.Is<string>(s => s == "AppSettings:InputDelimeters"))).Returns(configurationSectionInputDelimeters.Object);


            var configurationSectionInvalidEntry = new Mock<IConfigurationSection>();
            configurationSectionInvalidEntry.Setup(a => a.Value).Returns("0");
            _mockConfigurationRoot.Setup(a => a.GetSection(It.Is<string>(s => s == "AppSettings:InvalidNumberEntryDefaultValue"))).Returns(configurationSectionInvalidEntry.Object);

        }
        
        [TestCase("100,200", 2, 300)]
        [TestCase("100,300", 2, 400)]
        [TestCase("100, ", 2, 100)]
        [TestCase("100,", 2, 100)]
        [TestCase("100 ", 1, 100)]
        [TestCase("100", 1, 100)]
        [TestCase("1", 1, 1)]
        [TestCase("1, 4", 2, 5)]
        [TestCase("100, 4", 2, 104)]
        [TestCase("100 , 4", 2, 104)]
        [TestCase(" 100 , 4 ", 2, 104)]
        [TestCase("", 1, 0)]
        [TestCase(" ", 1, 0)]
        [TestCase(",", 2, 0)]
        [TestCase(" ,", 2, 0)]
        [TestCase(" , ", 2, 0)]
        [TestCase("fdfddf", 1, 0)]
        [TestCase("fdfddf,fdfddf", 2, 0)]
        [TestCase(", fdfddf", 2, 0)]
        [TestCase(" , fdfddf", 2, 0)]
        [TestCase(null, 1, 0)]
        public void AddNumbers_TwoNumbersMaxLimit_PositiveTests(string input, int length, int expectedResult)
        {
            MockConfigurationsValues();
            SetServiceProvider(_mockConfigurationRoot.Object);

            int[] numberEntries = _calculatorService.ParseInput(input);

            Assert.IsTrue(numberEntries.Length == length);

            var total = _calculatorService.AddNumbers(numberEntries);

            Assert.IsTrue(total == expectedResult);
        }


        [TestCase("1000,1111,11111")]
        [TestCase("1000, ,11111")]
        [TestCase("1000,,11111")]
        [TestCase("1000,1111,fdfdfd")]
        public void AddNumbers_TwoNumbersMaxLimit_ExceptionTests(string input)
        {
            MockConfigurationsValues();
            SetServiceProvider(_mockConfigurationRoot.Object);

            int[] numberEntries = _calculatorService.ParseInput(input);

            Assert.Throws<ArgumentException>(() => _calculatorService.AddNumbers(numberEntries));
        }

        [TestCase("1,2,3", 6)]
        [TestCase("1, ,2", 3)]
        [TestCase("1,,2,3,4, ,6,7,jlj", 23)]
        [TestCase("1,2,3,4,5,6,7,8,9,10,11,12", 78)]
        public void AddNumbers_IgnoreTwoNumbersMaxLimit_PositiveTests(string input, int expectedResult)
        {
            SetServiceProvider();
            int[] numberEntries = _calculatorService.ParseInput(input);

            var total = _calculatorService.AddNumbers(numberEntries);

            Assert.AreEqual(total, expectedResult);
        }

        [TestCase("\n", 0)]
        [TestCase(" \n ", 0)]
        [TestCase(",\n", 0)]
        [TestCase("1\n2", 3)]
        [TestCase("1\n2,3", 6)]
        [TestCase("1,\n,2", 3)]
        [TestCase("1\n2\n3", 6)]
        [TestCase("1\n,,2,3,hkhkh\n2,, ,6\n,7,jlj", 21)]
        public void AddNumbers_IncludingNewLineDelimiter_PositiveTests(string input, int expectedResult)
        {
            SetServiceProvider();

            int[] numberEntries = _calculatorService.ParseInput(input);

            var total = _calculatorService.AddNumbers(numberEntries);

            Assert.AreEqual(total, expectedResult);
        }

        [TestCase("-1", "-1")]
        [TestCase("-1\n", "-1")]
        [TestCase("-1\n,", "-1")]
        [TestCase("-1\n-2", "-1,-2")]
        [TestCase("-1\n,-2", "-1,-2")]
        [TestCase("1\n2,-3", "-3")]
        [TestCase("1,-2,-3,4,-5", "-2,-3,-5")]
        [TestCase("1\n-2\n3,-10,-1,-2", "-2,-10,-1,-2")]
        public void AddNumber_NegativeNumberConstraintViolation_ExceptionTests(string input, string expectedResult)
        {
            SetServiceProvider();

            try
            {
                int[] numberEntries = _calculatorService.ParseInput(input);
                _calculatorService.AddNumbers(numberEntries);
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex is NegativeNumberException);
                Assert.IsTrue(ex.Message.EndsWith(expectedResult));
            }
        }
    }
}