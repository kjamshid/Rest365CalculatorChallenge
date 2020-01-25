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
        private static IConfigurationRoot configuration = null;
        private static IServiceProvider serviceProvider = null;
        static void Main(string[] args)
        {
            ICalculatorService calculatorService = null;
            StartupApp();

            var loggerFactory = ConfigureLogging(configuration);
            var logger = loggerFactory.CreateLogger<Program>();

            logger.LogDebug("Starting application");

            // Prompting user to enter two numbers
            Console.WriteLine("Please enter two numbers to be added (comma separated)");
            string userInput = Console.ReadLine();

            try
            {
                // Calling calcultor serivce to get the sum
                calculatorService = serviceProvider.GetService<ICalculatorService>();

                var numbers = calculatorService.ParseValidNumbersFromInput(userInput);

                Console.WriteLine($"The addition of the following entries {string.Join(",", numbers)} is {calculatorService.AddNumbers(numbers)}");

                Console.ReadLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Invalid input {ex.Message}");
            }

        }

        /// <summary>
        /// Initial setup of the application which includes loading the app settings config file and registering the dependency injections with appropriate life cylcle
        /// </summary>
        private static void StartupApp()
        {
            configuration = LoadAppSettingsConfig();

            //setup our Dependency Injection for internal logging, and calculator service
            serviceProvider = new ServiceCollection()
                .AddLogging()
                .AddSingleton<IConfiguration>(configuration)
                .AddSingleton<ICalculatorService, CalculatorService>()
                .BuildServiceProvider();
        }

        private static IConfigurationRoot LoadAppSettingsConfig()
        {
            // Logging the appsettings.json file
            var builder = new ConfigurationBuilder()
                            .SetBasePath(Directory.GetCurrentDirectory())
                            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            IConfigurationRoot configuration = builder.Build();

            return configuration;
        }

        /// <summary>
        /// configuring logging with the relative log level (default = debug mode)
        /// </summary>
        /// <param name="configuration"></param>
        /// <returns></returns>
        private static ILoggerFactory ConfigureLogging(IConfiguration configuration)
        {
            // Setting the default log level from config
            string confgLogLevel = configuration.GetSection("Logging").GetSection("LogLevel").Value;

            LogLevel defaultLogLevel;
            
            if (!Enum.TryParse(confgLogLevel, true, out defaultLogLevel))
            {
                defaultLogLevel = LogLevel.Debug;
            }

            //configure console logging
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                    .AddFilter("Calculator.ConsoleApp.Program", defaultLogLevel)
                    .AddConsole();
            });

            return loggerFactory;
        }

    }
}
