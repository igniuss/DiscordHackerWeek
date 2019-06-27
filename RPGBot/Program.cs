using Newtonsoft.Json;
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

            var json = File.ReadAllText("config.json");
            var options = JsonConvert.DeserializeObject<Options>(json);
            DiscordBot = new Bot();
            DiscordBot.RunAsync(options).GetAwaiter().GetResult();
        }
    }
}