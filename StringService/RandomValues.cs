using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace StringService
{
    public class RandomValues
    {
        /// <summary>
        /// Potentially it may imitate web service which return image by some ID,
        /// this is why I'm using key value pair
        /// </summary>
        /// <param name="ID"></param>
        /// <returns></returns>
        public static KeyValuePair<int,string> GetRandomStringWDelay(int ID)
        {
            var r = new Random();
            var delay = r.Next(0, 500);
            Thread.Sleep(delay);
            return new KeyValuePair<int, string>(ID, delay.ToString());
        }

    
        // async version of method above
        public static async Task<KeyValuePair<int, string>> GetRandomStringWDelayAsync(int ID)
        {
            return await Task.Run(() => GetRandomStringWDelay(ID));
        }
    }
}
