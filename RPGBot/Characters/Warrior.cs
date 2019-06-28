using DSharpPlus.Entities;

namespace RPGBot.Characters {

    // Warrior has an attack bonus, but average health and gold earned
    public class Warrior : CharacterBase {

        public override float AttackPowerMultiplier {
            get { return 1.5f; }
        }

        public override float HealthMultiplier {
            get { return 1; }
        }

        public override float GoldMultiplier {
            get { return 1; }
        }

        public override DiscordEmoji GetEmoji() {
            return DiscordEmoji.FromName(Bot.Client, Emoji);
        }

        public string Emoji {
            get {
                return ":crossed_swords:";
            }
        }

        public override int Id {
            get {
                return 0;
            }
        }
    }
}