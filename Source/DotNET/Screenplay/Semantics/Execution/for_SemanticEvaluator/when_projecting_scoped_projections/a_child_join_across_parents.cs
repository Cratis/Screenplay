// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_projecting_scoped_projections;

// Chronicle's child join matches existing children across parents (ProjectionFactory.SetupJoinsForChildren,
// ProjectionFactory.cs:509-522); one occurrence must reach both matching children, without creating a parent.
public class a_child_join_across_parents : Specification
{
    const string Source =
        """
        type Line
          lineId String
          updates Int?
        module Orders
          feature Ordering
            slice StateChange Events
              event OrderPlaced
                orderId String
              event LineAdded
                orderId String
                lineId String
              event LineUpdated
                lineId String
            slice StateView Lookup
              readmodel OrderView
                orderId String
                lines Line[]
              query OrderById => OrderView?
                by orderId String
              projection Orders => OrderView
                from OrderPlaced key orderId
                children lines identified by lineId
                  from LineAdded key lineId
                    parent orderId
                    count updates
                  join lineId on lineId
                    with LineUpdated
                      count updates
        """;

    ImmutableArray<SemanticReadModelInstance> _instances;
    string? _failure;
    SemanticId _lines;
    SemanticId _updates;

    void Because()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Orders"));
        const string StableKey = "child-join";
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument(StableKey), StableKey, "Orders.play", Source);
        var compilation = new SemanticModelCompiler().Compile("Orders", SemanticDocumentSet.Create([document], catalog));
        var model = compilation.Value?.Model ?? throw new InvalidOperationException(string.Join("; ", compilation.Diagnostics.Select(_ => _.Message)));
        var planCompilation = SemanticExecutionPlan.Compile(model);
        var plan = planCompilation.Plan ?? throw new InvalidOperationException(string.Join("; ", planCompilation.Issues.Select(_ => _.Details)));
        var events = plan.Events.Values.ToDictionary(_ => _.Name);
        var readModel = plan.ReadModels.Values.Single();
        _lines = readModel.Properties.Single(_ => _.Name == "lines").Id;
        _updates = plan.Model.Application.Types.Single(_ => _.Name == "Line").Properties.Single(_ => _.Name == "updates").Id;
        SemanticFact Fact(string name, string source, params (string Property, string Value)[] properties) =>
            new(events[name].Id, SemanticValue.Text(source), [.. properties.Select(property =>
                new SemanticPropertyValue(events[name].Properties.Single(_ => _.Name == property.Property).Id, SemanticValue.Text(property.Value)))]);
        SemanticEvaluator.Establish(
            plan,
            [],
            [
                Fact("OrderPlaced", "first", ("orderId", "first")),
                Fact("OrderPlaced", "second", ("orderId", "second")),
                Fact("LineAdded", "line", ("orderId", "first"), ("lineId", "line")),
                Fact("LineAdded", "line", ("orderId", "second"), ("lineId", "line")),
                Fact("LineUpdated", "line", ("lineId", "line"))
            ],
            out _instances,
            out _failure);
    }

    [Fact] void should_project() => _failure.ShouldBeNull();
    [Fact] void should_update_matching_children_under_both_parents() =>
        _instances.Select(instance => ((SemanticArrayValue)instance.Values.Single(_ => _.TargetProperty == _lines).Value).Values.Single())
            .All(child => SemanticValueRules.AreEqual(
                ((SemanticCompositeValue)child).Properties.Single(_ => _.TargetProperty == _updates).Value, SemanticValue.Number(2))).ShouldBeTrue();
    [Fact] void should_not_create_a_parent() => _instances.Length.ShouldEqual(2);
}
