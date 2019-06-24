using Newtonsoft.Json;
using System;
using System.IO;

namespace RPGBot {
    class Program {
        public static Bot DiscordBot { get; private set; }

        static void Main(string[] args) {
            if (!File.Exists("config.json")) {
                Console.WriteLine("Couldn't find config.json. Please create it");
                return;
            }

            var json = File.ReadAllText("config.json");
            var settings = JsonConvert.DeserializeObject<Options>(json);
            DiscordBot = new Bot();
            DiscordBot.RunAsync(settings.Token).GetAwaiter().GetResult();
        }
    }
}
