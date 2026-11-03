// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_projecting_scoped_projections.given;

public class a_nested_join_projection : for_SemanticModelBinder.given.a_semantic_binder
{
    protected const string NestedJoin = """
        from Created
          label = reference
        nested details
          from Created
            reference = reference
          join linked on reference
            with Joined
              value = value
        """;

    protected const string ChildFrom = """
        from Created
          label = reference
        children items identified by id
          from Attached key itemId
            parent rootId
            id = itemId
          nested details
            from Attached key itemId
              parent rootId
              reference = reference
        """;

    protected SemanticCompilation _bound;
    protected SemanticExecutionPlanCompilation _compilation;
    protected ImmutableArray<SemanticReadModelInstance> _instances;
    protected string? _failure;

    protected SemanticApplication Application => _bound.Model.Application;
    protected SemanticProjection Projection => Application.Modules.SelectMany(module => module.Features)
        .SelectMany(feature => feature.Slices).SelectMany(slice => slice.Projections).Single();

    protected void Plan(string body)
    {
        var result = Bind("""
            type Details
              reference String
              value String?
              children DetailChild[]?
            type DetailChild
              id String
            type Item
              id String
              value String?
              details Details?
            type Envelope
              details Details?
            module Example
              feature Example
                slice StateChange Events
                  event Created
                    reference String
                  event Attached
                    rootId String
                    itemId String
                    reference String
                  event Joined
                    value String
                  event Cleared
                    reference String
                slice StateView Lookup
                  readmodel View
                    id String
                    label String?
                    value String?
                    details Details?
                    envelope Envelope?
                    items Item[]
                  query ViewById => View?
                    by id String
                  projection Example => View
            """ + "\n" + string.Join('\n', body.Split('\n').Select(line => $"        {line}")));
        result.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}").ShouldBeEmpty();
        _bound = result.Value!;
        _compilation = SemanticExecutionPlan.Compile(_bound.Model);
    }

    protected void Project(params SemanticFact[] facts) =>
        SemanticEvaluator.Establish(_compilation.Plan!, [], [.. facts], out _instances, out _failure);

    protected SemanticFact Fact(string name, string source, params (string Property, string Value)[] values)
    {
        var contract = Application.Modules.SelectMany(module => module.Features).SelectMany(feature => feature.Slices)
            .SelectMany(slice => slice.Events).Single(contract => contract.Name == name);
        return new(contract.Id, SemanticValue.Text(source), [.. values.Select(value =>
            new SemanticPropertyValue(contract.Properties.Single(property => property.Name == value.Property).Id, SemanticValue.Text(value.Value)))]);
    }

    protected SemanticValue Value(string source, string property) =>
        _instances.Single(instance => SemanticValueRules.AreEqual(instance.Key, SemanticValue.Text(source))).Values
            .Single(value => value.TargetProperty == Application.Modules.SelectMany(module => module.Features).SelectMany(feature => feature.Slices)
                .SelectMany(slice => slice.ReadModels).Single().Properties.Single(target => target.Name == property).Id).Value;

    protected SemanticValue Member(SemanticValue value, string type, string property) =>
        ((SemanticCompositeValue)value).Properties.Single(member => member.TargetProperty == MemberId(type, property)).Value;

    // A composite omits members that were never set rather than carrying a null value.
    protected bool HasMember(SemanticValue value, string type, string property) =>
        ((SemanticCompositeValue)value).Properties.Any(member => member.TargetProperty == MemberId(type, property));

    SemanticId MemberId(string type, string property) =>
        Application.Types.Single(target => target.Name == type).Properties.Single(target => target.Name == property).Id;
}
