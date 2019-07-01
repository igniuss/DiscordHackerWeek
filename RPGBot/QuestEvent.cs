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
        public TimeSpan waitTime = TimeSpan.FromMinutes(0.1f);

        private static readonly Random random = new Random();
        private static readonly ConcurrentDictionary<string, string> CachedImages = new ConcurrentDictionary<string, string>();
        private static readonly ImageGenerator ImgGenerator = new ImageGenerator();

        private System.Diagnostics.Stopwatch Timer { get; set; }

        public DiscordChannel Channel { get; private set; }

        public string QuestName { get; private set; }

        public string[] Enemies { get; }
        public string Boss { get; }
        public int EncounterCount { get; private set; }
        public int EncounterIndex { get; private set; }

        public string BackgroundPath { get; private set; }
        public string BackgroundUrl { get; private set; }

        public ulong[] UserIds { get; private set; }
        public float MaxHP { get; private set; }
        public float CurrentHP { get; private set; }

        public bool Success { get; set; }

        public TimeSpan CompletedTime {
            get {
                return TimeSpan.FromMinutes(random.Range(5f, 500f));
            }
        }
        //public TimeSpan CompletedTime {
        //    get {
        //        return Timer.Elapsed;
        //    }
        //}

        public Quest(DiscordChannel channel, string questName, string[] enemies, string boss) {
            QuestName = questName;
            EncounterCount = enemies.Length;
            Channel = channel;
            Enemies = enemies;
            Boss = boss;
        }

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

        private void UpdateHP(IEnumerable<Player> players) {
            var diff = MaxHP - CurrentHP;
            MaxHP = players.Sum(x => x.GetHP());
            CurrentHP = MaxHP - diff;
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

        private float CalculateDamage(int enemyLevel) {
            return enemyLevel * 5f * random.Range(1f, 3f);
        }

        private ulong CalculateGold(int enemyLevel, float currentHP, float maxHP) {
            return (ulong)Math.Ceiling(enemyLevel * 25f * (CurrentHP == 0 ? 1f : 1f - (currentHP / maxHP)));
        }

        private long CalculateExp(int enemyLevel, float currentHP, float maxHP) {
            return (long)Math.Ceiling(enemyLevel * 10f * (CurrentHP == 0 ? 1f : 1f - (currentHP / maxHP)));
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
    }
}


