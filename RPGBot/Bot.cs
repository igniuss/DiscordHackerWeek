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
        public Timer PeriodicEvent { get; private set; }

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
            Commands.RegisterCommands<RantCommands>();
            Commands.RegisterCommands<RPGCommands>();
            Commands.RegisterCommands<InfoCommands>();
            Commands.RegisterCommands<HelpCommands>();

            Interactivty = Client.UseInteractivity(new InteractivityConfiguration {
                PaginationBehaviour = DSharpPlus.Interactivity.Enums.PaginationBehaviour.Default,
                PollBehaviour = DSharpPlus.Interactivity.Enums.PollBehaviour.Default,
                PaginationDeletion = DSharpPlus.Interactivity.Enums.PaginationDeletion.Default,
            });
            Client.MessageCreated += OnMessageCreated;

            #endregion Commands

            await Client.ConnectAsync();

            PeriodicEvent = new Timer(TimeSpan.FromHours(1f).TotalMilliseconds);
            PeriodicEvent.Elapsed += OnUpdate;
            PeriodicEvent.Start();
            LastEvent = DateTime.Now;
            await Task.Delay(-1);
        }

        private async Task OnSocketClosed(SocketCloseEventArgs e) {
            await e.Client.ReconnectAsync(true);
        }

        private async Task OnHeartbeat(HeartbeatEventArgs e) {
            //calculate time left till new mission
            var span = TimeSpan.FromMilliseconds(PeriodicEvent.Interval);
            var timeLeft = LastEvent + span - DateTime.Now;

            //get all the other data in here boys
            var serverCount = Bot.Client.Guilds.Count;
            var memberCount = Bot.Client.Guilds.Sum(x => x.Value.MemberCount);
            var activity = new DiscordActivity($"{Math.Floor(timeLeft.TotalMinutes)}:{timeLeft.Seconds.ToString("##")} until event.\nIn {serverCount} servers with {memberCount} members.", ActivityType.Streaming);
            await Bot.Client.UpdateStatusAsync(activity, UserStatus.Online);
        }

        private DateTime LastEvent { get; set; }

        private void OnUpdate(object sender, ElapsedEventArgs e) {
            Log(LogLevel.Info, "Starting Event");
            foreach (var option in GuildOptions) {
                var quest = new QuestEvent(option.GetChannel());

                //We don't wanna call this async. Start them all at once
                quest.StartQuest();
            }
            LastEvent = DateTime.Now;
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
            
            if(!GuildOptions.Any(x=>x.Id == e.Guild.Id)) {
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