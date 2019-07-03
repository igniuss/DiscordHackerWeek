using Rant;
using System;
using System.IO;
using System.Linq;

namespace RPGBot.Generative {

    public abstract class RantGenerator {
        public abstract string RantPath { get; }
        private readonly RantEngine Engine = new RantEngine();
        private RantProgram Program;

        internal RantGenerator() {
            if (string.IsNullOrEmpty(RantPath) || !File.Exists(RantPath)) {
                throw new FileNotFoundException(RantPath);
            }
            this.Engine = new RantEngine();
            this.Engine.LoadPackage("Rantionary.rantpkg");
            Reload();
        }

        public void Reload() {
            this.Program = RantProgram.CompileFile(RantPath);
        }

        public string GetResult() {
            return string.Join(" ", this.Engine.Do(this.Program).Select(x => x.Value));
            throw new NotImplementedException();
        }

        public string[] GetResults(uint count) {
            var results = new string[count];
            for (var i = 0; i < count; i++) {
                results[i] = GetResult();
            }
            return results;
        }
    }
}