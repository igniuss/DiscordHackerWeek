using DSharpPlus.Entities;
using RPGBot.Characters;
using RPGBot.Generative;
using RPGBot.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace RPGBot.Commands {
    public class PublicEvent {
        private static ImageGenerator ImageGenerator { get; set; } = new ImageGenerator();
        private static QuestGenerator QuestGenerator { get; set; } = new QuestGenerator();
        private static NamesGenerator NamesGenerator { get; set; } = new NamesGenerator();

        public DiscordChannel Channel { get; private set; }
        public ConcurrentDictionary<CharacterBase, List<Player>> CurrentPlayers { get; private set; }

        public double CurrentHP { get; private set; } = 1000;
        public double MaxHP { get; private set; } = 1000;

        public PublicEvent(DiscordChannel channel) {
            Channel = channel;
            CurrentPlayers = new ConcurrentDictionary<CharacterBase, List<Player>>();
        }

        public async Task RandomDelayMinutes(int min, int max) {
            var r = new Random();
            await Task.Delay(TimeSpan.FromMinutes(r.Next(min, max)));
        }

        public async Task<ConcurrentDictionary<DiscordEmoji, int>> GetReactions(DiscordMessage msg, IEnumerable<DiscordEmoji> emojis, TimeSpan timeout) {
            if (msg == null) { return null; }
            if (emojis == null) { return null; }

            foreach (var emoji in emojis) {
                await msg.CreateReactionAsync(emoji);
            }
            await Task.Delay(timeout);
            var reactions = new ConcurrentDictionary<DiscordEmoji, List<DiscordUser>>();
            foreach (var emoji in emojis) {
                var reacts = await msg.GetReactionsAsync(emoji);
                reactions.TryAdd(emoji, reacts.ToList());
            }

            var votedUsers = new List<ulong>();
            var result = new ConcurrentDictionary<DiscordEmoji, int>();
            foreach (var reaction in reactions) {
                foreach (var user in reaction.Value) {
                    if (user.IsBot || votedUsers.Contains(user.Id)) { continue; }

                    result.AddOrUpdate(reaction.Key, 1, (key, count) => count + 1);
                    votedUsers.Add(user.Id);
                }
            }

            return result;
        }

        public async Task StartQuest() {
            //Grab all Characters

            #region Get Everything Setup

            var backdrop = ImageGenerator.RandomBackground();
            var url = await RPGCommands.GetURL(ImageGenerator.CreateImage(null, backdrop));
            var characters = typeof(Characters.CharacterBase).Assembly.GetTypes()
                .Where(t => t.IsSubclassOf(typeof(Characters.CharacterBase)) && !t.IsAbstract)
                .Select(t => (Characters.CharacterBase)Activator.CreateInstance(t));

            var characterEmojis = characters.Select(x => x.GetEmoji());

            #endregion Get Everything Setup

            #region Join Quest Message

            var embed = new DiscordEmbedBuilder()
                .WithTitle("[Adventure] Leaving in 1 minute!")
                .WithImageUrl(url)
                .WithDescription($@"**Current Quest:**
{QuestGenerator.GetResult()}

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
            }
            await Task.Delay(TimeSpan.FromMinutes(0.1));
            var reactions = new ConcurrentDictionary<DiscordEmoji, List<DiscordUser>>();
            foreach (var emoji in characterEmojis) {
                var reacts = await msg.GetReactionsAsync(emoji);
                reactions.TryAdd(emoji, reacts.ToList());
            }

            var votedUsers = new List<ulong>();
            foreach (var reaction in reactions) {
                var character = characters.First(x => x.GetEmoji() == reaction.Key);
                foreach (var user in reaction.Value) {
                    if (user.IsBot || votedUsers.Contains(user.Id)) { continue; }
                    if (!CurrentPlayers.ContainsKey(character)) {
                        CurrentPlayers.TryAdd(character, new List<Player>());
                    }
                    try {
                        var player = Player.GetPlayer(Channel.GuildId, user.Id);
                        player.character = character;
                        player.discordUser = user;

                        CurrentPlayers[character].Add(player);
                        votedUsers.Add(user.Id);
                    } catch (System.Exception ex) {
                        Console.WriteLine(ex);
                    }
                }
            }

            #region SET HP

            CurrentHP = 0f;
            MaxHP = 0f;
            foreach (var player in CurrentPlayers.Values.SelectMany(x => x)) {
                MaxHP += player.GetHP();
            }
            CurrentHP = MaxHP;

            #endregion SET HP

            #endregion Collect Players

            #region Start Quest Message

            embed = new DiscordEmbedBuilder(embed)
                 .WithDescription($@"Quest commences!

{string.Join("\n",

CurrentPlayers.Select(x => $"{x.Key.GetType().Name} {x.Key.GetEmoji()} - {x.Value.Count}"))}")
                 .WithImageUrl(url)
                 .WithColor(DiscordColor.Blue);

            msg = await Channel.SendMessageAsync(embed: embed);

            #endregion Start Quest Message

            #region Encounter Loop

            var random = new Random();
            var encounterCount = random.Next(1, 5);
            for (var i = 0; i < encounterCount; i++) {
                await RandomDelayMinutes(1, 5);
                var enemy = ImageGenerator.RandomCharacter();
                await Ecounter(enemy, backdrop, embed);
                if (CurrentHP <= 0f) { break; }
            }
            await Channel.SendMessageAsync("Event has been completed! Congratulations to everyone who participated!");
            await Channel.SendMessageAsync($"Congratulations to {string.Join("\n", CurrentPlayers.Values.SelectMany(x => x).Select(x => x.discordUser))}");

            #endregion Encounter Loop
        }

        public async Task Ecounter(string enemy, string backdrop, DiscordEmbed ogEmbed) {
            try {
                var random = new Random();
                var url = await RPGCommands.GetURL(ImageGenerator.CreateImage(enemy, backdrop));
                var actions = typeof(Actions.ActionBase).Assembly.GetTypes()
                    .Where(t => t.IsSubclassOf(typeof(Actions.ActionBase)) && !t.IsAbstract)
                    .Select(t => (Actions.ActionBase)Activator.CreateInstance(t));

                var maxEnemyHP = (double)random.Next(10, 100);
                var currentEnemyHP = maxEnemyHP;
                var damageDealt = 0f;
                var damageReceived = 0f;
                var damageBlocked = 0f;

                while (currentEnemyHP > 0) {
                    var embed = new DiscordEmbedBuilder(ogEmbed)
                        .WithColor(DiscordColor.Red)
                        .WithDescription($@"RANDOM ENCOUNTER!

You have 1 minute to respond!
```
HP:     {ProgressBar.GetProcessBar(CurrentHP / MaxHP)}
ENEMY:  {ProgressBar.GetProcessBar(currentEnemyHP / maxEnemyHP)}
```
")
                        .WithImageUrl(url);

                    if (damageDealt > 0f || damageReceived > 0f) {
                        embed.Description += $@"
DMG : -{damageReceived}
ATK : {damageDealt}
DEF : {damageBlocked}
";
                        damageDealt = 0;
                        damageReceived = 0;
                        damageBlocked = 0;
                    }
                    //Calculate damage
                    var msg = await Channel.SendMessageAsync(embed: embed);
                    foreach (var action in actions) {
                        await msg.CreateReactionAsync(action.GetEmoji());
                    }

                    //check if everyone voted;
                    var actionQueue = new ConcurrentDictionary<Player, Actions.ActionBase>();
                    var players = CurrentPlayers.Values.SelectMany(x => x);

                    async Task handler(DSharpPlus.EventArgs.MessageReactionAddEventArgs e) {
                        //only check our own msg
                        if (e.Message.Id == msg.Id) {
                            //only check actions we predefined
                            var action = actions.First(x => x.GetEmoji() == e.Emoji);
                            if (action != null) {
                                //only check for reactions of current players
                                var player = players.First(x => x.Id == e.User.Id);
                                if (player != null) {
                                    actionQueue.AddOrUpdate(player, action, (key, value) => action);
                                }
                            }
                        }
                        await Task.Delay(0);
                        return;
                    }

                    Console.WriteLine("ADDING HANDLER");
                    Bot.Client.MessageReactionAdded += handler;

                    var timeoutTimer = Stopwatch.StartNew();
                    //while the queue keys are smaller than the total amount of players, we wait
                    while (actionQueue.Keys.Count < players.Count()) {
                        await Task.Delay(100);
                        //just run if we've been going for 60 seconds.
                        if (timeoutTimer.Elapsed.TotalSeconds > 60) { break; }
                    }
                    //remove the handler
                    Bot.Client.MessageReactionAdded -= handler;

                    Console.WriteLine("REMOVING HANDLER");

                    #region CALCULATE

                    var fledPlayers = new List<Player>();
                    foreach (var kv in actionQueue) {
                        if (kv.Value.GetType() == typeof(Actions.Flee)) { //Player runs the fuck away
                            fledPlayers.Add(kv.Key);
                        }
                    }
                    foreach (var fledPlayer in fledPlayers) {
                        actionQueue.TryRemove(fledPlayer, out _);
                        CurrentPlayers[fledPlayer.character].Remove(fledPlayer);
                        CurrentHP -= fledPlayer.GetHP();
                        MaxHP -= fledPlayer.GetHP();
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
                    var enemyDamage = 10f;
                    damageReceived += enemyDamage;
                    if (damageReceived > damageBlocked) {
                        CurrentHP -= damageReceived - damageBlocked;
                    }

                    if (CurrentHP <= 0f) {
                        await Channel.SendMessageAsync("The Creature has won!");
                        return;
                    }

                    if (players.Count() == 0 || CurrentHP <= 0f) {
                        await Channel.SendMessageAsync("YOU'VE DIED! RIP 👌👌👌");
                        break;
                    }

                    #endregion CALCULATE

                    await Task.Delay(100);
                }
            } catch (System.Exception ex) {
                await Channel.SendMessageAsync(ex.ToString());
            }

            var goldReceived = (ulong)100;
            await Channel.SendMessageAsync($"You've defeated a mighty opponent!\nEveryone who joined the fight received {goldReceived} gold");
            foreach (var player in CurrentPlayers.Values.SelectMany(x => x)) {
                player.Gold += goldReceived;
                try {
                    player.Update();
                } catch (System.Exception ex) {
                    await Channel.SendMessageAsync(ex.ToString());
                }
            }
        }
    }
}
