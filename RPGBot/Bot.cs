using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using RPGBot.Commands;
using RPGBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;

namespace RPGBot {

    public class Bot {
        public static DiscordChannel ImageCache { get; private set; }
        public static DiscordClient Client { get; private set; }
        public Options Options { get; private set; }
        public static CommandsNextExtension Commands { get; private set; }
        public static InteractivityExtension Interactivty { get; private set; }
        //public Timer PeriodicEvent { get; private set; }

        public static List<GuildOption> GuildOptions;
        public static readonly ulong[] BotOwnerIds = new ulong[] { 109706676650663936, 330452192391593987 };
        public Bot() {
            GuildOptions = LoadGuildOptions();
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
            Client.Heartbeated += OnHeartbeat;
            Client.SocketClosed += OnSocketClosed;
            Client.GuildCreated += OnGuildCreated;

            #endregion EVENTS

            #region Commands

            Commands = Client.UseCommandsNext(new CommandsNextConfiguration {
                EnableDms = true,
                EnableMentionPrefix = true,
                CaseSensitive = false,
                IgnoreExtraArguments = false,
                UseDefaultCommandHandler = false,
                EnableDefaultHelp = false,
            });

            Commands.RegisterCommands<ModeratorCommands>();
            Commands.RegisterCommands<RPGCommands>();
            Commands.RegisterCommands<HelpCommands>();

            Interactivty = Client.UseInteractivity(new InteractivityConfiguration {
                PaginationBehaviour = DSharpPlus.Interactivity.Enums.PaginationBehaviour.Default,
                PollBehaviour = DSharpPlus.Interactivity.Enums.PollBehaviour.Default,
                PaginationDeletion = DSharpPlus.Interactivity.Enums.PaginationDeletion.Default,
            });
            Client.MessageCreated += OnMessageCreated;

            #endregion Commands

            await Client.ConnectAsync();

            while (true) {
                var now = DateTime.Now;
                var minutes = now.Minute;
                var left = 60 - minutes;
                await Task.Delay(TimeSpan.FromMinutes(left));
                OnUpdate(null, null);
            }
        }

        private async Task OnGuildCreated(GuildCreateEventArgs e) {
            var channel = e.Guild.GetDefaultChannel();
            await channel.SendMessageAsync("Thank you for inviting RPG Bot! Here are some tips to get you started. My default prefix is ``!!`` and can be changed by any admin of the server by using the command ``!!prefix newPrefix``. Adventures begin at the start of every hour. Currently, the adventures will show up in this channel. Admins can change the channel by using the command ``!!setchannel #otherchannel`` or by saying ``!!setchannel`` in the channel they wish to use. If you need any help, have suggestions or bugs to report, or just want to chat, you can join our support server here -> https://discord.gg/VMBn2yV");
        }

        private async Task OnSocketClosed(SocketCloseEventArgs e) {
            await e.Client.ReconnectAsync(true);
        }

        private async Task OnHeartbeat(HeartbeatEventArgs e) {
            //calculate time left till new mission
            var timeLeft = TimeSpan.FromMinutes(60 - DateTime.Now.Minute);

            //get all the other data in here boys
            var serverCount = Bot.Client.Guilds.Count;
            var memberCount = Bot.Client.Guilds.Sum(x => x.Value.MemberCount);
            var activity = new DiscordActivity($"⚔ {Math.Floor(timeLeft.TotalMinutes)} minutes until next event.    [{serverCount} servers with {memberCount} members]", ActivityType.Streaming);
            await Bot.Client.UpdateStatusAsync(activity, UserStatus.Online);
        }

        private void OnUpdate(object sender, ElapsedEventArgs e) {
            Log(LogLevel.Info, "Starting Event");
            foreach (var option in GuildOptions) {
                var channel = option.GetChannel();
                if (channel == null) { continue; }
                var quest = new QuestEvent(channel);

                //We don't wanna call this async. Start them all at once
                quest.StartQuest();
            }
        }

        private async Task OnMessageCreated(MessageCreateEventArgs e) {
            if (e.Author.IsBot) { return; }
            await Task.Delay(10);
            var prefix = GetPrefix(e.Guild);
            if (e.Message.Content.StartsWith(prefix)) {
                var cmdText = e.Message.Content.Substring(prefix.Length);
                var command = Commands.FindCommand(cmdText, out var rawArgs);
                if (command != null) {
                    try {
                        var ctx = Commands.CreateContext(e.Message, prefix, command, rawArgs);
                        Commands.ExecuteCommandAsync(ctx);
                    } catch (System.Exception ex) {
                        Log(LogLevel.Error, ex.ToString());
                    }
                }
            }
        }

        public static string GetPrefix(DiscordGuild guild) {
            if (GuildOptions != null) {
                var prefix = GuildOptions.Where(x => x.Id == guild.Id).FirstOrDefault()?.Prefix;
                if (!string.IsNullOrEmpty(prefix)) {
                    return prefix;
                }
            }
            return "!!";
        }

        private List<GuildOption> LoadGuildOptions() {
            var options = DB.GetAll<GuildOption>(GuildOption.DBName, GuildOption.TableName).ToList();
            if (options == null) {
                options = new List<GuildOption>();
                DB.Insert(GuildOption.DBName, GuildOption.TableName, options);
            }
            return options;
        }

        #region Event Callbacks

        private async Task OnGuildUnavailable(GuildDeleteEventArgs e) {
            Log(LogLevel.Warning, $"{e.Guild.Name} is now Unavailable");
            await Task.Delay(1);
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

            if (!GuildOptions.Any(x => x.Id == e.Guild.Id)) {
                var options = new GuildOption {
                    Id = e.Guild.Id,
                    Channel = e.Guild.GetDefaultChannel().Id
                };

                DB.Insert(GuildOption.DBName, GuildOption.TableName, options);
                LoadGuildOptions();
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