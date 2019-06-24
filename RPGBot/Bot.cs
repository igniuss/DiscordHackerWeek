using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using System;
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
            Client.GuildAvailable += OnGuildAvailable;
            Client.GuildUnavailable += OnGuildUnavailable;
            #endregion

            await Client.ConnectAsync();
            await Task.Delay(-1);
        }

        #region Event Callbacks

        private async Task OnGuildUnavailable(GuildDeleteEventArgs e) {
            Log(LogLevel.Warning, $"{e.Guild.Name} is now Unavailable");
            await Task.Delay(1);
            /*
            There's a couple permissions we need, specifically

            AddReactions
            ReadMessageHistory
            SendMessages

            TODO: If we need more, ADD THEM HERE
            */
            var permissions = e.Guild.Permissions.HasValue ? e.Guild.Permissions.Value : 0;
            if (!permissions.HasPermission(Permissions.AddReactions) 
                || !permissions.HasPermission(Permissions.ReadMessageHistory) 
                || !permissions.HasPermission(Permissions.SendMessages)) {
                //if any of these fail. We need to report it somehow
                Log(LogLevel.Warning, $"Missing permissions in {e.Guild.Name}.");
                //TODO: Make this better I guess 👏
                var embed = new DiscordEmbedBuilder()
                    .WithTitle("Error")
                    .WithDescription($@"Hi there {e.Guild.Owner.Nickname}! 👋, seems like I'm missing some permissions to work properly!
To operate properly, I need to be able to Send Messages, Add Reactions, and Read Message History, be sure to invite me again with these settings. 
Thanks!
")
                    .WithAuthor("RPG-Bot")
                    .WithColor(DiscordColor.DarkRed);

                try {
                _ = await e.Guild.Owner.SendMessageAsync(embed: embed);
                } catch {
                    Log(LogLevel.Critical, $"Tried to contact {e.Guild.Owner.Mention} in {e.Guild.Name}");
                }
                await e.Guild.LeaveAsync();
            } else {
                //TODO: Send instructions to the owner.
            }

        }

        private async Task OnClientErrored(ClientErrorEventArgs e) {
            Log(LogLevel.Error, e.Exception.ToString());
            await Task.Delay(1);
        }

        private async Task OnClientReady(ReadyEventArgs e) {
            Log(LogLevel.Info, "Client Ready");
            await Task.Delay(1);
        }

        private async Task OnGuildAvailable(GuildCreateEventArgs e) {
            Log(LogLevel.Info, $"{e.Guild.Name} is now Available");
            await Task.Delay(1);
        }

        #endregion

        private void Log(LogLevel level, string msg) {
            Client.DebugLogger.LogMessage(level, "RPG-Bot", msg, DateTime.Now);
        }
    }
}

