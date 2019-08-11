using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

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
        const int batchSize = 4; // allowed quontity of parallel processes

        static void Main(string[] args)
        {
            CallAsynchronously();
            Task.WhenAll(CallAsynchronouslyBatches());
            CallSynchronously();
            TestYildCalls();

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
            Console.WriteLine();
            watch.Stop();
            Console.WriteLine($"Call asynchronously was done. Elapsed time: {watch.ElapsedMilliseconds} ms");

        }

        // Normally we cannot have unlimited parallel processes
        // We create specific amount of parellel processes per batch
        // And wait, until batch completed, then we create new batch
        private static async Task CallAsynchronouslyBatches()
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();

            Func<object, KeyValuePair<int, string>> action = (object o) =>
            {
                int i = (int)o; // getting parameter(s)
                return StringService.RandomValues.GetRandomStringWDelay(i);
            };

            var batchTasks = new List<Task<KeyValuePair<int, string>>>();
            var allTasks = new List<Task<KeyValuePair<int, string>>>();

            for (int i = 0; i < amount; i++)
            {
                batchTasks.Add(Task<KeyValuePair<int, string>>.Factory.StartNew(action, i));

                if (batchTasks.Count >= batchSize)
                {
                    // if task(s) completed, then we will remove it from the batch
                    await Task.WhenAny(batchTasks);
                    var toFinish = batchTasks.Where(t => t.IsCompleted);
                    allTasks.AddRange(toFinish);
                    batchTasks.RemoveAll(t => toFinish.Contains(t));
                }
            }

            // process last batch
            await Task.WhenAll(batchTasks);
            allTasks.AddRange(batchTasks);

            // get results
            var dictionary = Task.WhenAll(allTasks).Result;

            foreach (var s in dictionary)
                Console.Write($"{s.Key},{s.Value};");

            Console.WriteLine();
            Console.WriteLine();
            watch.Stop();
            Console.WriteLine($"Call asynchronously in batches. Done. Elapsed time: {watch.ElapsedMilliseconds} ms");
        }

        // Let's play with yield return (C# 8.0 feature)
        // TODO: develop


        private static void CallSynchronously()
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();

            IDictionary<int, string> dictionary = new Dictionary<int, string>();
            for (int i = 0; i < amount; i++)
                 dictionary.Add(StringService.RandomValues.GetRandomStringWDelay(i));

            foreach (var s in dictionary)
                Console.Write($"{s.Key},{s.Value};");

            Console.WriteLine();
            Console.WriteLine();
            watch.Stop();
            Console.WriteLine($"Call synchronously was done. Elapsed time: {watch.ElapsedMilliseconds} ms");
        }

        private static IEnumerable<Task> GetTasksQueue(Dictionary<int,string> dictionary)
        {
            object lockObj = new object();

            for (int i = 0; i < amount; i++)
            {
                yield return Task.Run(async delegate {
                    var pair = await StringService.RandomValues.GetRandomStringWDelayAsync(i);

                    if (pair.Value != null)
                    {
                        lock (lockObj)
                            dictionary[i] = pair.Value;
                    }
                });
            }
        }


        // This is not working. TODO
        private static async Task LoadDictionaryWYild(Dictionary<int, string> dictionary)
        {
            var tasks = new List<Task>();
            var completedTasks = new List<Task>();

            foreach (var task in GetTasksQueue(dictionary))
                tasks.Add(task);

            await Task.WhenAll(tasks);
            completedTasks.AddRange(tasks.Where(t => t.IsCompleted));
            await Task.WhenAll(completedTasks);
        }

        // This is not working. TODO
        private static void TestYildCalls() {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            var dictionary = new Dictionary<int, string>();

            var t = LoadDictionaryWYild(dictionary);
            Task.WhenAll(t);

            foreach (var s in dictionary)
                Console.Write($"{s.Key},{s.Value};");

            Console.WriteLine();
            Console.WriteLine();
            watch.Stop();
            Console.WriteLine($"Call asynchronously with YIELD done. Elapsed time: {watch.ElapsedMilliseconds} ms");
        }
    }
}
