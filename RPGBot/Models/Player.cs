using DSharpPlus.Entities;
using RPGBot.Characters;
using RPGBot.Helpers;
using System;
using System.Linq;

namespace RPGBot.Models {

    public class Player {
        public ulong Id { get; set; }
        public ulong GuildId { get; set; }
        public ulong[] Experience { get; set; }
        public ulong Gold { get; set; }
        public int EnemiesKilled { get; set; }
        public int TotalQuests { get; set; }
        public int SuccessfulQuests { get; set; }
        public int MercenariesHired { get; set; }

        public CharacterBase character;
        public DiscordUser discordUser;

        public Player() { }

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
                var characters = Characters.CharacterBase.GetAllCharacters();
                player.Experience = new ulong[characters.Count()];
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
            return this.character == null ? 0 : Experience.GetSafe<ulong>(this.character.Id, 0);
        }

        public int GetCurrentLevel() {
            if (this.character == null) { return 0; }
            var exp = GetCurrentExp();
            return CalculateLevel(exp);
        }

        public float GetHP() {
            if (this.character == null) { return 0f; }
            var lvl = GetCurrentLevel();
            var _base = 100;

            return lvl * _base * this.character.HealthMultiplier;
        }

        public float GetAttack() {
            if (this.character == null) { return 0f; }
            var lvl = GetCurrentLevel();
            var _base = 5;

            return lvl * _base * this.character.AttackPowerMultiplier;
        }

        public float GetDefense() {
            if (this.character == null) { return 0f; }
            var lvl = GetCurrentLevel();
            var _base = 5;

            return lvl * _base * this.character.HealthMultiplier;
        }

        public void IncreaseGold(float gold) {
            if (this.character == null) { return; }
            Gold += (ulong)Math.Ceiling(gold * this.character.GoldMultiplier);
        }

        public void IncreaseExperience(ulong exp) {
            if (this.character == null) { return; }
            if (Experience == null) { Experience = new ulong[0]; }
            if (Experience.Length <= this.character.Id) {
                var characters = CharacterBase.GetAllCharacters();
                var newExp = new ulong[characters.Count()];
                for (var i = 0; i < Experience.Length; i++) {
                    newExp[i] = Experience[i];
                }
                Experience = newExp;
            }
            Experience[this.character.Id] += exp;
        }
    }
}