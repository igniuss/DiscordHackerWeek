namespace RPGBot.Actions {

    public class Defend : ActionBase {

        public override int Id {
            get {
                return 1;
            }
        }

        public override string Emoji {
            get {
                return ":shield:";
            }
        }
    }
}