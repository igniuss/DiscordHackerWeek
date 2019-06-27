using DSharpPlus.Entities;

namespace RPGBot.Characters {

    // Healer has average attack, health, and gold, but gives a health bonus to the rest of the group
    public class Healer : CharacterBase {

        public override float AttackPowerMultiplier {
            get { return 1.25f; }
        }

        public override float HealthMultiplier {
            get { return 1.25f; }
        }

        public override float GoldMultiplier {
            get { return 1.25f; }
        }

        public override DiscordEmoji GetEmoji() {
            return DiscordEmoji.FromName(Bot.Client, Emoji);
        }

        public string Emoji {
            get {
                return ":pill:";
            }
        }

        public override int Id {
            get {
                return 2;
            }
        }
    }
}