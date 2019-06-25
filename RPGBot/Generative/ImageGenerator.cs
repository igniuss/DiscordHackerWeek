using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using RPGBot.Helpers;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Advanced;
using SixLabors.Primitives;

namespace RPGBot.Generative {
    public class ImageGenerator {
        private const string CharacterPath = "Assets/Characters/";
        private const string BackgroundPath = "Assets/Backgrounds/";
        private const string MiscPath = "Assets/Misc/";

        private DirectoryInfo CharacterDir { get; set; }
        private DirectoryInfo BackgroundDir { get; set; }
        private DirectoryInfo MiscDir { get; set; }

        public ImageGenerator() {
            CharacterDir = new DirectoryInfo(CharacterPath);
            BackgroundDir = new DirectoryInfo(BackgroundPath);
            MiscDir = new DirectoryInfo(MiscPath);
        }

        public string RandomBackground() {
            var background = BackgroundDir.GetFiles("*.png").Random();
            return background.FullName;
        }

        public string RandomCharacter() {
            var character = CharacterDir.GetFiles("*.png").Random();
            return character.FullName;
        }

        public string RandomFromSearch(string charSearch = null, string backSearch = null) {
            var cSearch = "*.png";
            if (!string.IsNullOrEmpty(charSearch)) {
                cSearch = charSearch;
            }

            var bSearch = "*.png";
            if (!string.IsNullOrEmpty(backSearch)) {
                bSearch = backSearch;
            }

            var character = CharacterDir.GetFiles(cSearch).Random();
            var background = BackgroundDir.GetFiles(bSearch).Random();

            return CreateImage(character.FullName, background.FullName);
        }

        public string CreateImage(string charPath, string backPath) {
            var randomPath = Path.Combine("Output", $"{Guid.NewGuid()}.png");
            var info = new FileInfo(randomPath);
            if (!info.Directory.Exists) {
                info.Directory.Create();
            }

            var random = new Random();

            using (var img = new Image<Rgba32>(Configuration.Default, 512, 512, Rgba32.White)) {
                //append background
                using (var bImg = Image.Load(backPath)) {
                    var width = img.Width;
                    var aspect = 1f / (bImg.Width / bImg.Height);
                    bImg.Mutate(x => x.Resize(width, bImg.Height * (int)aspect));
                    img.Mutate(x => x.DrawImage(bImg, 1));
                }

                if (!string.IsNullOrEmpty(charPath)) {
                    using (var cImg = Image.Load(charPath)) {
                        //resize the character
                        cImg.Mutate(x => x.Resize((int)(cImg.Width * 0.7), (int)(cImg.Height * 0.7)));
                        //set the 'center'
                        var point = new Point((img.Width / 2) - (cImg.Width / 2), (img.Height / 2) - (cImg.Height / 2));
                        //generate a little random offset
                        var rX = 0;// random.Next(-img.Width / 4, img.Width / 4);
                        var rY = random.Next(-img.Height / 8, img.Height / 12);
                        point.Offset(rX, rY);
                        //append the character ontop of the existing image
                        img.Mutate(x => x.DrawImage(cImg, point, 1));
                    }
                }
                img.Save(randomPath);
                return randomPath;
            }
        }
    }
}
