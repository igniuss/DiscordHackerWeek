using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGBot.Actions {

    public abstract class ActionBase {
        public abstract int Id { get; }
        public abstract string Emoji { get; }
        private DiscordEmoji emoji;

        public DiscordEmoji GetEmoji() {
            if (this.emoji == null) {
                this.emoji = DiscordEmoji.FromName(Bot.Client, Emoji);
            }
            return this.emoji;
        }
        public static IEnumerable<ActionBase> GetAllActions() {
            var actions = typeof(Actions.ActionBase).Assembly.GetTypes()
                .Where(t => t.IsSubclassOf(typeof(Actions.ActionBase)) && !t.IsAbstract)
                .Select(t => (Actions.ActionBase)Activator.CreateInstance(t));
            return actions;
        }
    }
}