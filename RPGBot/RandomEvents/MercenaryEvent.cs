using RPGBot.Generative;
using RPGBot.Models;
using System;
using System.Threading.Tasks;

namespace RPGBot.RandomEvents {

    public class MercenaryEvent : RandomEvent {
        public override int Id { get { return 0; } }
        public override string Description { get { return "You found a group of lone adventurers."; } }

        private readonly Random random = new Random();

        public override async Task<EventData> DoEvent(Quest quest) {
            var players = Player.GetPlayers(quest.Channel.GuildId, quest.UserIds);
            var count = this.random.Next(2, 20);
            foreach (var player in players) {
                player.CurrentMercenaries += count;
                player.Update();
            }

            var imgPath = ImageGenerator.Campsite(ImageGenerator.CreateOrGetImage(null, quest.BackgroundPath, 1f));
            var ret = new EventData {
                Url = await ImageGenerator.GetImageURL(imgPath),
                Message = $"{Description}\nThey joined your party\nEveryone gained {count} mercenaries",
            };
            return ret;
        }
    }
}