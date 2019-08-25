using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace AsynchronousCalls
{
    /// <summary>
    /// This is demo of multi threaded asynchronous calls to the service application
    /// As a service, I use just simple library which return string with random delay
    /// I will use various ways to add asynchronious tasks to demonstrate it
    /// and compare with normal, synchronous task
    /// 
    /// So, I have 4 possible scenarious, synchronous is obviously the longest one 
    /// because I'm getting slow data one after each other
    /// 
    /// Quite opposite is CallAsynchronously, when I'm getting all data in parallel
    /// 
    /// Most realistic scenario is async call in batches: CallAsynchronouslyBatches
    /// It's because often we have some limitation of parallelism, 
    /// like we can't call same web service more than 20 times at the same time.
    /// 
    /// Last one and the trickiest is with yield: TestYieldCalls
    /// Basically I'm creating "lazy" dictionarly of tasks. 
    /// And I had issue with scope, when was creating these demo. 
    /// Practicality of it? Well.. 
    /// Normally asynchronous tasks will come back unordered, shortest will come first.
    /// And sometimes we need to order result in given order. 
    /// Like list of named payments should be in the same order.
    /// So this sever this purpose. 
    /// 
    /// TODO: write another version of TestYieldCalls with batches. I will do it later. 
    /// 
    /// heh... should I put copyright? Feel free to use it, 
    /// Serge Klokov, 2019 :)
    /// Enjoy!
    /// </summary>
    class Program
    {
        const int callCount = 18;  // total amount of calls to the service
        const int batchSize = 4; // allowed quontity of parallel processes

        static void Main(string[] args)
        {
            CallAsynchronously();
            Console.WriteLine("\r\n==============================================\r\n");

            Task.WhenAll(CallAsynchronouslyBatches()).Wait();
            Console.WriteLine("\r\n==============================================\r\n");

            CallSynchronously();
            Console.WriteLine("\r\n==============================================\r\n");

            Task.WhenAll(TestYieldCalls()).Wait();
            Console.WriteLine("\r\n==============================================\r\n");

            Console.WriteLine("All done. Press any key..");
            Console.ReadKey();
        }


        /// <summary>
        /// This task should execute much faster than synchronous method
        /// in fact it should not be significantly longer, than one individual and longest call to service
        /// </summary>
        private static void CallAsynchronously()
        {
            Console.WriteLine($"Call asynchronously start.");

            var watch = System.Diagnostics.Stopwatch.StartNew();

            Func<object, KeyValuePair<int, string>> action = (object o) =>
            {
                int i = (int)o; // getting parameter(s)
                return StringService.RandomValues.GetRandomStringWDelay(i);
            };

            var tasks = new List<Task<KeyValuePair<int, string>>>();

            for (int i = 0; i < callCount; i++)
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
            Console.WriteLine($"Call asynchronously in batches of {0} start.", batchSize);

            var watch = System.Diagnostics.Stopwatch.StartNew();

            Func<object, KeyValuePair<int, string>> action = (object o) =>
            {
                int i = (int)o; // getting parameter(s)
                return StringService.RandomValues.GetRandomStringWDelay(i);
            };

            var batchTasks = new List<Task<KeyValuePair<int, string>>>();
            var allTasks = new List<Task<KeyValuePair<int, string>>>();

            for (int i = 0; i < callCount; i++)
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
            Console.WriteLine($"Call synchronously start.");

            var watch = System.Diagnostics.Stopwatch.StartNew();

            IDictionary<int, string> dictionary = new Dictionary<int, string>();
            for (int i = 0; i < callCount; i++)
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

            for (int i = 0; i < callCount; i++)
            {
                int j = i; // because of scope we can't use "i"  !!! Wow, I didn't expect it, took me for a while to figure out. 

                yield return Task.Run(async delegate {
                    var pair = await StringService.RandomValues.GetRandomStringWDelayAsync(j);

                    if (pair.Value != null)
                    {
                        lock (lockObj)
                            dictionary[j] = pair.Value;
                    }
                });
            }
        }


        // This was not working until issue with lazy call of for(int i..) 
        // loop was replaced by local variable j = i
        // see GetTaskQueue above
        private static async Task LoadDictionaryWYield(Dictionary<int, string> dictionary)
        {
            var tasks = new List<Task>();
            //var completedTasks = new List<Task>();

            foreach (var task in GetTasksQueue(dictionary))
                tasks.Add(task);

            await Task.WhenAll(tasks);
            //completedTasks.AddRange(tasks.Where(t => t.IsCompleted));
            //await Task.WhenAll(completedTasks);
        }

        private static async Task TestYieldCalls() {
            Console.WriteLine($"Call asynchronously with YIELD start.");

            var watch = System.Diagnostics.Stopwatch.StartNew();
            var dictionary = new Dictionary<int, string>();

            await LoadDictionaryWYield(dictionary);

            foreach (var s in dictionary)
                Console.Write($"{s.Key},{s.Value};");

            Console.WriteLine();
            Console.WriteLine();
            watch.Stop();
            Console.WriteLine($"Call asynchronously with YIELD done. Elapsed time: {watch.ElapsedMilliseconds} ms");
        }
    } 
}
