using System;
using System.Collections.Generic;
using System.Text;
using DSharpPlus.Entities;
using NLog;
using NLog.Config;
using NLog.Targets;

namespace RPGBot {
    [Target("DiscordChannel")]
    class DiscordChannelTarget : TargetWithLayout {
        [RequiredParameter]
        public DiscordChannel Channel { get; }
        public DiscordChannelTarget(DiscordChannel channel) {
            Channel = channel;
        }
        protected override void Write(LogEventInfo logEvent) {
            var msg = this.Layout.Render(logEvent);
            if(Channel != null) {
                var embed = Bot.GetDefaultEmbed()
                    .WithDescription(msg)
                    .WithColor(DiscordColor.Red);
                Channel.SendFileAsync(msg);
            }

        }
    }
}
