using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGBot.Helpers {

    public static class LinqExtensions {
        private static readonly Random r = new Random();

        public static T Random<T>(this IEnumerable<T> input) {
            return input.ElementAt(r.Next(input.Count() - 1));
        }
    }
}