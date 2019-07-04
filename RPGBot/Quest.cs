using DSharpPlus.Entities;
using RPGBot.Actions;
using RPGBot.Characters;
using RPGBot.Generative;
using RPGBot.Helpers;
using RPGBot.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RPGBot {

    public class Quest {

        public struct QuestData {
            public string QuestName { get; set; }
            public string[] EnemyPaths { get; set; }
            public string[] EnemyNames { get; set; }
            public string BossPath { get; set; }
            public string BossName { get; set; }
        }

        #region Private Structs

        private struct EncounterData {

            #region Public Properties

            public IEnumerable<ulong> Ids { get; set; }
            public string Message { get; set; }

            #endregion Public Properties
        }

        #endregion Private Structs

        #region Public Fields

        public TimeSpan waitTime = TimeSpan.FromMinutes(1f);

        #endregion Public Fields

        #region Private Fields

        private static readonly ConcurrentDictionary<string, string> CachedImages = new ConcurrentDictionary<string, string>();
        private static readonly Random random = new Random();

        #endregion Private Fields

        #region Public Properties

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        public string BackgroundPath { get; private set; }

        public string BackgroundUrl { get; private set; }

        public DiscordChannel Channel { get; private set; }

        public TimeSpan CompletedTime {
            get {
                return Timer.Elapsed;
            }
        }

        public float CurrentHP { get; set; }
        public int EncounterCount { get; private set; }
        public int EncounterIndex { get; private set; }

        public string[] EnemyPaths { get; }
        public string[] EnemyNames { get; set; }
        public string BossPath { get; }
        public string BossName { get; set; }

        public float MaxHP { get; private set; }
        public string QuestName { get; private set; }
        public bool Success { get; private set; } = false;
        public ulong[] UserIds { get; private set; }

        #endregion Public Properties

        #region Private Properties

        private System.Diagnostics.Stopwatch Timer { get; set; }

        #endregion Private Properties

        #region Public Constructors

        public Quest(QuestData data, DiscordChannel channel) {
            Channel = channel;

            QuestName = data.QuestName;
            EncounterCount = data.EnemyPaths.Length;
            EnemyPaths = data.EnemyPaths;
            EnemyNames = data.EnemyNames;
            BossPath = data.BossPath;
            BossName = data.BossName;
        }

        #endregion Public Constructors

        #region Public Methods

        public async Task<Quest> Start() {
            try {
                Timer = System.Diagnostics.Stopwatch.StartNew();
                //Send 'Starting Quest'

                BackgroundPath = ImageGenerator.RandomBackground();

                #region Starting Quest

                var msg = await PostStartMessage();
                //Collect players playing.

                #endregion Starting Quest

                await Task.Delay(this.waitTime);

                #region CollectPlayers

                var userIds = new List<ulong>();
                var characters = CharacterBase.Characters;
                foreach (var character in characters) {
                    await Task.Delay(500);
                    var users = await msg.GetReactionsAsync(character.GetEmoji());
                    foreach (var user in users) {
                        if (user.IsBot) { continue; }
                        if (userIds.Contains(user.Id)) { continue; }

                        var player = Player.GetPlayer(Channel.GuildId, user.Id);
                        player.characterId = character.Id;
                        if (player.Update()) {
                            userIds.Add(user.Id);
                        }
                    }
                }

                //TODO: no-one joined.
                if (userIds.Count == 0) {
                    await msg.DeleteAsync();
                    Timer.Stop();
                    return null;
                }
                Logger.Info($"{userIds.Count} joined quest in {Channel.Guild.Name}");
                UserIds = userIds.ToArray();
                await msg.DeleteAsync();
                await Task.Delay(500);

                #endregion CollectPlayers

                var embed = GetQuestCommences();
                msg = await Channel.SendMessageAsync(embed: embed);
                await Task.Delay(500);

                await Task.Delay(this.waitTime);

                for (EncounterIndex = 0; EncounterIndex < EncounterCount; EncounterIndex++) {
                    //do an encounter
                    await msg.DeleteAsync();
                    await Task.Delay(500);

                    Logger.Debug($"Encounter: {EncounterIndex} - {EncounterCount} on {Channel.Guild.Name}");
                    var enemy = EnemyPaths[EncounterIndex];
                    var enemyName = EnemyNames[EncounterIndex];
                    //Fetch updated player stats
                    var players = Player.GetPlayers(Channel.GuildId, UserIds);
                    UpdateHP(players);

                    var enemyLevel = Player.CalculateLevel((ulong)players.Sum(x => (long)x.GetCurrentExp()));
                    enemyLevel = (int)Math.Round(enemyLevel * random.Range(0.75f, 3f));

                    var survivors = await Encounter(enemy, enemyLevel, enemyName);
                    if (survivors.Ids == null || survivors.Ids.Count() == 0) {
                        embed.WithDescription(survivors.Message);
                        await Channel.SendMessageAsync(embed: embed);
                        Timer.Stop();
                        return this;
                    }

                    UserIds = survivors.Ids.ToArray();
                    //wait between 30 and 120 seconds for next encounter.

                    var newEmbed = await RandomEvent(GetQuestCommences());
                    if (!string.IsNullOrEmpty(survivors.Message)) {
                        newEmbed.AddField("Status", survivors.Message);
                    }
                    msg = await Channel.SendMessageAsync(embed: newEmbed);
                    await Task.Delay(TimeSpan.FromSeconds(random.Range(20f, 60f)));
                }
                //BOSS FIGHT
                {
                    await msg.DeleteAsync();
                    Logger.Debug("Boss Fight");
                    await Task.Delay(500);
                    var enemy = BossPath;

                    //Fetch updated player stats
                    var players = Player.GetPlayers(Channel.GuildId, UserIds);
                    UpdateHP(players);

                    var enemyLevel = Player.CalculateLevel((ulong)players.Sum(x => (long)x.GetCurrentExp()));
                    enemyLevel = (int)Math.Round(enemyLevel * random.Range(1f, 5f));

                    //var enemyName = Generative.NamesGenerator.Instance.GetResult();
                    var enemyName = BossName;
                    var survivors = await Encounter(enemy, enemyLevel, enemyName);
                    if (survivors.Ids == null || survivors.Ids.Count() == 0) {
                        embed.WithDescription(survivors.Message);
                        await Channel.SendMessageAsync(embed: embed);
                        Timer.Stop();
                        return this;
                    }
                }

                Success = true;
                Timer.Stop();
            } catch (System.Exception ex) {
                Logger.Error(ex);
            }
            return this;
        }

        #endregion Public Methods

        #region Private Methods

        private float CalculateDamage(int enemyLevel) {
            return enemyLevel * 5f * random.Range(1f, 3f);
        }

        private long CalculateExp(int enemyLevel, float currentHP, float maxHP) {
            return (long)Math.Ceiling(enemyLevel * 20f * (CurrentHP == 0 ? 1f : 1f - (currentHP / maxHP)));
        }

        private ulong CalculateGold(int enemyLevel, float currentHP, float maxHP) {
            return (ulong)Math.Ceiling(enemyLevel * 50f * (CurrentHP == 0 ? 1f : 1f - (currentHP / maxHP)));
        }

        private async Task<EncounterData> Encounter(string enemyPath, int enemyLevel, string enemyName) {
            try {
                var path = ImageGenerator.CreateOrGetImage(enemyPath, BackgroundPath, CurrentHP / MaxHP);
                var url = await ImageGenerator.GetImageURL(path);
                var currentUserIds = UserIds.ToList();

                var maxHPEnemy = enemyLevel * 50 * random.Range(0.8f, 1.5f);
                var currentHPEnemy = maxHPEnemy;

                DiscordMessage msg = null;
                var actions = Actions.ActionBase.GetAllActions();
                var turnCount = 0;
                var additional = "";
                while (true) {
                    turnCount++;

                    var healthPercentage = CurrentHP / MaxHP;
                    var healthPercentageEnemy = currentHPEnemy / maxHPEnemy;

                    var embed = Bot.GetDefaultEmbed()
                        .WithImageUrl(url)
                        .AddField("Quest", QuestName)
                        .WithColor(new DiscordColor(1f, healthPercentage, healthPercentage))
                        .AddField("__Encounter__", $"{enemyName} - LVL {enemyLevel}")
                        .AddField($"HP - {CurrentHP.ToString("0.00")} / {MaxHP.ToString("0.00")}", $"`{ProgressBar.GetProcessBar(healthPercentage)}`")
                        .AddField($"Enemy - {currentHPEnemy.ToString("0.00")} / {maxHPEnemy.ToString("0.00")}", $"`{ProgressBar.GetProcessBar(healthPercentageEnemy)}`");

                    if (!string.IsNullOrEmpty(additional)) {
                        embed.AddField("Info", additional);
                    }

                    if (msg != null) {
                        await msg.DeleteAsync();
                        await Task.Delay(500);
                    }

                    msg = await Channel.SendMessageAsync(embed: embed);
                    await Task.Delay(500);

                    #region Get Reactions

                    var currentPlayerActions = currentUserIds.ToDictionary(x => x, k => -1);

                    async Task CollectActions(CancellationToken token) {
                        while (currentPlayerActions.Values.Any(x => x == -1)) {
                            try {
                                if (token.IsCancellationRequested) { return; }
                                await Task.Delay(1000);
                                foreach (var action in actions) {
                                    if (token.IsCancellationRequested) { return; }
                                    var reactions = await msg.GetReactionsAsync(action.GetEmoji());
                                    await Task.Delay(500);
                                    foreach (var user in reactions) {
                                        if (user.IsBot) { continue; }
                                        if (currentUserIds.Contains(user.Id)) {
                                            Logger.Info($"Found {user.Id} with action {action.GetType().Name}");
                                            currentPlayerActions[user.Id] = action.Id;
                                        }
                                    }
                                }
                            } catch (System.Exception ex) {
                                Logger.Error(ex);
                            }
                        }
                    }

                    #region Add Reactions

                    foreach (var action in actions) {
                        await msg.CreateReactionAsync(action.GetEmoji());
                        await Task.Delay(500);
                    }

                    #endregion Add Reactions

                    using (var timeout = new CancellationTokenSource()) {
                        var task = CollectActions(timeout.Token);
                        var completed = await Task.WhenAny(Task.Delay(TimeSpan.FromMinutes(1), timeout.Token), task);
                        if (completed == task) {
                            timeout.Cancel();
                            await task;
                        } else {
                            Logger.Info("TIMEOUT");
                        }
                    }

                    #endregion Get Reactions

                    #region Handle Actions

                    var totalAttacked = 0f;
                    var totalBlocked = 0f;
                    var ranPlayers = new List<ulong>();
                    foreach (var kv in currentPlayerActions) {
                        if (kv.Value == -1) { continue; }
                        var action = actions.First(x => x.Id == kv.Value);
                        if (action != null) {
                            var playerId = kv.Key;
                            var type = action.GetType();
                            if (type == typeof(Flee)) {
                                //remove from current party
                                ranPlayers.Add(playerId);
                                _ = currentUserIds.Remove(playerId);
                            } else if (type == typeof(Attack)) {
                                var player = Player.GetPlayer(Channel.GuildId, playerId);
                                totalAttacked += player.GetAttack();
                            } else if (type == typeof(Defend)) {
                                var player = Player.GetPlayer(Channel.GuildId, playerId);
                                totalBlocked += player.GetAttack();
                            }
                        }
                    }
                    //LEAVE IF NO PLAYERS REMAIN
                    if (currentUserIds.Count == 0) {
                        await msg.DeleteAsync();
                        await Task.Delay(500);
                        var exp = CalculateExp(enemyLevel, currentHPEnemy, maxHPEnemy);
                        var gold = CalculateGold(enemyLevel, currentHPEnemy, maxHPEnemy);
                        foreach (var id in ranPlayers) {
                            var player = Player.GetPlayer(Channel.GuildId, id);
                            player.AddExperience(exp);
                            player.AddGold(gold);
                            MaxHP -= player.GetTotalHP();
                            player.Update();
                        }

                        return new EncounterData {
                            Message = $"The remaining party ran away safely, and received {exp} exp and {gold} gold."
                        };
                    }

                    currentHPEnemy -= totalAttacked;
                    if (currentHPEnemy <= 0f) {
                        //victory
                        await msg.DeleteAsync();
                        var players = Player.GetPlayers(Channel.GuildId, currentUserIds);

                        var exp = CalculateExp(enemyLevel, 0, 1);
                        var gold = CalculateGold(enemyLevel, 0, 1);
                        foreach (var player in players) {
                            player.Victory(exp, gold);
                            player.Update();
                        }

                        await Task.Delay(500);
                        return new EncounterData() {
                            Ids = currentPlayerActions.Keys,
                            Message = $"Everyone pulled together, and defeated the enemy in {turnCount} turns!\nReceived a total of {exp} exp and {gold} gold.",
                        };
                    }
                    var damage = CalculateDamage(enemyLevel);
                    CurrentHP -= Math.Max(0, damage - totalBlocked);
                    additional = $"Dealt {totalAttacked.ToString("0.00")} damage, and received {Math.Max(0, damage - totalBlocked).ToString("0.00")} damage";
                    if (CurrentHP <= 0f) {
                        //dead
                        await msg.DeleteAsync();
                        var players = Player.GetPlayers(Channel.GuildId, currentUserIds);
                        var exp = (long)Math.Round(CalculateExp(enemyLevel, 0, 1) * 0.75f);
                        foreach (var player in players) {
                            player.CurrentMercenaries = 0;
                            player.AddExperience(-exp);
                            player.Update();
                        }

                        await Task.Delay(500);
                        return new EncounterData {
                            Ids = null,
                            Message = $"Everyone died in {turnCount} turns and lost {exp} exp."
                        };
                    }

                    #endregion Handle Actions
                }
            } catch (System.Exception ex) {
                Logger.Error(ex);
                return new EncounterData();
            }
        }

        private DiscordEmbedBuilder GetQuestCommences() {
            var players = Player.GetPlayers(Channel.GuildId, UserIds);
            return Bot.GetDefaultEmbed()
                .WithImageUrl(BackgroundUrl)
                .AddField("Quest", QuestName)
                .AddField("Characters", string.Join("\n", CharacterBase.Characters.Select(x => $"{x.GetEmoji()} - {x.GetType().Name} - {players.Count(p => p.characterId == x.Id)}")))
                //.AddField("Players", Channel.Users.Where(x => UserIds.Contains(x.Id)).Select(x => x.Username)))
                ;
        }

        private async Task<DiscordMessage> PostStartMessage() {
            var path = ImageGenerator.CreateOrGetImage(null, BackgroundPath, 1f);
            BackgroundUrl = await ImageGenerator.GetImageURL(path);

            var embed = Bot.GetDefaultEmbed()
                .WithImageUrl(BackgroundUrl)
                .AddField("Quest", QuestName);
            var msg = await Channel.SendMessageAsync(embed: embed);
            await Task.Delay(500);

            foreach (var character in CharacterBase.Characters) {
                await msg.CreateReactionAsync(character.GetEmoji());
                await Task.Delay(500);
            }

            return msg;
        }

        private async Task<DiscordEmbedBuilder> RandomEvent(DiscordEmbedBuilder embed) {
            //50% chance
            if (random.Range(0f, 1f) <= 0.5f) {
                var e = RandomEvents.RandomEvent.Events.Random();
                var data = await e.DoEvent(this);

                embed.AddField("**Event**", $"{data.Message}");
                if (!string.IsNullOrEmpty(data.Url)) {
                    embed.WithImageUrl(data.Url);
                }
            }
            return embed;
        }

        private void UpdateHP(IEnumerable<Player> players) {
            var diff = MaxHP - CurrentHP;
            MaxHP = players.Sum(x => x.GetTotalHP());
            CurrentHP = MaxHP - diff;
        }

        #endregion Private Methods
    }
}