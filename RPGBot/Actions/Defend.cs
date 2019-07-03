using DSharpPlus.Entities;

namespace RPGBot.Actions {

    public class Defend : ActionBase {

        public override int Id {
            get {
                return 1;
            }
        }

        public override DiscordEmoji GetEmoji() {
            return DiscordEmoji.FromName(Bot.Client, ":shield:");
        }
    }
}