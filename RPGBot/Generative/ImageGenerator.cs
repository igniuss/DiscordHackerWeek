﻿using RPGBot.Helpers;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.Primitives;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace RPGBot.Generative {

    public static class ImageGenerator {
        private const string BackgroundPath = "Assets/Backgrounds/";
        private const string MiscPath = "Assets/Misc/";

        private static DirectoryInfo BackgroundDir { get; set; } = new DirectoryInfo(BackgroundPath);
        private static DirectoryInfo MiscDir { get; set; } = new DirectoryInfo(MiscPath);

        public static string RandomBackground() {
            var background = BackgroundDir.GetFiles("*.png").Random();
            return background.FullName;
        }

        public static string Campsite(string backPath) {
            var original = CreateOrGetImage(null, backPath, 1f);
            var path = original + "_night.gif";
            if (File.Exists(path)) {
                return path;
            }

            var random = new Random();
            using (var img = Image.Load(original)) {
                //append background

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

                img.Frames.RemoveFrame(0);
                img.Save(path);
                return path;
            }
        }

        public static string CreateOrGetImage(string charPath, string backPath, float HPPercentage) {
            var charName = Path.GetFileNameWithoutExtension(charPath);
            var backName = Path.GetFileNameWithoutExtension(backPath);
            var path = Path.Combine("Output", $"{charName}_{backName}.png");
            //Make sure the file actually exists
            if (!File.Exists(path)) {
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
                }
            }

            var transparency = 1f - HPPercentage;
            var rounded = (float)Math.Round(transparency * 4f) / 4f; //Round this to 1/4ths, we don't need more precision than this.
            if (rounded == 0f) {
                return path;
            }
            return SimulateDamage(path, HPPercentage);
        }

        public static string SimulateDamage(string path, float percentage) {
            var newPath = $"{path}_{percentage.ToString("0.00")}.png";
            if (File.Exists(newPath)) {
                return newPath;
            }
            using (var img = Image.Load(path)) {
                var options = new GraphicsOptions() {
                    BlendPercentage = percentage,
                };
                img.Mutate(x => x.Vignette(options, Rgba32.DarkRed));
                var savePath = newPath;
                img.Save(savePath);
                return savePath;
            }
        }

        public static async Task<string> GetImageURL(string path) {
            var name = Path.GetFileNameWithoutExtension(path);
            var cacheName = Path.Combine("Cache", $"{name}.cache");
            if (File.Exists(cacheName)) {
                return File.ReadAllText(cacheName);
            }
            try {
                //Add an extra wait?
                await Task.Delay(500);
                var msg = await Bot.ImageCache.SendFileAsync(path);
                var url = msg.Attachments.First().Url;
                File.WriteAllText(cacheName, url);
                return url;
            } catch (Exception ex) {
                Console.WriteLine(ex);
                await Task.Delay(1500);
                return await GetImageURL(path);
            }
        }
    }
}