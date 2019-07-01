using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace RPGBot.Characters {

    // CharacterBase will be used by all player classes
    // The multipliers will determine the attack, health, and gold earned for each class
    public abstract class CharacterBase {
        public abstract int Id { get; }
        public abstract float AttackPowerMultiplier { get; }
        public abstract float DefenseMultiplier { get; }
        public abstract float HealthMultiplier { get; }
        public abstract float GoldMultiplier { get; }
        public abstract DiscordEmoji GetEmoji();

        public static ReadOnlyCollection<CharacterBase> Characters { get {
                if (characters == null) {
                    characters = typeof(CharacterBase).Assembly.GetTypes()
                    .Where(t => t.IsSubclassOf(typeof(CharacterBase)) && !t.IsAbstract)
                    .Select(t => (CharacterBase)Activator.CreateInstance(t))
                    .ToList()
                    .AsReadOnly();
                }
                return characters;
            }
        }

        private static ReadOnlyCollection<CharacterBase> characters = null;

        public static CharacterBase GetCharacter(int characterId) {
            foreach(var character in characters) {
                if(character.Id == characterId) { return character; }
            }
            return null;
        }
    }
}