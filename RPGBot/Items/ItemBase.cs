using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RPGBot.Items {
    public abstract class ItemBase {
        public abstract string Name { get; }
        public abstract ulong Price { get; }
        public abstract ulong EmojiId { get; }
        public abstract string ItemDescription { get; }
        private DiscordEmoji emoji;

        public DiscordEmoji GetEmoji() {
            if (this.emoji == null) {
                this.emoji = DiscordEmoji.FromGuildEmote(Bot.Client, EmojiId);
            }
            return this.emoji;
        }

        public static IEnumerable<ItemBase> GetAllItems() {
            var items = typeof(ItemBase).Assembly.GetTypes()
                .Where(t => t.IsSubclassOf(typeof(ItemBase)) && !t.IsAbstract)
                .Select(t => (ItemBase)Activator.CreateInstance(t));
            return items;
        }

        public override string ToString() {
            return $"{GetEmoji()} {Name}: {Price} :moneybag:\n" +
                $"``{ItemDescription}``";
        }
    }
}
