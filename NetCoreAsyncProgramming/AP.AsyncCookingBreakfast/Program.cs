using AP.AsyncCookingBreakfast.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// Modified source files from repo: 
/// https://github.com/dotnet/docs/tree/main/docs/csharp/programming-guide/concepts/async/snippets/index
/// </summary>

namespace AP.AsyncCookingBreakfast
{
    internal class Program
    {
        #region Main

        static async Task<int> Main(string[] args)
        {
            // Ex. 1
            await CookingBreakfastV1();

            // Ex. 2
            //var breakfastReady = await CookingBreakfastV2();
            //Console.WriteLine($"Breakfast completed = {breakfastReady}");

            Console.Read();
            return 0;
        }

        #endregion

        #region Examples

        // Await tasks individually
        private static async Task CookingBreakfastV1()
        {
            var cup = PourCoffee();
            Console.WriteLine($"Coffee is ready at {DateTime.Now}");

            var eggsTask = FryEggsAsync(3);
            var baconTask = FryBaconAsync(4);
            var toastTask = MakeToastWithButterAndJamAsync(3);

            var eggs = await eggsTask;
            Console.WriteLine($"Eggs are ready at {DateTime.Now}");

            var bacon = await baconTask;
            Console.WriteLine($"Bacon is ready at {DateTime.Now}");

            var toast = await toastTask;
            Console.WriteLine($"Toast is ready at {DateTime.Now}");

            var oj = PourOJ();
            Console.WriteLine($"Orange juice is ready");

            await Task.Delay(1000);
            Console.WriteLine($"Breakfast is ready! at {DateTime.Now}");
        }

        // Await tasks efficiently
        private static async Task<bool> CookingBreakfastV2()
        {
            var cup = PourCoffee();
            Console.WriteLine($"Coffee is ready at {DateTime.Now}");

            var eggsTask = FryEggsAsync(3);
            var baconTask = FryBaconAsync(4);
            var toastTask = MakeToastWithButterAndJamAsync(3);

            #region Option 1. Wait for all

            await Task.WhenAll(eggsTask, baconTask, toastTask);
            Console.WriteLine($"Eggs are ready at {DateTime.Now}");
            Console.WriteLine($"Bacon is ready at {DateTime.Now}");
            Console.WriteLine($"Toast is ready at {DateTime.Now}");

            #endregion

            #region Option 2. Wait the first task to finish and then process its result

            //var breakfastTasks = new List<Task> { eggsTask, baconTask, toastTask };

            //while (breakfastTasks.Count > 0)
            //{
            //    var finishedTask = await Task.WhenAny(breakfastTasks);

            //    if (finishedTask == eggsTask)
            //    {
            //        Console.WriteLine($"Eggs are ready at {DateTime.Now}");
            //    }
            //    else if (finishedTask == baconTask)
            //    {
            //        Console.WriteLine($"Bacon is ready at {DateTime.Now}");
            //    }
            //    else if (finishedTask == toastTask)
            //    {
            //        Console.WriteLine($"Toast is ready at {DateTime.Now}");
            //    }
            //    breakfastTasks.Remove(finishedTask);
            //}

            #endregion

            var oj = PourOJ();
            Console.WriteLine($"Orange juice is ready");
            
            Console.WriteLine($"Breakfast is ready! at {DateTime.Now}");

            return true;
        }

        private static Coffee PourCoffee()
        {
            Console.WriteLine("Coffee - Pouring coffee");
            return new Coffee();
        }

        private static async Task<Egg> FryEggsAsync(int howMany)
        {
            Console.WriteLine("Eggs - Warming the egg pan...");
            await Task.Delay(3000);
            Console.WriteLine($"Eggs - cracking {howMany} eggs");
            Console.WriteLine("Eggs - cooking the eggs ...");
            await Task.Delay(3000);
            Console.WriteLine($"Eggs - Put eggs on plate at {DateTime.Now}");

            return new Egg();
        }

        private static async Task<Bacon> FryBaconAsync(int slices)
        {
            Console.WriteLine($"Bacon - putting {slices} slices of bacon in the pan");
            Console.WriteLine("Bacon - cooking first side of bacon...");
            
            await Task.Delay(5000);
           
            for (int slice = 0; slice < slices; slice++)
            {
                Console.WriteLine("Bacon - flipping a slice of bacon");
            }
            Console.WriteLine("Bacon - cooking the second side of bacon...");
            
            await Task.Delay(3000);
            
            Console.WriteLine($"Bacon - Put bacon on plate at {DateTime.Now}");

            return new Bacon();
        }

        private static async Task<Toast> MakeToastWithButterAndJamAsync(int number)
        {
            var toast = await ToastBreadAsync(number);
            ApplyButter(toast);
            ApplyJam(toast);

            return toast;
        }
        
        private static void ApplyJam(Toast toast) =>
            Console.WriteLine($"Toast - Putting jam on the toast at {DateTime.Now}");

        private static void ApplyButter(Toast toast) =>
            Console.WriteLine($"Toast - Putting butter on the toast at {DateTime.Now}");

        private static async Task<Toast> ToastBreadAsync(int slices)
        {
            for (int slice = 0; slice < slices; slice++)
            {
                Console.WriteLine("Toast - Putting a slice of bread in the toaster");
            }

            Console.WriteLine($"Toast - Start toasting... at {DateTime.Now}");

            await Task.Delay(4000);
            
            // Ex. 1 Uncomment these lines for testing exception behavior
            // Console.WriteLine("Fire! Toast is ruined!");
            // throw new InvalidOperationException("The toaster is on fire");
            
            await Task.Delay(1000);
            
            Console.WriteLine($"Toast - Remove toast from toaster at {DateTime.Now}");

            return new Toast();
        }

        private static Juice PourOJ()
        {
            Console.WriteLine("Juice - Pouring orange juice");
            return new Juice();
        }

        #endregion
    }
}
