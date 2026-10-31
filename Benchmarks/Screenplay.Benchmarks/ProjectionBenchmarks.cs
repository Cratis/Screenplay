// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using BenchmarkDotNet.Attributes;

namespace Cratis.Screenplay.Benchmarks;

/// <summary>
/// Measures projection of 10,000 facts; compilation and fact construction are excluded.
/// Each invocation starts from an empty projection, with a bounded document/child population.
/// </summary>
[MemoryDiagnoser]
public class ProjectionBenchmarks
{
    ProjectionWorkload _rootOnly = null!;
    ProjectionWorkload _childWithoutSharedEvents = null!;
    ProjectionWorkload _childWithSharedEvents = null!;
    ProjectionWorkload _childWithSharedEventsDistinctChildren = null!;

    /// <summary>
    /// Compiles the scope shapes and prepares their four immutable fact histories.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        _rootOnly = ProjectionWorkload.Create(withChild: false, shared: false);
        _childWithoutSharedEvents = ProjectionWorkload.Create(withChild: true, shared: false);
        _childWithSharedEvents = ProjectionWorkload.Create(withChild: true, shared: true);
        _childWithSharedEventsDistinctChildren = ProjectionWorkload.Create(withChild: true, shared: true, distinctChildren: true);
    }

    /// <summary>Projects facts into a root-only projection.</summary>
    /// <returns>The number of projected roots, consumed by BenchmarkDotNet.</returns>
    [Benchmark]
    public int RootOnly() => _rootOnly.Project();

    /// <summary>Projects a child whose event contracts are not shared with its parent.</summary>
    /// <returns>The number of projected roots, consumed by BenchmarkDotNet.</returns>
    [Benchmark]
    public int ChildWithoutSharedEvents() => _childWithoutSharedEvents.Project();

    /// <summary>Projects a child sharing an update event with its parent.</summary>
    /// <returns>The number of projected roots, consumed by BenchmarkDotNet.</returns>
    [Benchmark]
    public int ChildWithSharedEvents() => _childWithSharedEvents.Project();

    /// <summary>
    /// Projects shared updates across 1,000 distinct children, measuring creation-index growth.
    /// </summary>
    /// <returns>The number of projected roots, consumed by BenchmarkDotNet.</returns>
    [Benchmark]
    public int ChildWithSharedEventsDistinctChildren() => _childWithSharedEventsDistinctChildren.Project();
}
