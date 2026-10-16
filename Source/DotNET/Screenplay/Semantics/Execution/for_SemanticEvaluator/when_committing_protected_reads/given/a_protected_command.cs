// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_committing_protected_reads.given;

public class a_protected_command : for_SemanticSpecificationRunner.given.a_v6_scenario
{
    protected const string Source = """
        command reads protected
        module M
          feature F
            slice StateChange Write
              event Recorded
                value String
              event Removed
              event OtherRecorded
                value String
              event OtherRemoved
              event Saved
                value String
              constraint UniqueValue
                unique value on Saved
              command Update
                id String identifier
                lookup String
                reads View by lookup
                validate
                  require View.value != ""
                    message "Value is empty."
                produces Saved
                  for id
                  value = View.value
              command NoFacts
                id String identifier
                lookup String
                reads View by lookup
                validate
                  require View.value != ""
                    message "Value is empty."
              command Merge
                id String identifier
                lookup String
                reads View as first by lookup
                reads OtherView as second by lookup
              command Record
                id String identifier
                value String
                produces Recorded
                  for id
                  value = value
              command Remove
                id String identifier
                produces Removed
                  for id
              command RecordOther
                id String identifier
                value String
                produces OtherRecorded
                  for id
                  value = value
              command RemoveOther
                id String identifier
                produces OtherRemoved
                  for id
            slice StateView Lookup
              readmodel View
                id String key
                value String
              projection Build => View
                from Recorded
                  id = $eventSourceId
                  value = value
                remove with Removed
              query Get => View optional
                by id String
              readmodel OtherView
                id String key
                value String
              projection BuildOther => OtherView
                from OtherRecorded
                  id = $eventSourceId
                  value = value
                remove with OtherRemoved
        """;

    protected readonly SemanticEvaluator _evaluator = new();
    protected SemanticWorld _world;
    protected SemanticExecutionResult _result;

    void Establish()
    {
        Compile(Source);
        _world = World(Fact("Recorded", "read", "original"));
    }

    protected SemanticExecutionRequest Request(string command = "Update", string lookup = "read", string id = "write")
    {
        var declaration = _plan.Commands.Values.Single(value => value.Name == command);
        return SemanticExecutionRequest.Create(
            declaration.Id,
            [new(declaration.Properties.Single(property => property.Name == "id").Id, SemanticValue.Text(id)),
             new(declaration.Properties.Single(property => property.Name == "lookup").Id, SemanticValue.Text(lookup))],
            []);
    }

    protected SemanticFact Fact(string name, string key, string? value = null)
    {
        var contract = _plan.Events.Values.Single(@event => @event.Name == name);
        return new(contract.Id, SemanticValue.Text(key), value is null ? [] : [new(contract.Properties.Single().Id, SemanticValue.Text(value))])
        {
            Context = new(new(SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text), SemanticValue.Text(key)))
        };
    }

    protected SemanticWorld World(params SemanticFact[] facts)
    {
        var result = _evaluator.EstablishWorld(_plan, [.. facts]);
        Assert.True(result is SemanticAccepted, result.ToString());
        return result.World;
    }

    protected SemanticWorld Append(SemanticWorld world, params SemanticFact[] facts)
    {
        var result = _evaluator.ExtendWorld(_plan, world, [.. facts]);
        Assert.True(result is SemanticAccepted, result.ToString());
        return result.World;
    }

    protected SemanticWorld InternalWorld(params SemanticFact[] facts)
    {
        var projected = SemanticEvaluator.Establish(_plan, [], [.. facts], out var readModels, out var failure);
        Assert.True(projected, failure);
        return SemanticWorld.Create([.. facts], readModels);
    }

    protected SemanticWorld InternalAppend(SemanticWorld world, SemanticFact fact)
    {
        var result = SemanticEvaluator.Append(_plan, world, fact, [], null);
        Assert.True(result is SemanticAccepted, result.ToString());
        return result.World;
    }
}
