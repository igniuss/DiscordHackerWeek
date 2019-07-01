using DSharpPlus.Entities;
using RPGBot.Characters;
using RPGBot.Generative;
using RPGBot.Models;
using RPGBot.Helpers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace RPGBot.Commands {

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
            var characters = CharacterBase.GetAllCharacters();
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
                    player.character = character;
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

                var enemyLevel =(int) Math.Round(Math.Max(random.Range(totalLevel * 0.9f, totalLevel * 20f), 1));
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
                    #endregion

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
}