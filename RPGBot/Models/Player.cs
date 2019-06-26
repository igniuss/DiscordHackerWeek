using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DSharpPlus.Entities;
using RPGBot.Characters;

namespace RPGBot.Models {
    public class Player {
        public ulong Id { get; set; }
        public ulong GuildId { get; set; }
        public ulong[] Experience { get; set; }
        public ulong Gold { get; set; }

        
        public DiscordUser discordUser;
        public CharacterBase character;

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
                var actions = Actions.ActionBase.GetActions();
                player.Experience = new ulong[actions.Count()];
            }

            return player;
        }

        public bool Update() {
            return DB.Update($"{GuildId}.db", "players", this);
        }

        internal double GetHP() {
            if (this.character == null) { return 0f; }
            //TODO ACTUALLY CALCULATE THIS BASED ON CLASS AND EXPERIENCE
            return 100f;
        }

        internal float GetAttack() {
            if (this.character == null) { return 0f; }
            //TODO ACTUALLY CALCULATE THIS BASED ON CLASS AND EXPERIENCE
            return 10f;
        }

        internal float GetDefense() {
            if (this.character == null) { return 0f; }
            //TODO ACTUALLY CALCULATE THIS BASED ON CLASS AND EXPERIENCE
            return 10f;
        }
    }
}
