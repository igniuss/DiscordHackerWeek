using Rant;
using System;

namespace RPGBot.Generative {
    public class NameGenerator : RantGenerator {
        public static NameGenerator Instance { get; } = new NameGenerator();

        public override string RantPath {
            get {
                return "Generative/names.rant";
            }
        }

    }
}
