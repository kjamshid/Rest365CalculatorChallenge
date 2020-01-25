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
    public class Tests
    {
        ICalculatorService calculatorService = null;
        IServiceProvider serviceProvider = null;
        Mock<IConfiguration> mockConfigurationRoot = null;

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

            serviceProvider = new ServiceCollection()
                        .AddLogging()
                        .AddSingleton<IConfiguration>(configuration)
                        .AddSingleton<ICalculatorService, CalculatorService>()
                        .BuildServiceProvider();

            calculatorService = serviceProvider.GetService<ICalculatorService>();
        }

        private void MockConfigurationsValues()
        {
            mockConfigurationRoot = new Mock<IConfiguration>();

            //Overriding the two number max constraint and setting it to true and reseting the service provider
            var configurationSectionAllowMaxConstraint = new Mock<IConfigurationSection>();
            configurationSectionAllowMaxConstraint.Setup(a => a.Value).Returns("true");
            mockConfigurationRoot.Setup(a => a.GetSection(It.Is<string>(s => s == "AppSettings:AllowTwoNumbersMaxConstraint"))).Returns(configurationSectionAllowMaxConstraint.Object);

            var configurationSectionMaximumValidNumbersAllowed = new Mock<IConfigurationSection>();
            configurationSectionMaximumValidNumbersAllowed.Setup(a => a.Value).Returns("2");
            mockConfigurationRoot.Setup(a => a.GetSection(It.Is<string>(s => s == "AppSettings:MaximumValidNumbersAllowed"))).Returns(configurationSectionMaximumValidNumbersAllowed.Object);

            var configurationSectionInputDelimeters = new Mock<IConfigurationSection>();
            configurationSectionInputDelimeters.Setup(a => a.Value).Returns(",\n");
            mockConfigurationRoot.Setup(a => a.GetSection(It.Is<string>(s => s == "AppSettings:InputDelimeters"))).Returns(configurationSectionInputDelimeters.Object);


            var configurationSectionInvalidEntry = new Mock<IConfigurationSection>();
            configurationSectionInvalidEntry.Setup(a => a.Value).Returns("0");
            mockConfigurationRoot.Setup(a => a.GetSection(It.Is<string>(s => s == "AppSettings:InvalidNumberEntryDefaultValue"))).Returns(configurationSectionInvalidEntry.Object);

        }
        [SetUp]
        public void Setup()
        {
            
            //mockConfigurationRoot.SetupGet(x => x["AppSettings:InputDelimeters"]).Returns(",");
            //mockConfigurationRoot.SetupGet(x => x["AppSettings:MaximumValidNumbersAllowed"]).Returns("2");
            //mockConfigurationRoot.SetupGet(x => x["AppSettings:InvalidNumberEntryDefaultValue"]).Returns("0");
            //mockConfigurationRoot.SetupGet(x => x["AppSettings:AllowTwoNumbersMaxConstraint"]).Returns("false");

            //  mockConfigurationRoot.SetupGet(x => x[It.Is<string>(s => s == "AppSettings:AllowTwoNumbersMaxConstraint")]).Returns("false");

            //   mockConfSection.SetupGet(m => m[It.Is<string>(s => s == "default")]).Returns("mock value");

            //mockConfigurationRoot.SetupGet(x => x[It.Is<string>(s => s == "AppSettings:InputDelimeters")]).Returns(",");
            //mockConfigurationRoot.SetupGet(x => x[It.Is<string>(s => s == "AppSettings:MaximumValidNumbersAllowed")]).Returns("2");
            //mockConfigurationRoot.SetupGet(x => x[It.Is<string>(s => s == "AppSettings:InvalidNumberEntryDefaultValue")]).Returns("0");

            //mockConfigurationRoot.SetupGet(x => x[It.Is<string>(s => s == "AppSettings:AllowTwoNumbersMaxConstraint")]).Returns("false");
            // mockConfigurationRoot.SetupGet(m => m[It.Is<string>(s => s == "AppSettings:AllowTwoNumbersMaxConstraint")]).Returns(configurationSection.Object);

            // mockConfigurationRoot.Setup(a => a.GetSection("AppSettings:AllowTwoNumbersMaxConstraint")).Returns(configurationSection.Object);

            //        mockConfigurationRoot.Setup(c => c.GetValue<string>("AppSettings:InputDelimeters"))
            //.Returns(",");
            //  .Verifiable();
        }

        [TestCase("100,200", 2, 300)]
        [TestCase("100,300", 2, 400)]
        [TestCase("100, ", 2, 100)]
        [TestCase("100,", 2, 100)]
        [TestCase("100 ", 1, 100)]
        [TestCase("100", 1, 100)]
        [TestCase("-1", 1, -1)]
        [TestCase("-1,-4", 2, -5)]
        [TestCase("100,-4", 2, 96)]
        [TestCase("100 , -4", 2, 96)]
        [TestCase(" 100 , -4 ", 2, 96)]
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
        public void AddNumbers_TwoNumbersMaxLimit_PositiveTests(string input, int length, int result)
        {
            MockConfigurationsValues();
            SetServiceProvider(mockConfigurationRoot.Object);

            int[] numberEntries = calculatorService.ParseInput(input);

            Assert.IsTrue(numberEntries.Length == length);

            var total = calculatorService.AddNumbers(numberEntries);

            Assert.IsTrue(total == result);
        }


        [TestCase("1000,1111,11111")]
        [TestCase("1000, ,11111")]
        [TestCase("1000,,11111")]
        [TestCase("1000,1111,fdfdfd")]
        public void AddNumbers_TwoNumbersMaxLimit_ExceptionTests(string input)
        {
            MockConfigurationsValues();
            SetServiceProvider(mockConfigurationRoot.Object);

            int[] numberEntries = calculatorService.ParseInput(input);

            Assert.Throws<ArgumentException>(() => calculatorService.AddNumbers(numberEntries));
        }

        [TestCase("1,2,3", 6)]
        [TestCase("1, ,2", 3)]
        [TestCase("1,,2,3,4, ,6,7,jlj", 23)]
        [TestCase("1,2,3,4,5,6,7,8,9,10,11,12", 78)]
        public void AddNumbers_IgnoreTwoNumbersMaxLimit_PositiveTests(string input, int result)
        {
            SetServiceProvider();
            int[] numberEntries = calculatorService.ParseInput(input);

            var total = calculatorService.AddNumbers(numberEntries);

            Assert.AreEqual(total, result);
        }

        [TestCase("\n", 0)]
        [TestCase(" \n ", 0)]
        [TestCase(",\n", 0)]
        [TestCase("1\n2", 3)]
        [TestCase("1\n2,3", 6)]
        [TestCase("1,\n,2", 3)]
        [TestCase("1\n2\n3", 6)]
        [TestCase("1\n,,2,3,hkhkh\n2,, ,6\n,7,jlj", 21)]
        public void AddNumbers_IncludingNewLineDelimiter_PositiveTests(string input, int result)
        {
            SetServiceProvider();

            int[] numberEntries = calculatorService.ParseInput(input);

            var total = calculatorService.AddNumbers(numberEntries);

            Assert.AreEqual(total, result);
        }
    }
}