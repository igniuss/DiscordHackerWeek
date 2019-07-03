namespace RPGBot.Generative {

    internal class QuestGenerator : RantGenerator {
        public static QuestGenerator Instance { get; } = new QuestGenerator();

        public QuestGenerator() {
        }

        public override string RantPath {
            get {
                return "Generative/quests.rant";
            }
        }
    }
}