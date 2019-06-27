using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RPGBot.Items {
    public abstract class ItemBase {
        public ItemBase() { }
        public abstract int Id { get; }
        public abstract string Name { get; }
        public abstract ulong Price { get; }
        public abstract string ItemDescription { get; }
        public abstract int Count { get; set; }

        public DiscordEmoji emoji { get; set; }

        public static IEnumerable<ItemBase> GetAllItems() {
            var items = typeof(ItemBase).Assembly.GetTypes()
                .Where(t => t.IsSubclassOf(typeof(ItemBase)) && !t.IsAbstract)
                .Select(t => (ItemBase)Activator.CreateInstance(t));
            return items;
        }

        public abstract DiscordEmoji GetEmoji();

        public override string ToString() {
            return $"{GetEmoji()} {Name}: {Price} :moneybag:\n" +
                $"``{ItemDescription}``";
        }
    }
}
