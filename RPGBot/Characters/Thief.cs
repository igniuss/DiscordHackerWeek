using DSharpPlus.Entities;

namespace RPGBot.Characters {

    // Thief has average attack and health, but gets a bonus on gold earned
    public class Thief : CharacterBase {


        public override float AttackPowerMultiplier {
            get { return 1f; }
        }

        public override float DefenseMultiplier {
            get { return 1f; }
        }

        public override float HealthMultiplier {
            get { return 1f; }
        }

        public override float GoldMultiplier {
            get { return 1.5f; }
        }

        public override DiscordEmoji GetEmoji() {
            return DiscordEmoji.FromName(Bot.Client, Emoji);
        }

        public string Emoji {
            get {
                return ":dagger:";
            }
        }

        public override int Id {
            get {
                return 3;
            }
        }
    }
}