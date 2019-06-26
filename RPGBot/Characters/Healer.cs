using System;
using System.Collections.Generic;
using System.Text;

namespace RPGBot.Characters {
    // Healer has average attack, health, and gold, but gives a health bonus to the rest of the group
    public class Healer : CharacterBase {
        public override float AttackPowerMultiplier {
            get { return 1; }
        }

        public override float HealthMultiplier {
            get { return 1; }
        }

        public override float GoldMultiplier {
            get { return 1; }
        }
    }
}
