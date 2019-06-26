using System;
using System.Collections.Generic;
using System.Text;

namespace RPGBot.Characters {
    // Knight has average attack and gold earned, but gets a health bonus from its armor
    public class Knight : CharacterBase {
        public override float AttackPowerMultiplier {
            get { return 1; }
        }

        public override float HealthMultiplier {
            get { return 1.5f; }
        }

        public override float GoldMultiplier {
            get { return 1; }
        }
    }
}
