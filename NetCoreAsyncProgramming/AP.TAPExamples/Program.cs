using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Modified source files from repo: 
/// https://github.com/dotnet/docs/tree/main/docs/csharp/programming-guide/concepts/async/snippets
/// </summary>

namespace AP.TAPExamples
{
    internal class Program
    {
        #region Main

        static async Task<int> Main(string[] args)
        {
            string options =
                @"Options:
    1. Read Words Async
    2. File Access - Write Async - Simple
    3. File Access - Write Async - Multiple
    4. Cancel Tasks on Enter
    5. Cancel Tasks after 2 secs
    6. Page Size Retry On Fault
    7. Sum Page Sizes Interleaved Async
    8. Sum Page Sizes When All Or First Exception
    0. Exit
    Which example you want to run: ";

            bool show = true;

            while (show)
            {
                Console.Clear();
                Console.Write(options);

                switch (Console.ReadLine())
                {
                    case "0":
                        show = false;
                        break;
                    case "1":
                        await ReadWordsAsync();
                        break;
                    case "2":
                        await SimpleParallelWriteAsync();
                        break;
                    case "3":
                        await ProcessMultipleWritesAsync();
                        break;
                    case "4":
                        await CancelAListOfTasks();
                        break;
                    case "5":
                        await CancelTasksAfterAPeriodOfTimeAsync();
                        break;
                    case "6":
                        await GetUrlContentSizeWithRetryAsync();
                        break;
                    case "7":
                        await SumPageSizesInterleavedAsync();
                        break;
                    case "8":
                        await SumPageSizesWhenAllOrFirstExceptionAsync();
                        break;
                }

                Console.WriteLine("Press ENTER to continue...");
                Console.ReadLine();
            }

            return 0;
        }

        #endregion

        #region Examples

        #region Members

        static readonly CancellationTokenSource s_cts = new CancellationTokenSource();

        static readonly HttpClient s_client = new HttpClient
        {
            MaxResponseContentBufferSize = 1_000_000
        };

        static readonly IEnumerable<string> s_urlList = new string[]
        {
            "https://docs.microsoft.com",
            "https://docs.microsoft.com/aspnet/core",
            "https://docs.microsoft.com/azure",
            "https://docs.microsoft.com/azure/devops",
            "https://docs.microsoft.com/dotnet",
            "https://docs.microsoft.com/dynamics365",
            "https://docs.microsoft.com/education",
            "https://docs.microsoft.com/enterprise-mobility-security",
            "https://docs.microsoft.com/gaming",
            "https://docs.microsoft.com/graph",
            "https://docs.microsoft.com/microsoft-365",
            "https://docs.microsoft.com/office",
            "https://docs.microsoft.com/powershell",
            "https://docs.microsoft.com/sql",
            "https://docs.microsoft.com/surface",
            "https://docs.microsoft.com/system-center",
            "https://docs.microsoft.com/visualstudio",
            "https://docs.microsoft.com/windows",
            "https://docs.microsoft.com/xamarin"
        };

        #endregion

        #region Async Stream

        static async Task ReadWordsAsync()
        {
            await foreach (string word in ReadWordsFromStreamAsync())
            {
                Console.WriteLine(word);
            }
        }

        static async IAsyncEnumerable<string> ReadWordsFromStreamAsync()
        {
            string data =
                @"This is a line of text.
                  Here is the second line of text.
                  And there is one more for good measure.
                  Wait, that was the penultimate line.";

            using var readStream = new StringReader(data);

            string line = await readStream.ReadLineAsync();

            while (line != null)
            {
                foreach (string word in line.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    yield return word;
                }

                line = await readStream.ReadLineAsync();
            }
        }

        #endregion

        #region File Access

        static async Task SimpleParallelWriteAsync()
        {
            string folder = Directory.CreateDirectory("tempfolder").Name;
            IList<Task> writeTaskList = new List<Task>();

            for (int index = 11; index <= 20; ++index)
            {
                string fileName = $"file-{index:00}.txt";
                string filePath = $"{folder}/{fileName}";
                string text = $"In file {index}{Environment.NewLine}";

                writeTaskList.Add(WriteFileToDisk(filePath, text));
            }

            Task allTasks = Task.WhenAll(writeTaskList);

            try
            {
                Console.WriteLine("Awaiting all tasks...");
                await allTasks;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
                Console.WriteLine($"Task IsFaulted: {allTasks.IsFaulted}");
                foreach (var inEx in allTasks.Exception.InnerExceptions)
                {
                    Console.WriteLine($"Task Inner Exception: {inEx.Message}");
                }
            }
            finally
            {
                Console.WriteLine($"All files written into folder : {folder}");
            }
        }

