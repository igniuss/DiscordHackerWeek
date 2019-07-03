using RPGBot.Helpers;
using System.IO;

namespace RPGBot.Generative {

    public static class EnemyGenerator {
        private static DirectoryInfo CharacterDir { get; set; } = new DirectoryInfo(CharacterPath);
        private static DirectoryInfo BossDir { get; set; } = new DirectoryInfo(BossPath);
        private const string CharacterPath = "Assets/Characters/";
        private const string BossPath = "Assets/Boss/";

        public static string RandomEnemy(bool boss = false) {
            return boss ? BossDir.GetFiles("*.png").Random().FullName : CharacterDir.GetFiles("*.png").Random().FullName;
        }
    }
}