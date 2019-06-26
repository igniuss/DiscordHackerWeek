using RPGBot.Characters;
using System;
using System.Collections.Generic;
using System.Text;

namespace RPGBot.Models {
    public class Player {
        public ulong Id { get; set; }
        public ulong[] Experience { get; set; }
        public ulong Gold { get; set; }
        public CharacterBase CurrentCharacter;
        public int EnemiesKilled { get; set; }
        public int TotalQuests { get; set; }
        public int SuccessfulQuests { get; set; }
        public int MercenariesHired { get; set; }

        public Player(ulong id, ulong[] experience, ulong gold, CharacterBase currentCharacter, int enemiesKilled, int totalQuests, int successfulQuests, int mercenariesHired) : this(id) {
            Experience = experience;
            Gold = gold;
            this.CurrentCharacter = currentCharacter;
            EnemiesKilled = enemiesKilled;
            TotalQuests = totalQuests;
            SuccessfulQuests = successfulQuests;
            MercenariesHired = mercenariesHired;
        }

        public Player(ulong id) {
            Id = id;
            Gold = 0;
            Experience = new ulong[] { 0, 0, 0, 0 };
            EnemiesKilled = 0;
            TotalQuests = 0;
            SuccessfulQuests = 0;
            MercenariesHired = 0;
        }

        public Player() { }

        public static Player GetPlayer(ulong guildId, ulong id) {
            var player = DB.FindOne<Player>($"{guildId}.db", "players", x => x.Id == id);
            if(player == null) {
                player = new Player(id);
                DB.Insert($"{guildId}.db", "players", player);
            }
            return player;
        }

        public static bool UpdatePlayer(ulong guildId, Player player) {
            var success = DB.Update($"{guildId}.db", "players", player);
            return success;
        }
    }
}
