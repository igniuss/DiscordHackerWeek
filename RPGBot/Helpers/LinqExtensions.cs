using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGBot.Helpers {

    public static class LinqExtensions {
        private static readonly Random r = new Random();

        public static T GetSafe<T>(this IList<T> list,  int index, T defaultValue) {
            // other checks omitted
            return index < 0 || index >= list.Count ? defaultValue : list[index];
        }

        public static T Random<T>(this IEnumerable<T> input) {
            return input.ElementAt(r.Next(input.Count() - 1));
        }
    }
}