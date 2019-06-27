using DSharpPlus.Entities;

namespace RPGBot.Actions {

    public class Flee : ActionBase {

        public override int Id {
            get {
                return 2;
            }
        }
        public override DiscordEmoji GetEmoji() {
            return DiscordEmoji.FromName(Bot.Client, ":runner:");
        }
    }
}