using System;

namespace RPGBot.Helpers {

    public static class RandomExtensions {

        public static float Range(this Random random, float min, float max) {
            var t = (float)random.NextDouble();
            return min + ((max - min) * t);
        }
    }
}