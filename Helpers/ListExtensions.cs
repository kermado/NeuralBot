using System.Collections.Generic;

namespace AimBot.Helpers
{
    public static class ListExtensions
    {
        public static T RemoveFast<T>(this List<T> list, int index)
        {
            var last = list.Count - 1;
            var item = list[index];
            list[index] = list[last];
            list.RemoveAt(last);
            return item;
        }
    }
}
