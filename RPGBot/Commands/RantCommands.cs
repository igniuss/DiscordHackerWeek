using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using RPGBot.Generative;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RPGBot.Commands {
    public class RantCommands : BaseCommandModule {
        [Command("rquest")]
        public async Task GetRandomQuest(CommandContext ctx, int count = 10) {
            try {

            await ctx.RespondAsync($"```{string.Join("\n", QuestGenerator.Instance.GetResults((uint)count))}```");
            } catch(System.Exception ex) {
                await ctx.RespondAsync(ex.ToString());
            }
        }
        [Command("rname")]
        public async Task GetRandomName(CommandContext ctx, int count = 10) {
            await ctx.RespondAsync($"```{string.Join("\n", NameGenerator.Instance.GetResults((uint)count))}```");
        }

        [Command("dorant")]
        public async Task DoRant(CommandContext ctx, [RemainingText] string cmd) {
            try {
                var rant = new Rant.RantEngine();
                rant.LoadPackage("Rantionary.rantpkg");

                var prog = Rant.RantProgram.CompileString(cmd);
                var output = rant.Do(prog);
                await ctx.RespondAsync($"```{string.Join("\n", output.Select(x => x.Value))}```");
            } catch (System.Exception ex) {
                await ctx.RespondAsync(ex.ToString());
            }
        }
    }
}
