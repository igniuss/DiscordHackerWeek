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
        [Command("rreload")]
        public async Task ReloadRant(CommandContext ctx) {
            //Let's find all the types
            try {
                var types = System.Reflection.Assembly.GetAssembly(typeof(RantGenerator))
                    .GetTypes()
                    .Where(x => x.IsClass && !x.IsAbstract && x.IsSubclassOf(typeof(RantGenerator)));
                foreach (var type in types) {
                    var instance = (RantGenerator)type.GetField("Instance").GetValue(null);
                    instance.Reload();
                }
            } catch (System.Exception ex) {
                Console.WriteLine($"[ERROR] {ex}");
                await ctx.RespondAsync($"```csharp\n[ERROR]\n{ex}```");
                return;
            }
            await ctx.RespondAsync($"👌👌");
        }
    }
}