        private static Task WriteFileToDisk(string filePath, string text)
        {
            Console.WriteLine($"- Writting File To Disk : {filePath}");

            if (filePath.Contains("13") || filePath.Contains("15") || filePath.Contains("17"))
            {
                //await Task.Delay(2000);
                //throw new Exception("Error-" + filePath);
            }

            return File.WriteAllTextAsync(filePath, text);
        }

        static async Task ProcessMultipleWritesAsync()
        {
            IList<FileStream> sourceStreams = new List<FileStream>();

            try
            {
                string folder = Directory.CreateDirectory("tempfolder").Name;
                IList<Task> writeTaskList = new List<Task>();

                for (int index = 1; index <= 10; ++index)
                {
                    string fileName = $"file-{index:00}.txt";
                    string filePath = $"{folder}/{fileName}";

                    string text = $"In file {index}{Environment.NewLine}";
                    byte[] encodedText = Encoding.Unicode.GetBytes(text);

                    var sourceStream =
                        new FileStream(
                            filePath,
                            FileMode.Create, FileAccess.Write, FileShare.None,
                            bufferSize: 4096, useAsync: true);

                    Task writeTask = sourceStream.WriteAsync(encodedText, 0, encodedText.Length);
                    sourceStreams.Add(sourceStream);

                    writeTaskList.Add(writeTask);

                    Console.WriteLine($"- Writting File To Disk : {filePath}");
                }

                await Task.WhenAll(writeTaskList);
            }
            finally
            {
                foreach (FileStream sourceStream in sourceStreams)
                {
                    sourceStream.Close();
                }
            }
        }

        #endregion

        #region Cancel Tasks & Multiple Tasks (Process asynchronous tasks as they complete)

        // Cancel a list of tasks
        static async Task CancelAListOfTasks()
        {
            Console.WriteLine("Cancel a list of tasks started.");
            Console.WriteLine("Press the ENTER key to cancel...\n");

            Task cancelTask = Task.Run(() =>
            {
                while (Console.ReadKey().Key != ConsoleKey.Enter)
                {
                    Console.WriteLine("Press the ENTER key to cancel...");
                }

                Console.WriteLine("\nENTER key pressed: cancelling downloads.\n");
                s_cts.Cancel();
            });

            Task sumPageSizesTask = SumPageSizesAsyncV1();

            await Task.WhenAny(new[] { cancelTask, sumPageSizesTask });

            Console.WriteLine("Cancel a list of tasks ending.");
        }

        // Cancel async tasks after a period of time
        static async Task CancelTasksAfterAPeriodOfTimeAsync()
        {
            Console.WriteLine("Cancel async tasks after a period of time started.");

            try
            {
                s_cts.CancelAfter(2000);

                await SumPageSizesAsyncV2();
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine("\nTasks cancelled: timed out.\n");
            }
            finally
            {
                s_cts.Dispose();
            }

            Console.WriteLine("Cancel async tasks after a period of time ending.");
        }

        static async Task SumPageSizesAsyncV1()
        {
            var stopwatch = Stopwatch.StartNew();

            int total = 0;
            foreach (string url in s_urlList)
            {
                int contentLength = await ProcessUrlAsync(url, s_client, s_cts.Token);
                total += contentLength;
            }

            stopwatch.Stop();

            Console.WriteLine($"\nTotal bytes returned:  {total:#,#}");
            Console.WriteLine($"Elapsed time:          {stopwatch.Elapsed}\n");
        }

        // Process asynchronous tasks as they complete
        static async Task SumPageSizesAsyncV2()
        {
            var stopwatch = Stopwatch.StartNew();

            IEnumerable<Task<int>> downloadTasksQuery =
                from url in s_urlList
                select ProcessUrlAsync(url, s_client, s_cts.Token);

            List<Task<int>> downloadTasks = downloadTasksQuery.ToList();

            int total = 0;
            while (downloadTasks.Any())
            {
                Task<int> finishedTask = await Task.WhenAny(downloadTasks);
                downloadTasks.Remove(finishedTask);
                total += await finishedTask;
            }

            stopwatch.Stop();

            Console.WriteLine($"\nTotal bytes returned:  {total:#,#}");
            Console.WriteLine($"Elapsed time:          {stopwatch.Elapsed}\n");
        }

        static async Task<int> ProcessUrlAsync(string url, HttpClient client, CancellationToken token)
        {
            await Task.Delay(1000);
            HttpResponseMessage response = await client.GetAsync(url, token);
            Console.WriteLine($"Processing url: {url}");
            byte[] content = await response.Content.ReadAsByteArrayAsync(token);
            Console.WriteLine($"Completed {url,-60} {content.Length,10:#,#}");

            return content.Length;
        }

        #endregion

        #region Task-based Combinators

