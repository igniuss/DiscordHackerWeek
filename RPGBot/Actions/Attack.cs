using DSharpPlus.Entities;

namespace RPGBot.Actions {

    public class Attack : ActionBase {

        public override int Id {
            get {
                return 0;
            }
        }
        public override DiscordEmoji GetEmoji() {
            return DiscordEmoji.FromName(Bot.Client, ":crossed_swords:");
        }
    }
}