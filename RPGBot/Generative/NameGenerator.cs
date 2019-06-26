namespace RPGBot.Generative {

    public class NamesGenerator : RantGenerator {
        public static NamesGenerator Instance { get; } = new NamesGenerator();

        public override string RantPath {
            get {
                return "Generative/names.rant";
            }
        }
    }
}