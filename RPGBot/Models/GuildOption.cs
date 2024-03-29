﻿using DSharpPlus.Entities;
using System.Linq;

namespace RPGBot.Models {

    public class GuildOption {
        public const string DBName = "guildoptions.db";
        public const string TableName = "options";

        public ulong Id { get; set; }
        public string Prefix { get; set; }
        public ulong Channel { get; set; }
        public ulong RoleId { get; set; }
        private DiscordChannel channel;

        public DiscordChannel GetChannel() {
            if (Bot.Client.Guilds.Any(x => x.Key == Id)) {
                if (this.channel == null || this.channel.Id != Channel) {
                    if (Channel == 0) {
                        this.channel = Bot.Client.Guilds[Id].GetDefaultChannel();
                        Channel = this.channel.Id;
                    } else {
                        this.channel = Bot.Client.Guilds[Id].GetChannel(Channel);
                    }
                }
                return this.channel;
            }
            return null;
        }
    }
}