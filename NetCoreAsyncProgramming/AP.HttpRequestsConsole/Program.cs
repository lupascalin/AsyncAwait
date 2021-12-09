/*
 * Notice: This Sample Code is provided for the purpose of illustration only and is not intended to be used in a production environment.
 * THIS SAMPLE CODE AND ANY RELATED INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, EITHER EXPRESSED OR IMPLIED,
 * INCLUDING BUT NOT LIMITED TO THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR PURPOSE.
 *
 */
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace AP.HttpRequestsConsole
{
    internal class Program
    {
        private static HttpClient Client = new HttpClient();
        private static readonly string url1 = "https://dotnetpodcasts.azurewebsites.net"; // 20.42.128.99
        private static readonly string url2 = "https://azurecharts.com/";                 // 52.165.220.33

        static async Task Main(string[] args)
        {
            Console.WriteLine("Starting HTTP requests...");

            // Open Windows Terminal: netstat -a -p TCP -f 2

            // 1. Comment/Uncomment
            for (int i = 0; i < 50; i++)
            {
                using (var client = new HttpClient())
                {
                    var result1 = await client.GetAsync(url1);
                    Console.WriteLine($"{i} - request status code is {result1.StatusCode} for {result1.RequestMessage.RequestUri.Host} on thread {Thread.CurrentThread.ManagedThreadId}");
                };

                var result2 = await Client.GetAsync(url2);
                Console.WriteLine($"{i} - request status code is {result2.StatusCode} for {result2.RequestMessage.RequestUri.Host} on thread {Thread.CurrentThread.ManagedThreadId}");
            }

            // 2. Uncomment/Comment
            //Parallel.For(0, 50, async (i) =>
            //{
            //    using (var client = new HttpClient())
            //    {
            //        var result1 = await client.GetAsync(url1);
            //        Console.WriteLine($"{i} - request status code is {result1.StatusCode} for {result1.RequestMessage.RequestUri.Host} on thread {Thread.CurrentThread.ManagedThreadId}");
            //    };

            //    var result2 = await Client.GetAsync(url2);
            //    Console.WriteLine($"{i} - request status code is {result2.StatusCode} for {result2.RequestMessage.RequestUri.Host} on thread {Thread.CurrentThread.ManagedThreadId}");
            //});

            await Task.Delay(10000);

            Console.WriteLine("Done.");
            Console.Read();
            Console.WriteLine("Existing...");
        }
    }
}
