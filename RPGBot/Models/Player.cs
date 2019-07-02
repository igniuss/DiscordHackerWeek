using RPGBot.Characters;
using RPGBot.Helpers;
using RPGBot.Items;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGBot.Models {

    public class Player {
        private readonly Random random = new Random();

        public ulong Id { get; set; }
        public ulong GuildId { get; set; }
        public ulong[] Experience { get; set; }
        public ulong Gold { get; set; }
        public int EnemiesKilled { get; set; }
        public int TotalQuests { get; set; }
        public int SuccessfulQuests { get; set; }
        public int DeathCounter { get; set; }
        public int LifetimeMercenariesHired { get; set; }

        [LiteDB.BsonIgnore]
        public int CurrentMercenaries {
            get {
                return Items.FirstOrDefault(x => x.GetType() == typeof(Mercenary)).Count;
            }
            set {
                Items.FirstOrDefault(x => x.GetType() == typeof(Mercenary)).Count = value;
            }
        }

        public List<ItemBase> Items { get; set; }

        //public CharacterBase character { get; set; }
        public int characterId { get; set; }

        public DateTime LastVoted { get; set; }
        public ulong VoteStreak { get; set; }

        //public DiscordUser discordUser;

        public Player() {
        }

        public static IEnumerable<Player> GetPlayers(ulong guildId, IEnumerable<ulong> Ids) {
            var players = DB.Find<Player>($"{guildId}.db", "players", x => Ids.Contains(x.Id));
            return players;
        }

        public static Player GetPlayer(ulong guildId, ulong id) {
            var player = DB.FindOne<Player>($"{guildId}.db", "players", x => x.Id == id);
            if (player == null) {
                player = new Player {
                    Id = id,
                    GuildId = guildId
                };
                DB.Insert($"{guildId}.db", "players", player);
            }

            if (player.Experience == null) {
                var characters = Characters.CharacterBase.Characters;
                player.Experience = new ulong[characters.Count()];
            }

            if (player.Items == null) {
                player.Items = ItemBase.GetAllItems().ToList();
            }

            return player;
        }

        public bool Update() {
            return DB.Update($"{GuildId}.db", "players", this);
        }

        public static int CalculateLevel(ulong exp) {
            return (int)Math.Round((Math.Sqrt(650f + (100f * exp)) - 25f) / 50f) + 1;
        }

        public ulong GetCurrentExp() {
            return Experience.GetSafe<ulong>(characterId, 0);
        }

        public int GetCurrentLevel() {
            var exp = GetCurrentExp();
            return CalculateLevel(exp);
        }

        public float GetTotalHP() {
            var character = CharacterBase.GetCharacter(characterId);
            if (character == null) { return 0f; }
            var lvl = GetCurrentLevel();
            var _base = 120;
            var mercenaryBonus = _base * CurrentMercenaries;
            return (lvl * _base * character.HealthMultiplier) + mercenaryBonus;
        }

        public float GetAttack() {
            var character = CharacterBase.GetCharacter(characterId);
            if (character == null) { return 0f; }
            var lvl = GetCurrentLevel();
            var _base = 5;
            var crit = this.random.Range(1f, 6f) * 0.75f;
            var mercenaryBonus = _base * CurrentMercenaries;
            return (lvl * _base * character.AttackPowerMultiplier * crit) + mercenaryBonus;
        }

        public float GetDefense() {
            var character = CharacterBase.GetCharacter(characterId);
            if (character == null) { return 0f; }
            var lvl = GetCurrentLevel();
            var _base = 5;
            var crit = this.random.Range(1f, 6f) * 0.75f;
            var mercenaryBonus = _base * CurrentMercenaries;
            return (lvl * _base * character.DefenseMultiplier * crit) + mercenaryBonus;
        }

        public void AddGold(ulong gold) {
            float mult = 1;
            var character = CharacterBase.GetCharacter(characterId);
            if(character != null) { mult = character.GoldMultiplier; }
            Gold += (ulong)Math.Ceiling(gold * mult);
        }

        public void AddExperience(long exp) {
            var character = CharacterBase.GetCharacter(characterId);
            if (character == null) { return; }
            if (Experience == null) { Experience = new ulong[0]; }
            if (Experience.Length <= character.Id) {
                var characters = CharacterBase.Characters;
                var newExp = new ulong[characters.Count()];
                for (var i = 0; i < Experience.Length; i++) {
                    newExp[i] = Experience[i];
                }
                Experience = newExp;
            }
            Experience[characterId] = (ulong)Math.Max(0, (long)Experience[characterId] + exp);
        }

        public void Death(long deathExp) {
            AddExperience(deathExp);
            CurrentMercenaries = 0;
            characterId = -1;
        }

        public void Victory(long expReceived, ulong goldReceived) {
            EnemiesKilled++;
            AddGold(goldReceived);
            AddExperience(expReceived);

            if (CurrentMercenaries > 0) {
                var newMercCount = CurrentMercenaries == 1 ? 0 : Math.Max(0, this.random.Next(1, CurrentMercenaries));
                CurrentMercenaries = newMercCount;
            }
        }
    }
}