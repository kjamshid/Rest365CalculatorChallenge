using Calculator.Core.Interfaces;
using Calculator.Service;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.IO;

namespace Calculator.ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
            {
            //setup our DI
            var serviceProvider = new ServiceCollection()
                .AddLogging()
                .AddSingleton<ICalculatorService, CalculatorService>()
                .BuildServiceProvider();

            var builder = new ConfigurationBuilder()
                            .SetBasePath(Directory.GetCurrentDirectory())
                            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            IConfigurationRoot configuration = builder.Build();

            string confgLogLevel = configuration.GetSection("Logging").GetSection("LogLevel").Value;

            LogLevel defaultLogLevel;

            if(!Enum.TryParse(confgLogLevel, true, out defaultLogLevel))
            {
                defaultLogLevel = LogLevel.Debug;
            }

            //configure console logging
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                    .AddFilter("LoggingConsoleApp.Program", defaultLogLevel)
                    .AddConsole();
            });
            
            var logger = loggerFactory.CreateLogger<Program>();

            logger.LogDebug("Starting application");

            Console.WriteLine("Please enter two numbers to be added (comma separated)");
            string userInput = Console.ReadLine();

            ICalculatorService calculatorService = null;
            try
            {
                calculatorService = serviceProvider.GetService<ICalculatorService>();

                int[] numbers = calculatorService.processInput(userInput);

                Console.WriteLine($"The addition of {string.Join(",", numbers)} is {calculatorService.AddNumbers(numbers)}");
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Invalid input {ex.Message}");
            }

        }
    }
}
