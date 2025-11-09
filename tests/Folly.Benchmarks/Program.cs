using BenchmarkDotNet.Running;

namespace Folly.Benchmarks;

class Program
{
    static int Main(string[] args)
    {
        // Check if we should run the CI performance test
        if (args.Length > 0 && args[0] == "--ci")
        {
            return PerformanceTest.Run(args.Skip(1).ToArray());
        }

        // Otherwise run full benchmarks
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
        return 0;
    }
}
