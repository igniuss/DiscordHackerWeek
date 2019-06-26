using System;
using System.Collections.Generic;
using System.Text;

namespace RPGBot.Characters {
    // CharacterBase will be used by all player classes
    // The multipliers will determine the attack, health, and gold earned for each class
    public abstract class CharacterBase {
        public abstract float AttackPowerMultiplier { get; }
        public abstract float HealthMultiplier { get; }
        public abstract float GoldMultiplier { get; }
    }
}
