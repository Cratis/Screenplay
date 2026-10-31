// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

var artifacts = Environment.GetEnvironmentVariable("SCREENPLAY_BENCHMARK_ARTIFACTS") ??
    Path.Combine(".ai-work", "132", "BenchmarkDotNet.Artifacts");
var config = ManualConfig.Create(DefaultConfig.Instance).WithArtifactsPath(artifacts);
var summaries = BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config).ToArray();
if (summaries.Length == 0)
{
    return 1;
}

return summaries.Any(Failed) ? 1 : 0;

static bool Failed(Summary summary) =>
    summary.HasCriticalValidationErrors || summary.Reports.Any(report => !report.Success);
