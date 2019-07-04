using Newtonsoft.Json;
using NLog;
using System;
using System.IO;

namespace RPGBot {

    internal class Program {
        public static Bot DiscordBot { get; private set; }

        private static void Main(string[] args) {
            if (!File.Exists("config.json")) {
                Console.WriteLine("Couldn't find config.json. Please create it");
                return;
            }

            NLog.Config.ConfigurationItemFactory.Default.Targets.RegisterDefinition("DiscordChannel", typeof(DiscordChannelTarget));

            var config = new NLog.Config.LoggingConfiguration();
            var logFile = new NLog.Targets.FileTarget("logfile") { FileName = "output.log" };
            var logConsole = new NLog.Targets.ConsoleTarget("logconsole");
            // Rules for mapping loggers to targets            
            config.AddRule(LogLevel.Info, LogLevel.Fatal, logConsole);
            config.AddRule(LogLevel.Debug, LogLevel.Fatal, logFile);

            // Apply config           
            LogManager.Configuration = config;

            var json = File.ReadAllText("config.json");
            var options = JsonConvert.DeserializeObject<Options>(json);
            DiscordBot = new Bot();
            DiscordBot.RunAsync(options).GetAwaiter().GetResult();
        }
    }
}