using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using RPGBot.Commands;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace RPGBot {

    public class Bot {
        public static DiscordChannel ImageCache { get; private set; }
        public static DiscordClient Client { get; private set; }
        public static CommandsNextExtension Commands { get; private set; }
        public static InteractivityExtension Interactivty { get; private set; }

        public static ConcurrentDictionary<ulong, DiscordChannel> ConfiguredChannels { get; set; }

        public static ConcurrentDictionary<ulong, string> Prefixes;

        private Options Options { get; set; }

        public Bot() {
            //TODO: Serialize and Deserialize this!
            Prefixes = new ConcurrentDictionary<ulong, string>();
            ConfiguredChannels = new ConcurrentDictionary<ulong, DiscordChannel>();
        }

        public async Task RunAsync(Options options) {
            Client = new DiscordClient(new DiscordConfiguration {
                Token = options.Token,
                TokenType = TokenType.Bot,

                AutoReconnect = true,
                ReconnectIndefinitely = true,
                LogLevel = LogLevel.Info,
                UseInternalLogHandler = true,
            });
            Options = options;

            #region EVENTS

            Client.Ready += OnClientReady;
            Client.ClientErrored += OnClientErrored;
            Client.GuildAvailable += OnGuildAvailable;
            Client.GuildUnavailable += OnGuildUnavailable;

            #endregion EVENTS

            #region Commands

            Commands = Client.UseCommandsNext(new CommandsNextConfiguration {
                EnableDms = true,
                EnableMentionPrefix = true,
                CaseSensitive = false,
                IgnoreExtraArguments = false,
                UseDefaultCommandHandler = false,
            });

            Commands.RegisterCommands<ModeratorCommands>();
            Commands.RegisterCommands<RantCommands>();
            Commands.RegisterCommands<RPGCommands>();
            Commands.RegisterCommands<InfoCommands>();

            Interactivty = Client.UseInteractivity(new InteractivityConfiguration {
                PaginationBehaviour = DSharpPlus.Interactivity.Enums.PaginationBehaviour.Default,
                PollBehaviour = DSharpPlus.Interactivity.Enums.PollBehaviour.Default,
                PaginationDeletion = DSharpPlus.Interactivity.Enums.PaginationDeletion.Default,
            });
            Client.MessageCreated += OnMessageCreated;

            #endregion Commands

            await Client.ConnectAsync();
            await Task.Delay(-1);
        }

        private async Task OnMessageCreated(MessageCreateEventArgs e) {
            if (e.Author.IsBot) { return; }
            var prefix = GetPrefix(e.Guild);
            if (e.Message.Content.StartsWith(prefix)) {
                var cmdText = e.Message.Content.Substring(prefix.Length);
                var command = Commands.FindCommand(cmdText, out var rawArgs);
                if (command != null) {
                    try {
                        var ctx = Commands.CreateContext(e.Message, prefix, command, rawArgs);
                        await Commands.ExecuteCommandAsync(ctx);
                    } catch (System.Exception ex) {
                        await e.Channel.SendMessageAsync(ex.ToString());
                    }
                }
            }
        }

        public static string GetPrefix(DiscordGuild guild) {
            if (Prefixes != null) {
                if (Prefixes.TryGetValue(guild.Id, out var prefix)) {
                    return prefix;
                }
            }
            return "!!";
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
            if (e.Guild.Id == Options.Guild) {
                Log(LogLevel.Info, $"Setting up ImageCache");
                var channels = await e.Guild.GetChannelsAsync();
                foreach (var channel in channels) {
                    if (channel.Id == Options.CacheChannel) {
                        ImageCache = channel;
                        break;
                    }
                }
            }
            Log(LogLevel.Info, $"{e.Guild.Name} is now Available");
            await Task.Delay(1);
        }

        #endregion Event Callbacks

        private void Log(LogLevel level, string msg) {
            Client.DebugLogger.LogMessage(level, "RPG-Bot", msg, DateTime.Now);
        }
    }
}