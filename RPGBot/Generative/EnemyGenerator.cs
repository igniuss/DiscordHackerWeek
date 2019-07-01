using RPGBot.Helpers;
using System.IO;

namespace RPGBot.Generative {

    public static class EnemyGenerator {
        private static DirectoryInfo CharacterDir { get; set; }
        private static DirectoryInfo BossDir { get; set; }
        private const string CharacterPath = "Assets/Characters/";
        private const string BossPath = "Assets/Boss/";

        public static string RandomEnemy(bool boss = false) {
            if (CharacterDir == null) { CharacterDir = new DirectoryInfo(CharacterPath); }
            if (BossDir == null) { BossDir = new DirectoryInfo(BossPath); }

            return boss ? BossDir.GetFiles("*.png").Random().FullName : CharacterDir.GetFiles("*.png").Random().FullName;
        }
    }
}