// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using BenchmarkDotNet.Attributes;
using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Benchmarks;

/// <summary>
/// Measures parsing and printing of the Invoicing sample, the broadest single-document model in the repository.
/// Run it with <c>--runtimes net10.0 net11.0</c> to see what the runtime alone changes.
/// </summary>
[MemoryDiagnoser]
public class ParserBenchmarks
{
    string _source = null!;
    ApplicationSyntax _application = null!;

    /// <summary>
    /// Loads the sample and parses it once for the print benchmark.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Samples", "Invoicing", "invoicing.play")))
        {
            directory = directory.Parent;
        }

        _source = File.ReadAllText(Path.Combine(
            directory?.FullName ?? throw new FileNotFoundException("Samples/Invoicing/invoicing.play was not found above the benchmark output."),
            "Samples",
            "Invoicing",
            "invoicing.play"));
        var parsed = new ScreenplayCompiler().Parse(_source);
        _application = parsed.Value ?? throw new InvalidOperationException("The Invoicing sample did not parse.");
    }

    /// <summary>Parses the sample.</summary>
    /// <returns>Whether parsing succeeded, consumed by BenchmarkDotNet.</returns>
    [Benchmark]
    public bool Parse() => new ScreenplayCompiler().Parse(_source).Success;

    /// <summary>Prints the parsed sample.</summary>
    /// <returns>The printed length, consumed by BenchmarkDotNet.</returns>
    [Benchmark]
    public int Print() => new ScreenplayPrinter().Print(_application).Length;
}
