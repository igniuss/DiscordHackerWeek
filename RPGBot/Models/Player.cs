using System;
using System.Collections.Generic;
using System.Text;

namespace RPGBot.Models {
    public class Player {
        public ulong Id { get; }
        public ulong[] Experience { get; set; }
        public ulong Gold { get; set; }

        public Player(ulong id, ulong[] experience, ulong gold) {
            Id = id;
            Experience = experience;
            Gold = gold;
        }

        public Player(ulong id) {
            Id = id;
            Gold = 0;
            Experience = new ulong[] { 0, 0, 0, 0 };
        }

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
