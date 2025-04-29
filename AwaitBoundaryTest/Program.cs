using System.Runtime.CompilerServices;

namespace AwaitBoundaryTest
{
    internal static class Program
    {
        public sealed class GCObject
        {
            public int Value;

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
            public GCObject(int value)
            {
                Value = value;
            }
        }

        // Conclusion: To allow GC variables to be collected early in async methods...
        // - Avoid using it across await boundaries
        // - Set it to null before the await boundary

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private static async Task Main(string[] args)
        {
            var gcObject = new GCObject(42);

            var weakRef = new WeakReference(gcObject);

            var consumeGCObjectTask = ConsumeGCObject(gcObject);

            // ReSharper disable once RedundantAssignment
            // Need to set it to null to GC collect early
            gcObject = null;

            await Task.Yield();

            GC.Collect();

            Console.WriteLine(
            $"""
            {nameof(gcObject)} is alive: {weakRef.IsAlive}
            {nameof(consumeGCObjectTask)} is complete: {consumeGCObjectTask.IsCompleted}
            """);
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
        private static async Task ConsumeGCObject(GCObject gcObject)
        {
            DoWorkWithGCObject(gcObject);

            // ReSharper disable once RedundantAssignment
            // Need to set it to null to GC collect early
            gcObject = null!;

            await Task.Delay(1_000_000);

            Console.WriteLine($"We will never reach here! {gcObject}");
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private static void DoWorkWithGCObject(GCObject gcObject)
        {
            // Simulate some work with the GCObject
            Console.WriteLine($"Working with GCObject: {gcObject.Value}");
        }
    }
}