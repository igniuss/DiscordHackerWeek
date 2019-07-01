using RPGBot.Generative;
using System;
using System.Threading.Tasks;

namespace RPGBot.RandomEvents {

    public class HealEvent : RandomEvent {

        public override int Id {
            get {
                return 1;
            }
        }

        public override string Description {
            get {
                return "Your team sets up a camp to rest.";
            }
        }

        public async override Task<EventData> DoEvent(Quest quest) {
            var campPath = ImageGenerator.Campsite(quest.BackgroundPath);
            var healed = quest.MaxHP * 0.25f;
            quest.CurrentHP = Math.Min(quest.CurrentHP + healed, quest.MaxHP);
            var eventData = new EventData {
                Message = $"{Description}\nHealed {healed.ToString("0.00")}HP",
                Url = await ImageGenerator.GetImageURL(campPath)
            };

            return eventData;
        }
    }
}