namespace RPGBot.Commands {
    /*
    public class QuestEvent {
        public const float ChanceToHeal = 0.30f;
        //public const float ChanceToBoss = 0.75f;

        private static ImageGenerator ImageGenerator { get; set; } = new ImageGenerator();
        private static QuestGenerator QuestGenerator { get; set; } = new QuestGenerator();
        private static NamesGenerator NamesGenerator { get; set; } = new NamesGenerator();

        public DiscordChannel Channel { get; private set; }
        public ConcurrentDictionary<CharacterBase, List<ulong>> CurrentPlayers { get; private set; }

        public double CurrentHP { get; private set; } = 1000;
        public double MaxHP { get; private set; } = 1000;
        public string QuestName { get; private set; }

        public QuestEvent(DiscordChannel channel) {
            Channel = channel;
            CurrentPlayers = new ConcurrentDictionary<CharacterBase, List<ulong>>();
        }

        public async Task RandomDelayMinutes(float min, float max) {
            var r = new Random();
            var t = r.NextDouble();

            await Task.Delay(TimeSpan.FromMinutes(min + ((max - min) * t))); //LERP
        }

        public async Task StartQuest() {
            //Grab all Characters

            #region Get Everything Setup

            var backdrop = ImageGenerator.RandomBackground();
            var url = await RPGCommands.GetURL(ImageGenerator.CreateImage(null, backdrop));
            var characters = CharacterBase.Characters;
            var characterEmojis = characters.Select(x => x.GetEmoji());

            #endregion Get Everything Setup

            #region Join Quest Message

            QuestName = QuestGenerator.GetResult();
            var embed = new DiscordEmbedBuilder()
                .WithTitle("[Adventure] Leaving in 1 minute!")
                .WithImageUrl(url)
                .WithDescription($@"**Current Quest:**
{QuestName}

Pick your Fighter!
{string.Join("\n", characters.Select(x => $"{x.GetType().Name} - {x.GetEmoji()}"))}")
                .WithColor(DiscordColor.Green)
                .WithFooter("[RPG-Bot] Created by Jeff&Iggy 👋");

            var msg = await Channel.SendMessageAsync(embed: embed);

            #endregion Join Quest Message

            #region Collect Players

            CurrentPlayers.Clear();

            foreach (var emoji in characterEmojis) {
                await msg.CreateReactionAsync(emoji);
                await Task.Delay(500);
            }

            await Task.Delay(TimeSpan.FromMinutes(1));
            var reactions = new ConcurrentDictionary<DiscordEmoji, List<DiscordUser>>();
            foreach (var emoji in characterEmojis) {
                var reacts = await msg.GetReactionsAsync(emoji);
                await Task.Delay(500);
                reactions.TryAdd(emoji, reacts.ToList());
            }

            var votedUsers = new List<ulong>();
            foreach (var reaction in reactions) {
                var character = characters.First(x => x.GetEmoji() == reaction.Key);
                foreach (var user in reaction.Value) {
                    if (user.IsBot || votedUsers.Contains(user.Id)) { continue; }
                    if (!CurrentPlayers.ContainsKey(character)) {
                        CurrentPlayers.TryAdd(character, new List<ulong>());
                    }
                    var player = Player.GetPlayer(Channel.GuildId, user.Id);
                    player.TotalQuests++;
                    player.characterId = character.Id;
                    player.Update();

                    CurrentPlayers[character].Add(player.Id);
                    votedUsers.Add(user.Id);
                }
            }
            if (CurrentPlayers.Count == 0) {
                var emb = new DiscordEmbedBuilder(embed)
                    .WithDescription(embed.Description + "\n\nNo-one joined the Quest.")
                    .WithTitle("[Adventure] - Empty 😔");

                await msg.ModifyAsync(embed: new Optional<DiscordEmbed>(emb));
                await msg.DeleteAllReactionsAsync();
                await Task.Delay(500);
                return;
            }

            #endregion Collect Players

            #region Start Quest Message

            embed = new DiscordEmbedBuilder(embed)
                 .WithDescription($@"The Quest begins!
{QuestName}
{string.Join("\n",

CurrentPlayers.Select(x => $"{x.Key.GetType().Name} {x.Key.GetEmoji()} - {x.Value.Count}"))}")
                 .WithImageUrl(url)
                 .WithTitle($"Quest: {QuestName}")
                 .WithColor(DiscordColor.Blue);

            await msg.DeleteAsync();
            await Task.Delay(500);
            msg = await Channel.SendMessageAsync(embed: embed);
            await Task.Delay(500);

            #endregion Start Quest Message

            #region SET HP

            CurrentHP = 0f;
            MaxHP = 0f;
            foreach (var kv in CurrentPlayers) {
                foreach (var playerId in kv.Value) {
                    var player = Player.GetPlayer(Channel.Guild.Id, playerId);
                    MaxHP += player.GetHP();
                }
            }
            CurrentHP = MaxHP;

            #endregion SET HP

            #region Encounter Loop

            var random = new Random();
            var encounterCount = random.Next(4, 20);
            for (var i = 0; i < encounterCount; i++) {
                //await Task.Delay(1000);
                await RandomDelayMinutes(0.15f, 1f);
                var enemy = ImageGenerator.RandomCharacter();
                if (random.NextDouble() > 0.5) {
                    backdrop = ImageGenerator.RandomBackground();
                }
                var totalLevel = CurrentPlayers.Values.SelectMany(x => x).Select(x => Player.GetPlayer(Channel.GuildId, x)).Sum(x => x.GetCurrentLevel());

                //level between current-5 & current+2
                var enemyLevel = (int)Math.Round(Math.Max(random.Range(totalLevel * 0.75f, totalLevel * 3.0f), 1));

                await Ecounter(enemy, backdrop, embed, enemyLevel);
                if (CurrentHP <= 0f) { break; }
                var emb = new DiscordEmbedBuilder(embed)
                    .WithTitle($"The quest continues - {QuestName}")
                    .WithDescription($"{QuestName}")
                    .WithImageUrl(url)
                    .WithColor(DiscordColor.LightGray);

                await msg.DeleteAsync();
                await Task.Delay(500);
                msg = await Channel.SendMessageAsync(embed: emb);
                await Task.Delay(500);
            }

            #endregion Encounter Loop

            //Boss fight OR chest

            #region BOSSFIGHT

            if (CurrentHP > 0) {
                await RandomDelayMinutes(0.15f, 1f);
                var enemy = ImageGenerator.RandomCharacter(true);
                backdrop = ImageGenerator.RandomBackground();

                var totalLevel = CurrentPlayers.Values.SelectMany(x => x).Select(x => Player.GetPlayer(Channel.GuildId, x)).Sum(x => x.GetCurrentLevel());

                var enemyLevel = (int)Math.Round(Math.Max(random.Range(totalLevel * 0.9f, totalLevel * 20f), 1));
                await Ecounter(enemy, backdrop, embed, enemyLevel);
            }

            #endregion BOSSFIGHT

            #region SUCCESS

            if (CurrentHP > 0) {
                await Channel.SendMessageAsync($"Event has been completed!\n\nCongratulations to\n{string.Join(",\n", CurrentPlayers.Values.SelectMany(x => x).Select(x => Player.GetPlayer(Channel.Guild.Id, x)).Select(x => Channel.Users.First(y => y.Id == x.Id).Username))}");
                await Task.Delay(500);
                foreach (var player in CurrentPlayers.Values.SelectMany(x => x).Select(x => Player.GetPlayer(Channel.Guild.Id, x))) {
                    player.SuccessfulQuests++;
                    player.Update();
                }
            }

            #endregion SUCCESS
        }

        public async Task Ecounter(string enemy, string backdrop, DiscordEmbed ogEmbed, int level) {
            try {
                var random = new Random();
                var original = ImageGenerator.CreateImage(enemy, backdrop);
                var url = await RPGCommands.GetURL(original);
                var actions = Actions.ActionBase.GetAllActions();

                var enemyLevel = level;

                var maxEnemyHP = enemyLevel * 50f;
                var currentEnemyHP = maxEnemyHP;
                var enemyName = NamesGenerator.GetResult();

                var damageDealt = 0f;
                var damageReceived = 0f;
                var damageBlocked = 0f;

                var turnCount = 0;
                while (currentEnemyHP > 0) {
                    turnCount++;
                    if (CurrentHP < MaxHP) {
                        url = await RPGCommands.GetURL(ImageGenerator.SimulateDamage(original, 1f - (float)(CurrentHP / MaxHP)));
                    }

                    var embed = new DiscordEmbedBuilder(ogEmbed)
                        .WithColor(DiscordColor.Red)
                        .WithTitle($"Encounter! - {QuestName}")
                        .WithDescription($@"A MONSTER APPEARED
{enemyName} - Level {enemyLevel}
```
HP:    {ProgressBar.GetProcessBar(CurrentHP / MaxHP)}
ENEMY: {ProgressBar.GetProcessBar(currentEnemyHP / maxEnemyHP)}
```
")
                        .WithImageUrl(url);

                    if (damageDealt > 0f || damageReceived > 0f) {
                        embed.Description += $@"
Damage Dealt : {damageDealt}
Damage Taken : {Math.Max(0, damageReceived - damageBlocked)}
";
                        damageDealt = 0;
                        damageReceived = 0;
                        damageBlocked = 0;
                    }
                    //Calculate damage
                    var msg = await Channel.SendMessageAsync(embed: embed);
                    await Task.Delay(500);

                    var actionQueue = new ConcurrentDictionary<Player, Actions.ActionBase>();
                    var playerIds = CurrentPlayers.Values.SelectMany(x => x);
                    async Task handler(DSharpPlus.EventArgs.MessageReactionAddEventArgs e) {
                        //only check our own msg
                        if (e.Message.Id == msg.Id) {
                            await Task.Delay(500);
                            //only check actions we predefined
                            var action = actions.First(x => x.GetEmoji() == e.Emoji);
                            if (action != null) {
                                //only check for reactions of current players
                                if (playerIds.Any(x => x == e.User.Id)) {
                                    var player = Player.GetPlayer(Channel.GuildId, e.User.Id);
                                    if (player != null) {
                                        actionQueue.AddOrUpdate(player, action, (key, value) => action);
                                    }
                                }
                            }
                        }
                        await Task.Delay(500);
                        return;
                    }

                    Bot.Client.MessageReactionAdded += handler;

                    //add the actual reactions
                    foreach (var action in actions) {
                        await msg.CreateReactionAsync(action.GetEmoji());
                        await Task.Delay(500);
                    }

                    var timeoutTimer = Stopwatch.StartNew();
                    //while the queue keys are smaller than the total amount of players, we wait
                    while (actionQueue.Keys.Count < playerIds.Count()) {
                        await Task.Delay(100);
                        //just run if we've been going for 60 seconds.
                        if (timeoutTimer.Elapsed.TotalSeconds > 60) { break; }
                    }
                    //remove the handler
                    Bot.Client.MessageReactionAdded -= handler;

                    #region CALCULATE

                    var fledPlayers = new List<Player>();
                    foreach (var kv in actionQueue) {
                        if (kv.Value.GetType() == typeof(Actions.Flee)) { //Player runs the fuck away
                            //Get a little xp for running, if you did damage

                            fledPlayers.Add(kv.Key);
                        }
                    }
                    foreach (var fledPlayer in fledPlayers) {
                        actionQueue.TryRemove(fledPlayer, out _);
                        if (CurrentPlayers.ContainsKey(fledPlayer.character)) {
                            CurrentPlayers[fledPlayer.character].Remove(fledPlayer.Id);
                        } else {
                            Console.WriteLine("For some reason the character wasn't in there??!!");
                            //bruteforce
                            foreach (var kv in CurrentPlayers) {
                                if (kv.Value.Contains(fledPlayer.Id)) {
                                    kv.Value.DefaultIfEmpty(fledPlayer.Id);
                                    Console.WriteLine($"Found player in {kv.Key.GetType().Name}");
                                    break;
                                }
                            }
                        }

                        CurrentHP -= fledPlayer.GetHP();
                        MaxHP -= fledPlayer.GetHP();

                        var exp = (long)Math.Ceiling(enemyLevel * 10f * (currentEnemyHP / maxEnemyHP));
                        fledPlayer.IncreaseExperience(exp);
                        fledPlayer.character = null;
                        fledPlayer.Update();
                    }

                    foreach (var kv in actionQueue) {
                        var charType = kv.Key.character;
                        if (kv.Value.GetType() == typeof(Actions.Attack)) {
                            damageDealt += kv.Key.GetAttack();
                        } else if (kv.Value.GetType() == typeof(Actions.Defend)) {
                            damageBlocked += kv.Key.GetDefense();
                        }
                    }

                    currentEnemyHP -= damageDealt;
                    var enemyDamage = enemyLevel * 5f * (random.Range(1, 6) * 0.5f);
                    damageReceived += enemyDamage;
                    if (damageReceived > damageBlocked) {
                        CurrentHP -= damageReceived - damageBlocked;
                    }

                    #region DELETE PREVIOUS BATTLE

                    await msg.DeleteAsync();
                    await Task.Delay(500);

                    #endregion DELETE PREVIOUS BATTLE

                    playerIds = CurrentPlayers.Values.SelectMany(x => x);

                    if (playerIds.Count() == 0) {
                        await Channel.SendMessageAsync($"Everyone ran in {turnCount} turns.");
                        await Task.Delay(500);
                        return;
                    }

                    if (CurrentHP <= 0f) {

                        #region PLAYER DEATH

                        foreach (var playerId in playerIds) {
                            var player = Player.GetPlayer(Channel.GuildId, playerId);
                            var deathExp = -(long)Math.Ceiling(enemyLevel * random.Next(1, 5) * 5f);
                            player.Death(deathExp);
                            player.Update();
                        }
                        await Channel.SendMessageAsync($"The Creature has defeated everyone in {turnCount} turns!");
                        await Task.Delay(500);

                        #endregion PLAYER DEATH

                        return;
                    }

                    #endregion CALCULATE

                    await Task.Delay(500);
                }

                #region PLAYER VICTORY

                var goldReceived = (ulong)Math.Ceiling(enemyLevel * random.Next(1, 5) * 25f);
                var expeReceived = (long)Math.Ceiling(enemyLevel * random.Next(1, 5) * 15f);

                double healed = 0f;
                var heal = random.NextDouble() <= ChanceToHeal;

                foreach (var playerId in CurrentPlayers.Values.SelectMany(x => x)) {
                    var player = Player.GetPlayer(Channel.Guild.Id, playerId);
                    player.Victory(expeReceived, goldReceived);
                    player.Update();

                    if (heal) {
                        healed += Math.Min(MaxHP, CurrentHP + (player.GetHP() * 0.15f));
                    }
                }

                if (heal) {
                    CurrentHP = Math.Min(MaxHP, CurrentHP + healed);
                    url = await RPGCommands.GetURL(ImageGenerator.NightTime(backdrop));
                    var embed = new DiscordEmbedBuilder(ogEmbed)
                        .WithColor(DiscordColor.DarkBlue)
                        .WithImageUrl(url)
                        .WithDescription($@"You've defeated a mighty opponent in {turnCount} turns!
Every survivor collected {goldReceived} gold.
Your party has healed up to {CurrentHP}HP");
                    await Channel.SendMessageAsync(embed: embed);
                    await Task.Delay(500);
                } else {
                    await Channel.SendMessageAsync($@"You've defeated a mighty opponent in {turnCount} turns!
Every survivor collected {goldReceived} gold.");
                    await Task.Delay(500);
                }
            } catch (System.Exception ex) {
                await Channel.SendMessageAsync(ex.ToString());
                await Task.Delay(500);
            }

            #endregion PLAYER VICTORY
        }
    }
    */
}