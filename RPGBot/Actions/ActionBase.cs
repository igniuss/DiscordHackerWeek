using DSharpPlus.Entities;

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
    }
}