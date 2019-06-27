namespace RPGBot.Actions {

    public class Flee : ActionBase {

        public override int Id {
            get {
                return 2;
            }
        }

        public override string Emoji {
            get {
                return ":runner:";
            }
        }
    }
}