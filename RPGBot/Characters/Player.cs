using System;
using System.Collections.Generic;
using System.Text;

namespace RPGBot.Characters {
    public class Player {
        // Discord user ID of the Player
        public ulong Id { get; }
        // Character class xp. This will be one ulong per character class
        public ulong[] Experience { get; }
        public ulong Gold { get; set; }
    }
}
