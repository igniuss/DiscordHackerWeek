using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RPGBot.RandomEvents {

    public abstract class RandomEvent {
        public class EventData {
            public string Message { get; set; }
            public string Url { get; set; }
        }
        public abstract int Id { get; }
        public abstract string Description { get; }

        public abstract Task<EventData> DoEvent(Quest quest);

        private static IEnumerable<RandomEvent> events = null;

        public static IEnumerable<RandomEvent> Events {
            get {
                if (events == null) {
                    events = FindAllEvents();
                }
                return events;
            }
        }

        private static IEnumerable<RandomEvent> FindAllEvents() {
            var events = typeof(RandomEvent).Assembly.GetTypes()
                .Where(t => t.IsSubclassOf(typeof(RandomEvent)) && !t.IsAbstract)
                .Select(t => (RandomEvent)Activator.CreateInstance(t));
            return events;
        }
    }
}