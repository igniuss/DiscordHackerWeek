using DSharpPlus.Entities;

namespace RPGBot.Characters {

    // CharacterBase will be used by all player classes
    // The multipliers will determine the attack, health, and gold earned for each class
    public abstract class CharacterBase {
        public abstract int Id { get; }
        public abstract float AttackPowerMultiplier { get; }
        public abstract float HealthMultiplier { get; }
        public abstract float GoldMultiplier { get; }
        public abstract string Emoji { get; }
        private DiscordEmoji _Emoji;

        public DiscordEmoji GetEmoji() {
            if (this._Emoji == null) {
                this._Emoji = DiscordEmoji.FromName(Bot.Client, Emoji);
            }
            return this._Emoji;
        }
    }
}