using System;
using System.Collections.Generic;
using System.Text;

namespace RPGBot.Items {
    public class Mercenary : ItemBase {
        public override string Name {
            get {
                return "Mercenary";
            }
        }
        public override ulong Price {
            get {
                return 20; // Not sure how pricing will be so this will change
            }
        }
        public override ulong EmojiId {
            get {
                return 593691369185476634;
            }
        }
        public override string ItemDescription {
            get {
                return "Hire an extra fighter to accompany your guild. One mercenary used per enemy encounter.";
            }
        }
        public int Health { get; set; }
        public int Attack { get; set; }
    }
}
