﻿using DSharpPlus.Entities;
using RPGBot.Characters;
using RPGBot.Helpers;
using RPGBot.Items;
using System;
using System.Collections.Generic;
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

        public CharacterBase character;
        //public DiscordUser discordUser;

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

            return lvl * _base * this.character.HealthMultiplier * ((CurrentMercenaries + 1) * 0.5f);
        }

        public float GetAttack() {
            if (this.character == null) { return 0f; }
            var random = new Random();
            var lvl = GetCurrentLevel();
            var _base = 5;
            var crit = random.Next(1, 6) * 0.75f;

            return lvl * _base * this.character.AttackPowerMultiplier * crit * ((CurrentMercenaries + 1) * 0.5f);
        }

        public float GetDefense() {
            if (this.character == null) { return 0f; }
            var random = new Random();
            var lvl = GetCurrentLevel();
            var _base = 5;
            var crit = random.Next(1, 6) * 0.75f;
            return lvl * _base * this.character.HealthMultiplier * crit * ((CurrentMercenaries + 1) * 0.5f);
        }

        public void IncreaseGold(float gold) {
            if (this.character == null) { return; }
            Gold += (ulong)Math.Ceiling(gold * this.character.GoldMultiplier);
        }

        public void IncreaseExperience(long exp) {
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
            Experience[this.character.Id] = (ulong)Math.Max(0, (long)Experience[this.character.Id] + exp);
        }

        internal void Death(long deathExp) {
            IncreaseExperience(deathExp);
            CurrentMercenaries = 0;
            character = null;
        }

        internal void Victory(long expeReceived, ulong goldReceived) {
            var random = new Random();
            EnemiesKilled++;
            IncreaseGold(goldReceived);
            IncreaseExperience(expeReceived);
            var newMercCount = Math.Max(0, random.Next(1, CurrentMercenaries));
            CurrentMercenaries = newMercCount;
        }
    }
}