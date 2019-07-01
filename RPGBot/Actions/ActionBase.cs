using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGBot.Actions {

    public abstract class ActionBase {
        public abstract int Id { get; }

        public abstract DiscordEmoji GetEmoji();

        public static IEnumerable<ActionBase> GetAllActions() {
            var actions = typeof(Actions.ActionBase).Assembly.GetTypes()
                .Where(t => t.IsSubclassOf(typeof(Actions.ActionBase)) && !t.IsAbstract)
                .Select(t => (Actions.ActionBase)Activator.CreateInstance(t));
            return actions;
        }

    }
}