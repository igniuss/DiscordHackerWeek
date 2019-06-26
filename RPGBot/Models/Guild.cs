using System;
using System.Collections.Generic;
using System.Text;

namespace RPGBot.Models {
    public class Guild {
        ulong Id { get; }
        Player[] Players { get; set; }

        public Guild(ulong id, Player[] players) {
            Id = id;
            Players = players;
        }

        public Guild(ulong id) {
            Id = id;
        }

        public Guild GetGuild(ulong id) {
            var guild = DB.FindOne<Guild>("guilds.db", "guilds", x => x.Id == id);
            if(guild == null) {
                guild = new Guild(id);
                DB.Insert("guilds.db", "guilds", guild);
            }
            return guild;
        }

        public static bool UpdateGuild(ulong guildId, Guild guild) {
            var success = DB.Update("guilds.db", "guilds", guild);
            return success;
        }
    }
}
