// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Screenplay.Benchmarks;

sealed class ProjectionWorkload(SemanticExecutionPlan plan, ImmutableArray<SemanticFact> facts)
{
    const int FactCount = 10_000;
    const string ModelSource = """
        concept Id : String
        type Child
          childId Id
          label String?
        module Demo
          feature Items
            slice StateView Browse
              event RootChanged
                rootId Id
                label String
              event ChildAdded
                rootId Id
                childId Id
                label String
              event ChildChanged
                rootId Id
                childId Id
                label String
              readmodel Root
                rootId Id
                label String?
        """;
    const string ProjectionSource = """
              query RootById => Root?
                by rootId Id
              projection Root => Root
                from RootChanged key rootId
                  label = label
        """;
    const string SharedParentBlock = """
                from ChildChanged key rootId
                  label = label
        """;
    const string ChildScope = """
                children children identified by childId
                  from ChildAdded key childId
                    parent rootId
                    childId = childId
                    label = label
                  from ChildChanged key childId
                    parent rootId
                    label = label
        """;

    internal static ProjectionWorkload Create(bool withChild, bool shared)
    {
        var source = ModelSource + (withChild ? "\n        children Child[]" : string.Empty) + "\n" + ProjectionSource +
            (shared ? "\n" + SharedParentBlock : string.Empty) + (withChild ? "\n" + ChildScope : string.Empty);
        var parsed = new ScreenplayCompiler().Parse(source);
        if (!parsed.Success)
        {
            throw new InvalidSemanticContract($"Benchmark source did not parse.{Environment.NewLine}{string.Join(Environment.NewLine, parsed.Diagnostics.Select(diagnostic => $"{diagnostic.Code} ({diagnostic.Location.Line},{diagnostic.Location.Column}): {diagnostic.Message}"))}");
        }

        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("ProjectionBenchmarks"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("application"), "application", "application.play", source);
        var compilation = new SemanticModelBinder().Bind("ProjectionBenchmarks", parsed.Value!, SemanticDocumentSet.Create([document], catalog));
        if (!compilation.Success)
        {
            throw new InvalidSemanticContract("Benchmark source did not bind.");
        }

        var executable = SemanticExecutionPlan.Compile(compilation.Value!.Model);
        var executablePlan = executable.Plan ?? throw new InvalidSemanticContract("Benchmark projection was not admitted.");
        return new(executablePlan, BuildFacts(executablePlan, withChild, shared));
    }

    internal int Project()
    {
        if (!SemanticEvaluator.Establish(plan, [], facts, out var readModels, out var failure))
        {
            throw new InvalidSemanticContract(failure ?? "Benchmark projection failed.");
        }

        return readModels.Length;
    }

    static ImmutableArray<SemanticFact> BuildFacts(SemanticExecutionPlan executablePlan, bool withChild, bool shared)
    {
        var root = Fact(executablePlan, "RootChanged", "root");
        if (!withChild)
        {
            return [.. Enumerable.Repeat(root, FactCount)];
        }

        var added = Fact(executablePlan, "ChildAdded", "child");
        var changed = Fact(executablePlan, "ChildChanged", "child");
        var result = ImmutableArray.CreateBuilder<SemanticFact>(FactCount);

        // Place creation halfway through the shared history so historical lookup is not always an early hit.
        var precedingRoots = shared ? FactCount / 2 : 1;
        result.AddRange(Enumerable.Repeat(root, precedingRoots));
        result.Add(added);
        result.AddRange(Enumerable.Repeat(changed, FactCount - result.Count));
        return result.MoveToImmutable();
    }

    static SemanticFact Fact(SemanticExecutionPlan executablePlan, string name, string source)
    {
        var contract = executablePlan.Events.Values.Single(candidate => candidate.Name == name);
        return new(
            contract.Id,
            new SemanticTextValue(source),
            [.. contract.Properties.Select(property => new SemanticPropertyValue(
                property.Id, new SemanticTextValue(property.Name == "childId" ? "child" : "root")))]);
    }
}
