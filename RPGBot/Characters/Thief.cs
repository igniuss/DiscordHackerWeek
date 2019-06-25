using System;
using System.Collections.Generic;
using System.Text;

namespace RPGBot.Characters {
    // Thief has average attack and health, but gets a bonus on gold earned
    public class Thief : CharacterBase {
        public override float AttackPowerMultiplier {
            get { return 1; }
        }

        public override float HealthMultiplier {
            get { return 1; }
        }

        public override float GoldMultiplier {
            get { return 1.5f; }
        }
    }
}
