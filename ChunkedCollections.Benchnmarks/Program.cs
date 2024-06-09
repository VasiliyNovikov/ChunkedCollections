using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Filters;
using BenchmarkDotNet.Running;
using ChunkedCollections.Benchmarks;
using System.Linq;

var categories = new[]
{
    "Index",
    "Enumerate",
    //"Populate",
    //"Populate Capacity",
    //"Sort",
};

var config = DefaultConfig.Instance.AddFilter(new SimpleFilter(benchmark => benchmark.Descriptor.Categories.Any(c => categories.Contains(c))));
BenchmarkRunner.Run<ChunkedCollectionBenchmarks>(config);