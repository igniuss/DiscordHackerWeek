using DSharpPlus.Entities;

namespace RPGBot.Characters {

    public class Player {

        public Player(DiscordUser user, CharacterBase character = null) {
            Id = user.Id;
            discordUser = user;
            Character = character;
        }

        public Player() {
        }

        public DiscordUser discordUser { get; private set; }

        // Discord user ID of the Player
        public ulong Id { get; set; }

        // Character class xp. This will be one ulong per character class
        public ulong[] Experience { get; set; }

        public ulong Gold { get; set; }

        public CharacterBase Character { get; private set; }

        public int GetLevel(int id) {
            return 1;
        }

        internal double GetHP() {
            return 100f;
        }

        internal float GetAttack() {
            return 10f;
        }

        internal float GetDefense() {
            return 10f;
        }
    }
}