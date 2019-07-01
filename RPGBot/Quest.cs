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

        #region Public Fields

        public TimeSpan waitTime = TimeSpan.FromMinutes(0.1f);

        #endregion Public Fields

        #region Private Fields

        private static readonly ConcurrentDictionary<string, string> CachedImages = new ConcurrentDictionary<string, string>();
        private static readonly ImageGenerator ImgGenerator = new ImageGenerator();
        private static readonly Random random = new Random();

        #endregion Private Fields

        #region Public Properties

        public string BackgroundPath { get; private set; }
        public string BackgroundUrl { get; private set; }
        public string Boss { get; }
        public DiscordChannel Channel { get; private set; }

        public TimeSpan CompletedTime {
            get {
                return TimeSpan.FromMinutes(random.Range(5f, 500f));
            }
        }

        public float CurrentHP { get; private set; }
        public int EncounterCount { get; private set; }
        public int EncounterIndex { get; private set; }
        public string[] Enemies { get; }
        public float MaxHP { get; private set; }
        public string QuestName { get; private set; }
        public bool Success { get; set; }
        public ulong[] UserIds { get; private set; }

        #endregion Public Properties

        #region Private Properties

        private System.Diagnostics.Stopwatch Timer { get; set; }

        #endregion Private Properties

        #region Public Constructors

        public Quest(DiscordChannel channel, string questName, string[] enemies, string boss) {
            QuestName = questName;
            EncounterCount = enemies.Length;
            Channel = channel;
            Enemies = enemies;
            Boss = boss;
        }

        #endregion Public Constructors

        #region Public Methods

        //public TimeSpan CompletedTime {
        //    get {
        //        return Timer.Elapsed;
        //    }
        //}
        public async Task<Quest> Start() {
            Timer = System.Diagnostics.Stopwatch.StartNew();
            //Send 'Starting Quest'

            BackgroundPath = ImgGenerator.RandomBackground();

            #region Starting Quest

            var msg = await PostStartMessage();
            //Collect players playing.

            #endregion Starting Quest

            await Task.Delay(this.waitTime);

            #region CollectPlayers

            var userIds = new List<ulong>();
            var characters = CharacterBase.Characters;
            foreach (var character in characters) {
                await Task.Delay(300);
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
                Timer.Stop();
                Success = false;
                return null;
            }

            UserIds = userIds.ToArray();
            await msg.DeleteAsync();

            #endregion CollectPlayers

            msg = await PostQuestCommenceMessage();
            await Task.Delay(this.waitTime);

            for (EncounterIndex = 0; EncounterIndex < EncounterCount; EncounterIndex++) {
                //do an encounter
                var enemy = Enemies[EncounterIndex];

                //Fetch updated player stats
                var players = Player.GetPlayers(Channel.GuildId, UserIds);
                UpdateHP(players);

                var enemyLevel = players.Sum(x => x.GetCurrentLevel());
                enemyLevel = (int)Math.Round(enemyLevel * random.Range(0.75f, 2f));

                var survivors = await Encounter(enemy, enemyLevel);
                if (survivors == null || survivors.Count() == 0) {
                    Success = false;
                    Timer.Stop();
                    return this;
                }
                UserIds = survivors.ToArray();
                //wait between 30 and 120 seconds for next encounter.
                await Task.Delay(TimeSpan.FromSeconds(random.Range(30f, 120f)));
            }
            //BOSS FIGHT
            {
                var enemy = Boss;

                //Fetch updated player stats
                var players = Player.GetPlayers(Channel.GuildId, UserIds);
                UpdateHP(players);

                var enemyLevel = players.Sum(x => x.GetCurrentLevel());
                enemyLevel = (int)Math.Round(enemyLevel * random.Range(0.95f, 5f));

                var survivors = await Encounter(enemy, enemyLevel);
                if (survivors == null || survivors.Count() == 0) {
                    Success = false;
                    Timer.Stop();
                    return this;
                }
                UserIds = survivors.ToArray();
            }

            Success = true;
            Timer.Stop();

            return this;
        }

        #endregion Public Methods

        #region Private Methods

        private float CalculateDamage(int enemyLevel) {
            return enemyLevel * 5f * random.Range(1f, 3f);
        }

        private long CalculateExp(int enemyLevel, float currentHP, float maxHP) {
            return (long)Math.Ceiling(enemyLevel * 10f * (CurrentHP == 0 ? 1f : 1f - (currentHP / maxHP)));
        }

        private ulong CalculateGold(int enemyLevel, float currentHP, float maxHP) {
            return (ulong)Math.Ceiling(enemyLevel * 25f * (CurrentHP == 0 ? 1f : 1f - (currentHP / maxHP)));
        }

        private async Task<IEnumerable<ulong>> Encounter(string enemyPath, int enemyLevel) {
            try {
                var url = await GetImageURL(enemyPath, BackgroundPath);
                var currentUserIds = UserIds.ToList();

                var maxHPEnemy = enemyLevel * 50 * random.Range(0.8f, 1.5f);
                var currentHPEnemy = maxHPEnemy;

                DiscordMessage msg = null;
                var actions = Actions.ActionBase.GetAllActions();
                var turnCount = 0;
                while (true) {
                    turnCount++;

                    var healthPercentage = CurrentHP / MaxHP;
                    var healthPercentageEnemy = currentHPEnemy / maxHPEnemy;

                    var embed = Bot.GetDefaultEmbed()
                        .WithImageUrl(url)
                        .AddField("Quest", QuestName)
                        .WithColor(new DiscordColor(1f, healthPercentage, healthPercentage))
                        .AddField("__Encounter__", $"{System.IO.Path.GetFileNameWithoutExtension(enemyPath)} - LVL {enemyLevel}")
                        .AddField($"HP - {CurrentHP.ToString("0.00")} / {MaxHP.ToString("0.00")}", $"`{ProgressBar.GetProcessBar(healthPercentage)}`")
                        .AddField($"Enemy - {currentHPEnemy.ToString("0.00")} / {maxHPEnemy.ToString("0.00")}", $"`{ProgressBar.GetProcessBar(healthPercentageEnemy)}`");

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
                            foreach (var action in actions) {
                                if (token.IsCancellationRequested) { return; }
                                var reactions = await msg.GetReactionsAsync(action.GetEmoji());
                                await Task.Delay(500);
                                foreach (var user in reactions) {
                                    if (user.IsBot) { continue; }
                                    if (currentUserIds.Contains(user.Id)) {
                                        Console.WriteLine($"Found {user.Id} with action {action.GetType().Name}");
                                        currentPlayerActions[user.Id] = action.Id;
                                    }
                                }
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
                            Console.WriteLine("TIMEOUT");
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

                            MaxHP -= player.GetHP();
                            player.CurrentMercenaries = 0;
                            //save changes
                            player.Update();
                        }

                        await Channel.SendMessageAsync($"The remaining party ran away safely, and received {exp} exp and {gold} gold.");
                        return null;
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
                        await Channel.SendMessageAsync($"Everyone pulled together, and defeated the enemy in {turnCount} turns!\nReceived a total of {exp} exp and {gold} gold.");
                        return currentPlayerActions.Keys;
                    }

                    CurrentHP -= Math.Max(0, CalculateDamage(enemyLevel) - totalBlocked);
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
                        await Channel.SendMessageAsync($"Everyone died in {turnCount} turns and lost {exp} exp.");
                        return null;
                    }

                    #endregion Handle Actions
                }
            } catch (System.Exception ex) {
                Console.WriteLine(ex);
                return null;
            }
        }

        private async Task<string> GetImageURL(string enemy, string background) {
            var path = ImgGenerator.CreateImage(enemy, background);
            if (CachedImages.TryGetValue(path, out var url)) {
                return url;
            }
            var msg = await Bot.ImageCache.SendFileAsync(path);
            url = msg.Attachments.First().Url;
            CachedImages.TryAdd(path, url);
            return url;
        }

        private async Task<DiscordMessage> PostQuestCommenceMessage() {
            var players = Player.GetPlayers(Channel.GuildId, UserIds);
            var embed = Bot.GetDefaultEmbed()
                .WithImageUrl(BackgroundUrl)
                .AddField("Quest", QuestName)
                .AddField("Characters", string.Join("\n", CharacterBase.Characters.Select(x => $"{x.GetEmoji()} - {x.GetType().Name} - {players.Count(p => p.characterId == x.Id)}")))
                //.AddField("Players", Channel.Users.Where(x => UserIds.Contains(x.Id)).Select(x => x.Username)))
                ;
            return await Channel.SendMessageAsync(embed: embed);
        }

        private async Task<DiscordMessage> PostStartMessage() {
            BackgroundUrl = await GetImageURL(null, BackgroundPath);
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

        private void UpdateHP(IEnumerable<Player> players) {
            var diff = MaxHP - CurrentHP;
            MaxHP = players.Sum(x => x.GetHP());
            CurrentHP = MaxHP - diff;
        }

        #endregion Private Methods
    }
}