using DSharpPlus;
using DSharpPlus.EventArgs;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace RPGBot {
    public class Bot {
        
        public DiscordClient Client { get; set; }
        public async Task RunAsync(string token) {
            Client = new DiscordClient(new DiscordConfiguration {
                AutoReconnect = true,
                Token = token,
                TokenType = TokenType.Bot,
                ReconnectIndefinitely = true,
            });

            #region EVENTS
            Client.Ready += OnClientReady;
            Client.ClientErrored += OnClientErrored;
            #endregion

            await Client.ConnectAsync();
            await Task.Delay(-1);
        }

        private async Task OnClientErrored(ClientErrorEventArgs e) {
            Log(LogLevel.Error, e.Exception.ToString());
            await Task.Delay(1);
        }

        private async Task OnClientReady(ReadyEventArgs e) {
            Log(LogLevel.Info, "Client Ready");
            await Task.Delay(1);
        }

        private void Log(LogLevel level, string msg) {
            Client.DebugLogger.LogMessage(level, "RPG-Bot", msg, DateTime.Now);
        }
    }
}
