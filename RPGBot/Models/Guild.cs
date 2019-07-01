using System;
using System.Collections.Generic;
using System.Text;

namespace RPGBot.Models {
    public class Guild {
        public ulong Id { get; set; }
        //public Player[] Players { get; set; }

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

        public bool Update() {
            return DB.Update("guilds.db", "guilds", this);
        }
    }
}
