using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace Light.Utilities
{
    public enum DropPosition
    {
        Before,
        After
    }
    public static class DragHelper
    {
        static Dictionary<string, object> objects = new Dictionary<string, object>();
        static Random random = new Random();
        private static string RandomToken()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            for (;;)
            {
                var str =
                    new string(Enumerable.Repeat(chars, 10)
                    .Select(s => s[random.Next(s.Length)]).ToArray());
                if (!objects.ContainsKey(str))
                    return str;
            }
        }

        public static string Add(object o)
        {
            lock (objects)
            {
                var token = RandomToken();
                objects.Add(token, o);
                return token;
            }
        }

        public static object Get(string token)
        {
            lock (objects)
            {
                object ret;
                if (objects.TryGetValue(token, out ret))
                {
                    return ret;
                }
                return null;
            }
        }

        public static object Take(string token)
        {
            lock (objects)
            {
                if (objects.TryGetValue(token, out object ret))
                {
                    objects.Remove(token);
                    return ret;
                }
                return null;
            }
        }

        public static void Remove(string token)
        {
            lock (objects)
            {
                if (objects.ContainsKey(token))
                {
                    objects.Remove(token);
                }
            }
        }

        public static void Remove(object o)
        {
            lock (objects)
            {
                if (objects.ContainsValue(o))
                {
                    objects.Remove(objects.Where((kvp) => kvp.Value == o).Select((kvp) => kvp.Key).First());
                }
            }
        }

        public static bool Contains(string token)
        {
            lock (objects)
            {
                return objects.ContainsKey(token);
            }
        }
    }
}