        // Source: https://docs.microsoft.com/en-us/dotnet/standard/asynchronous-programming-patterns/consuming-the-task-based-asynchronous-pattern

        // RetryOnFault
        static async Task<T> RetryOnFaultAsync<T>(Func<Task<T>> function, int maxTries, Func<Task> retryWhen)
        {
            for (int i = 0; i < maxTries; i++)
            {
                try
                {
                    Console.WriteLine($"Calling function...");
                    return await function().ConfigureAwait(false);
                }
                catch
                {
                    if (i == maxTries - 1)
                        throw;
                }
                await retryWhen().ConfigureAwait(false);
            }
            return default;
        }

        static async Task<int> GetUrlContentSizeWithRetryAsync()
        {
            try
            {
                string url = "https://docsnotexist.microsoft.com";

                Console.WriteLine($"\nTrying to process the url: {url}");

                // Try up to three times in case of failure, and delaying for a second between retries
                var pageSize = await RetryOnFaultAsync(() => ProcessUrlAsync(url, s_client, s_cts.Token),
                                                       3,
                                                       () => Task.Delay(1000));

                Console.WriteLine($"\nTotal bytes returned:  {pageSize:#,#}");

                return pageSize;
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
            }
            return 0;
        }

        // Interleaved Operations
        static IEnumerable<Task<T>> Interleaved<T>(IEnumerable<Task<T>> tasks)
        {
            var inputTasks = tasks.ToList();

            var sources = (from _ in Enumerable.Range(0, inputTasks.Count)
                           select new TaskCompletionSource<T>())
                           .ToList();

            int nextTaskIndex = -1;

            foreach (var inputTask in inputTasks)
            {
                inputTask.ContinueWith(completed =>
                {
                    var source = sources[Interlocked.Increment(ref nextTaskIndex)];

                    if (completed.IsFaulted)
                    {
                        source.TrySetException(completed.Exception.InnerExceptions);
                    }
                    else if (completed.IsCanceled)
                    {
                        source.TrySetCanceled();
                    }
                    else
                    {
                        source.TrySetResult(completed.Result);
                    }
                }, 
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
            }

            return from source in sources
                   select source.Task;
        }

        // Interleaving without WhenAny
        static async Task SumPageSizesInterleavedAsync()
        {
            var stopwatch = Stopwatch.StartNew();

            IEnumerable<Task<int>> downloadTasksQuery =
                from url in s_urlList
                select ProcessUrlAsync(url, s_client, s_cts.Token);

            List<Task<int>> downloadTasks = downloadTasksQuery.ToList();

            int total = 0;

            foreach (var finishedTask in Interleaved(downloadTasks))
            {
                total += await finishedTask;
            }

            stopwatch.Stop();

            Console.WriteLine($"\nTotal bytes returned:  {total:#,#}");
            Console.WriteLine($"Elapsed time:          {stopwatch.Elapsed}\n");
        }

        // When all or first exception
        static Task<T[]> WhenAllOrFirstException<T>(IEnumerable<Task<T>> tasks)
        {
            var inputs = tasks.ToList();

            var ce = new CountdownEvent(inputs.Count);
            var tcs = new TaskCompletionSource<T[]>(TaskCreationOptions.RunContinuationsAsynchronously);

            void onCompleted(Task completed)
            {
                if (completed.IsFaulted)
                {
                    tcs.TrySetException(completed.Exception.InnerExceptions);
                }

                if (ce.Signal() && !tcs.Task.IsCompleted)
                {
                    tcs.TrySetResult(inputs.Select(t => t.Result).ToArray());
                }
            }

            foreach (var t in inputs)
            {
                t.ContinueWith((Action<Task>)onCompleted);
            }

            return tcs.Task;
        }

        static async Task SumPageSizesWhenAllOrFirstExceptionAsync()
        {
            var newUrlList = s_urlList.ToList();

            newUrlList.Insert(1, "https://docsnotexist1.microsoft.com");
            newUrlList.Insert(3, "https://docsnotexist2.microsoft.com");
            newUrlList.Insert(5, "https://docsnotexist3.microsoft.com");
            newUrlList.Insert(7, "https://docsnotexist4.microsoft.com");
            newUrlList.Insert(9, "https://docsnotexist5.microsoft.com");
            newUrlList.Insert(11, "https://docsnotexist6.microsoft.com");

            IEnumerable<Task<int>> downloadTasksQuery =
                    from url in newUrlList
                    select ProcessUrlAsync(url, s_client, s_cts.Token);

            int total = 0;
            var results = WhenAllOrFirstException(downloadTasksQuery);

            try
            {
                total = (await results).Sum();
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
            }

            Console.WriteLine($"\nTotal bytes returned:  {total:#,#}");
        }

        #endregion 

        #endregion
    }


}
