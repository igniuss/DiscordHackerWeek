using DSharpPlus.Entities;

namespace RPGBot.Items {

    public class Mercenary : ItemBase {

        public Mercenary() {
        }

        public override int Id { get; } = 0;

        public override string Name {
            get {
                return "Mercenary";
            }
        }

        public override ulong Price {
            get {
                return 500; // Not sure how pricing will be so this will change
            }
        }

        public override string ItemDescription {
            get {
                return "Hire an extra fighter to accompany your guild. One mercenary used per enemy encounter.";
            }
        }

        public int Health { get; set; }
        public int Attack { get; set; }

        public override int Count { get; set; }

        public override DiscordEmoji GetEmoji() {
            return DiscordEmoji.FromGuildEmote(Bot.Client, 593691369185476634);
        }
    }
}