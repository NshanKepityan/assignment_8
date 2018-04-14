using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ParallelLoop
{
    class ParallelLoop
    {
        /// <summary>
        /// Parallel for loop
        /// </summary>
        /// <param name="fromInclusive">the begining</param>
        /// <param name="toExclusive">the end</param>
        /// <param name="body">the body</param>
        public static void ParallelFor(int fromInclusive, int toExclusive, Action<int> body)
        {

            if(body == null)
                throw new ArgumentNullException();

            if(fromInclusive > toExclusive)
                throw new ArgumentOutOfRangeException();

           
            int chunkSize = 4;

           
            int countOfThreads = Environment.ProcessorCount;
            int index = fromInclusive - chunkSize;
            
            var locker = new object();

            // processing function
            // takes next chunk and processes it using action
            Action action =  delegate
            {
                while (true)
                {
                    int chunkStart;
                    lock (locker)
                    {
                        index += chunkSize;
                        chunkStart = index;
                    }

                   
                    for (int i = chunkStart; i < chunkStart + chunkSize; i++)
                    {
                        if (i >= toExclusive)
                        {
                            return;
                        }

                        body(i);
                    }
                }
            };

            // launching all process() threads
            IAsyncResult[] asyncResults = new IAsyncResult[countOfThreads];
            for (int i = 0; i < countOfThreads; ++i)
            {
                asyncResults[i] = action.BeginInvoke(null, null);
            }
            // waiting for all threads to finish
            for (int i = 0; i < countOfThreads; ++i)
            {
                action.EndInvoke(asyncResults[i]);
            }
        }

        /// <summary>
        /// Parallel foreach loop
        /// </summary>
        /// <typeparam name="TSource">tye type of the sorce </typeparam>
        /// <param name="source">the source</param>
        /// <param name="body">the body</param>
        public static void ParallelForEach<TSource>(IEnumerable<TSource> source, Action<TSource> body)
        {
            if (source == null || body == null)
                throw new ArgumentNullException();

            var resetEvents = new List<ManualResetEvent>();

            foreach (var item in source)
            {
                var evnt = new ManualResetEvent(false);
                ThreadPool.QueueUserWorkItem( i =>
                {
                    body((TSource) i);
                    evnt.Set();
                }, item);
                resetEvents.Add(evnt);
            }

            foreach (var r in resetEvents)
                r.WaitOne();
        }

        /// <summary>
        /// Parallel foreach loop with given option
        /// </summary>
        /// <typeparam name="TSource">the type of source</typeparam>
        /// <param name="source">the given source</param>
        /// <param name="parallelOptions">the option</param>
        /// <param name="body">the body</param>
        public static void ParallelForEachWithOptions<TSource>(IEnumerable<TSource> source,
            ParallelOptions parallelOptions, Action<TSource> body)
        {
            if (source == null || parallelOptions == null ||  body == null)
            {
                throw new ArgumentNullException();
            }

            Task[] allTasks = new Task[parallelOptions.MaxDegreeOfParallelism];
            int tasksIndex = 0;
            int activeTasks = 0;

            foreach (var i in source)
            {
                var task = new Task(() => body(i));
                if(activeTasks >= parallelOptions.MaxDegreeOfParallelism)
                    allTasks[Task.WaitAny(allTasks)] = task;// waiting for any of active Task objects to finish
                else

                {
                    allTasks[tasksIndex] = task;
                    tasksIndex++;   
                    activeTasks++;
                }

                task.Start();
            }

            Task.WaitAll(allTasks);
        }
    }
}