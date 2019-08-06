using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AsynchronousCalls
{
    /// <summary>
    /// This is demo of multythreaded asynchronous calls to the service application
    /// As a service, I use just simple library which return string with random delay
    /// I will use various ways to add asynchronious tasks to demonstrate it
    /// and compare with normal, synchronous task
    /// 
    /// heh... should I put copyright? Feel free to use it, Serge Klokov, 2019 :)
    /// </summary>
    class Program
    {
        const int amount = 18;  // total amount of calls to the service

        static void Main(string[] args)
        {
            CallAsynchronously();
            CallSynchronously();

            Console.WriteLine("All done. Press any key..");
            Console.ReadKey();
        }


        /// <summary>
        /// This task should execute much faster than synchronous method
        /// in fact it should not be significantly longer, than one individual and longest call to service
        /// </summary>
        private static void CallAsynchronously()
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();

            Func<object, KeyValuePair<int, string>> action = (object o) =>
            {
                int i = (int)o; // getting parameter(s)
                return StringService.RandomValues.GetRandomStringWDelay(i);
            };

            var tasks = new List<Task<KeyValuePair<int, string>>>();

            for (int i = 0; i < amount; i++)
                tasks.Add(Task<KeyValuePair<int, string>>.Factory.StartNew(action, i));

            var dictionary = Task.WhenAll(tasks).Result;
 
            foreach (var s in dictionary)
                Console.Write($"{s.Key},{s.Value};");

            Console.WriteLine();
            watch.Stop();
            Console.WriteLine($"Call asynchronously was done. Elapsed time: {watch.ElapsedMilliseconds} ms");

        }

        private static void CallSynchronously()
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();

            IDictionary<int, string> dictionary = new Dictionary<int, string>();
            for (int i = 0; i < amount; i++)
                 dictionary.Add(StringService.RandomValues.GetRandomStringWDelay(i));

            foreach (var s in dictionary)
                Console.Write($"{s.Key},{s.Value};");

            Console.WriteLine();
            watch.Stop();
            Console.WriteLine($"Call synchronously was done. Elapsed time: {watch.ElapsedMilliseconds} ms");
        }


    }
}
