using Calculator.Core.Interfaces;
using Calculator.Service;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Calculator.ConsoleApp
{
    class Program
    {
        // Global variable declaration
        private static bool _Cancelled = false;
        private static IConfigurationRoot _configuration = null;
        private static IServiceProvider _serviceProvider = null;
        private static ILogger<Program> _logger = null;

        static void Main(string[] args)
        {
            ICalculatorService calculatorService = null;
        
            // Starting the application with by loading config file and registering logging and services
            StartupApp();

            var loggerFactory = ConfigureLogging(_configuration);
            _logger = loggerFactory.CreateLogger<Program>();

            _logger.LogInformation("Starting application");

            // capturing ctrl + c (user entry) and exiting application
            Console.CancelKeyPress += new ConsoleCancelEventHandler(Console_CancelKeyPress);

            try
            {
                // Calling calcultor serivce to get the sum
                calculatorService = _serviceProvider.GetService<ICalculatorService>();
                string userInput = string.Empty;
                List<int> numbers = new List<int>();
                while (true)
                {
                    // Prompting user to enter two numbers to add and reading the input
                    Console.WriteLine("***********************************************************************");
                    Console.WriteLine("Please enter two numbers to add separated by comma or newline (, or \\n)");
                    userInput = Console.ReadLine();

                    if (_Cancelled || userInput == null) break; // if ctrl + c entered, breaking from loop

                    // parsing the input and grabbing the valid entries based on requirements
                    numbers = calculatorService.ParseValidNumbersFromInput(userInput);

                    Console.WriteLine();
                    // displaying to users the result
                    Console.WriteLine($"The addition of the following entries {string.Join("+", numbers)} is {calculatorService.AddNumbers(numbers)}");
                    Console.WriteLine();
                }
               
                // Removing the cancel key
                Console.CancelKeyPress -= new ConsoleCancelEventHandler(Console_CancelKeyPress);
                _logger.LogInformation("Exiting application");
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
            _configuration = LoadAppSettingsConfig();

            //setup our Dependency Injection for internal logging, and calculator service
            _serviceProvider = new ServiceCollection()
                .AddLogging()
                .AddSingleton<IConfiguration>(_configuration)
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
                    .AddFilter("ConsoleApp.Program", defaultLogLevel)
                    .AddConsole();
            });

            return loggerFactory;
        }

        /// <summary>
        /// Capturing ctrl + c if entered by user
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            if (e.SpecialKey == ConsoleSpecialKey.ControlC)
            {
                Console.WriteLine("Cancelling");
                _Cancelled = true;
                e.Cancel = true;
            }
        }

    }
}

