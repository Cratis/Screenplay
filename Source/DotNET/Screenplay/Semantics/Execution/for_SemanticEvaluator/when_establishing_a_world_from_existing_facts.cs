// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator;

public class when_establishing_a_world_from_existing_facts : Specification
{
    const string First = "00000000-0000-0000-0000-000000000002";
    const string Second = "00000000-0000-0000-0000-000000000001";
    const string Source =
        """
        module Projects
          feature Registration
            slice StateChange RegisterProject
              command RegisterProject
                projectId Uuid identifier
                name String
                produces ProjectRegistered
                  for projectId
                  name = name
              event ProjectRegistered
                name String
              specification ProjectsAlreadyRegistered
                given ProjectRegistered
                  for "00000000-0000-0000-0000-000000000002"
                  name = "Second"
                given ProjectRegistered
                  for "00000000-0000-0000-0000-000000000001"
                  name = "First"
                then readmodel ProjectSummary
                  projectId = "00000000-0000-0000-0000-000000000001"
                  name = "First"
                then readmodel ProjectSummary
                  projectId = "00000000-0000-0000-0000-000000000002"
                  name = "Second"
            slice StateView ProjectLookup
              readmodel ProjectSummary
                projectId Uuid
                name String
              query ProjectById => ProjectSummary?
                by projectId Uuid
              projection ProjectSummaryProjection => ProjectSummary
                from ProjectRegistered
                  projectId = $eventSourceId
                  name = name
        """;

    SemanticAccepted _established = null!;
    SemanticAccepted _reversed = null!;
    SemanticSpecificationRun _specification = null!;
    SemanticExecutionResult _lookup = null!;
    SemanticExecutionPlan _plan = null!;
    ImmutableArray<SemanticFact> _facts;

    void Because()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("existing-facts"), "existing-facts", "Projects.play", Source);
        var compilation = new SemanticModelCompiler().Compile("Projects", SemanticDocumentSet.Create([document], catalog));
        var plan = SemanticExecutionPlan.Compile(compilation.Value!.Model).Plan!;
        _plan = plan;
        var specification = plan.Specifications.Values.Single();
        _facts = [.. specification.GivenEvents.Select(given => new SemanticFact(
            given.EventContract, given.EventSource!.Value, given.Values) { Context = new(given.EventSource) })];
        var evaluator = new SemanticEvaluator();
        _established = (SemanticAccepted)evaluator.EstablishWorld(plan, _facts);
        _reversed = (SemanticAccepted)evaluator.EstablishWorld(plan, [.. _facts.Reverse()]);
        _specification = new SemanticSpecificationRunner().Run(plan, specification.Id);
        _lookup = evaluator.Execute(plan, _established.World, SemanticExecutionRequest.ForQueries(
            [new(plan.Queries.Values.Single().Id, SemanticValue.Text(First))]));
    }

    [Fact] void should_pass_the_equivalent_given_events_specification() => _specification.Passed.ShouldBeTrue();
    [Fact] void should_keep_both_facts_in_occurrence_order() => _established.World.Facts.Select(fact => fact.Destination).ShouldEqual(_facts.Select(fact => fact.Destination));
    [Fact] void should_keep_typed_occurrence_context() => _established.World.Facts.All(fact => fact.Context is not null).ShouldBeTrue();
    [Fact] void should_project_the_same_keyed_world_as_given_events()
    {
        var expected = _specification.Execution.World;
        _established.World.Facts.Length.ShouldEqual(expected.Facts.Length);
        _established.World.ReadModels.Length.ShouldEqual(expected.ReadModels.Length);
        foreach (var (actual, other) in _established.World.ReadModels.Zip(expected.ReadModels))
        {
            actual.ReadModel.ShouldEqual(other.ReadModel);
            SemanticValueRules.AreEqual(actual.Key, other.Key).ShouldBeTrue();
            actual.Values.All(value => other.Values.Any(candidate =>
                candidate.TargetProperty == value.TargetProperty && SemanticValueRules.AreEqual(candidate.Value, value.Value))).ShouldBeTrue();
        }
    }
    [Fact] void should_resolve_a_read_only_query_against_the_established_world()
    {
        _lookup.ShouldBeOfExactType<SemanticAccepted>();
        ((SemanticAccepted)_lookup).Queries.Single().Results.Single().Key.ShouldEqual(SemanticValue.Text(First));
    }
    [Fact] void should_apply_successive_occurrences_on_one_key_in_order()
    {
        var sameKey = _facts[1] with { Destination = _facts[0].Destination, Context = _facts[0].Context };
        var evaluator = new SemanticEvaluator();
        var forward = (SemanticAccepted)evaluator.EstablishWorld(_plan, [_facts[0], sameKey]);
        var backward = (SemanticAccepted)evaluator.EstablishWorld(_plan, [sameKey, _facts[0]]);
        forward.World.ReadModels.Single().Values.Any(value => SemanticValueRules.AreEqual(value.Value, SemanticValue.Text("First"))).ShouldBeTrue();
        backward.World.ReadModels.Single().Values.Any(value => SemanticValueRules.AreEqual(value.Value, SemanticValue.Text("Second"))).ShouldBeTrue();
    }
    [Fact] void should_sort_read_models_independently_of_fact_order()
    {
        _established.World.ReadModels.Select(instance => instance.Key).ShouldEqual(_reversed.World.ReadModels.Select(instance => instance.Key));
        SemanticValueRules.AreEqual(_established.World.ReadModels[0].Key, SemanticValue.Text(Second)).ShouldBeTrue();
        SemanticValueRules.AreEqual(_established.World.ReadModels[1].Key, SemanticValue.Text(First)).ShouldBeTrue();
    }
}
