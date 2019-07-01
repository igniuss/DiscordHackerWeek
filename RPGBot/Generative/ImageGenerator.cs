using RPGBot.Helpers;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.Primitives;
using System;
using System.IO;

namespace RPGBot.Generative {

    public class ImageGenerator {
        private const string BackgroundPath = "Assets/Backgrounds/";
        private const string MiscPath = "Assets/Misc/";

        private DirectoryInfo BackgroundDir { get; set; }
        private DirectoryInfo MiscDir { get; set; }

        public ImageGenerator() {
            BackgroundDir = new DirectoryInfo(BackgroundPath);
            MiscDir = new DirectoryInfo(MiscPath);
        }

        public string RandomBackground() {
            var background = BackgroundDir.GetFiles("*.png").Random();
            return background.FullName;
        }

        public string NightTime(string backPath) {
            var randomPath = Path.Combine("Output", $"{Guid.NewGuid()}.gif");
            var info = new FileInfo(randomPath);
            if (!info.Directory.Exists) {
                info.Directory.Create();
            }

            var random = new Random();

            using (var img = new Image<Rgba32>(Configuration.Default, 720, 720, Rgba32.White)) {
                //append background
                using (var bImg = Image.Load(backPath)) {
                    var width = img.Width;
                    var aspect = 1f / (bImg.Width / bImg.Height);
                    if (bImg.Width < img.Width || bImg.Height < img.Height) {
                        bImg.Mutate(x => x.Resize(0, img.Height));
                    }
                    img.Mutate(x => x.DrawImage(bImg, 1));
                    var bonfireFrames = MiscDir.GetFiles("Bonfire/*");
                    img.Mutate(x => x.Brightness(0.3f));

                    for (var i = 0; i < bonfireFrames.Length; i++) {
                        var frame = bonfireFrames[i];
                        using (var f = Image.Load(frame.FullName)) {
                            var clone = img.Clone();
                            var options = new GraphicsOptions {
                                BlendPercentage = 0.75f,
                            };

                            clone.Mutate(x => x.Vignette(options, Rgba32.Black));
                            var center = new Point(img.Width / 2, img.Height / 2);
                            f.Mutate(x => x.Resize(f.Width * 3, f.Height * 3));
                            center.Offset(-f.Width / 2, f.Height / 2);
                            clone.Mutate(x => x.DrawImage(f, center, 1));
                            clone.Mutate(x => x.GaussianBlur(2.5f));
                            img.Frames.AddFrame(clone.Frames[0]);
                        }
                    }
                }
                img.Frames.RemoveFrame(0);

                img.Save(randomPath);
                return randomPath;
            }
        }

        public string CreateImage(string charPath, string backPath) {
            var path = Path.Combine("Output", $"{charPath}_{backPath}.png");
            if (File.Exists(path)) { return path; }
            var info = new FileInfo(path);
            if (!info.Directory.Exists) {
                info.Directory.Create();
            }

            var random = new Random();

            using (var img = new Image<Rgba32>(Configuration.Default, 720, 720, Rgba32.White)) {
                //append background
                using (var bImg = Image.Load(backPath)) {
                    var width = img.Width;
                    var aspect = 1f / (bImg.Width / bImg.Height);
                    if (bImg.Width < img.Width || bImg.Height < img.Height) {
                        bImg.Mutate(x => x.Resize(0, img.Height));
                    }
                    img.Mutate(x => x.DrawImage(bImg, 1));
                }

                if (!string.IsNullOrEmpty(charPath)) {
                    using (var cImg = Image.Load(charPath)) {
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
                img.Save(path);
                return path;
            }
        }

        public string SimulateDamage(string path, float percentage) {
            using (var img = Image.Load(path)) {
                var options = new GraphicsOptions() {
                    BlendPercentage = percentage,
                };
                img.Mutate(x => x.Vignette(options, Rgba32.DarkRed));
                var randomPath = Path.Combine("Output", $"{Guid.NewGuid()}.png");
                img.Save(randomPath);
                return randomPath;
            }
        }
    }
}