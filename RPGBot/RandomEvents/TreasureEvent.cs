using RPGBot.Helpers;
using RPGBot.Models;
using System;
using System.Threading.Tasks;

namespace RPGBot.RandomEvents {

    internal class TreasureEvent : RandomEvent {

        public override int Id {
            get {
                return 2;
            }
        }

        public override string Description {
            get {
                return "You come along a Chest!";
            }
        }

        private readonly Random random = new Random();

        public async override Task<EventData> DoEvent(Quest quest) {
            var players = Player.GetPlayers(quest.Channel.GuildId, quest.UserIds);
            var gold = (ulong)Math.Round(this.random.Range(10f, 1500f));
            foreach (var player in players) {
                player.AddGold(gold);
                player.Update();
                await Task.Delay(50);
            }
            var eventData = new EventData {
                Message = $"{Description}\nYou've decided to split the gold.\nEveryone received {gold} gold.",
                Url = null,
            };
            return eventData;
        }
    }
}