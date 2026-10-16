// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator;

public class when_comparing_command_uuids : for_SemanticSpecificationRunner.given.a_v6_scenario
{
    const string Owner = "a1111111-1111-4111-8111-111111111111";
    const string Other = "b2222222-2222-4222-8222-222222222222";
    const string Flagged = "c3333333-3333-4333-8333-333333333333";

    const string Source = """
        command reads protected
        concept MemberId : Uuid
        module M
          feature F
            slice StateChange Write
              event Recorded
                memberId MemberId
              event Removed
              event Saved
              event Flagged
              command Update
                id String identifier
                lookup String
                requestedBy MemberId
                approvedBy MemberId
                reads View by lookup
                validate
                  require View.memberId == requestedBy
                    message "Not the owner."
                  require requestedBy != approvedBy
                    message "Same person."
                produces Saved
                  for id
                produces when approvedBy == "c3333333-3333-4333-8333-333333333333"
                  Flagged
                    for id
            slice StateView Lookup
              readmodel View
                id String key
                memberId MemberId
              projection Build => View
                from Recorded
                  id = $eventSourceId
                  memberId = memberId
                remove with Removed
              query Get => View optional
                by id String
        """;

    SemanticWorld _world;
    SemanticExecutionResult _accepted;
    SemanticExecutionResult _flagged;
    SemanticExecutionResult _notOwner;
    SemanticExecutionResult _samePerson;
    SemanticExecutionResult _nonCanonical;

    void Establish()
    {
        Compile(Source);
        var recorded = _plan.Events.Values.Single(@event => @event.Name == "Recorded");
        var fact = new SemanticFact(recorded.Id, SemanticValue.Text("read"), [new(recorded.Properties.Single().Id, SemanticValue.Text(Owner))])
        {
            Context = new(new(SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text), SemanticValue.Text("read")))
        };
        var projected = SemanticEvaluator.Establish(_plan, [], [fact], out var readModels, out var failure);
        Assert.True(projected, failure);
        _world = SemanticWorld.Create([fact], readModels);
    }

    void Because()
    {
        _accepted = Execute(Owner, Other);
        _flagged = Execute(Owner, Flagged);
        _notOwner = Execute(Other, Owner);
        _samePerson = Execute(Owner, Owner);
        _nonCanonical = Execute(Owner.ToUpperInvariant(), Other);
    }

    [Fact] void should_accept_when_the_read_path_equals_the_property_and_the_properties_differ() => (_accepted is SemanticAccepted { Facts.Length: 1 }).ShouldBeTrue();
    [Fact] void should_produce_the_guarded_event_when_the_property_equals_the_literal() => (_flagged is SemanticAccepted { Facts.Length: 2 }).ShouldBeTrue();
    [Fact] void should_reject_when_the_read_path_differs_from_the_property() => (_notOwner is SemanticRejected { Category: SemanticRejectionCategory.Validation }).ShouldBeTrue();
    [Fact] void should_reject_when_the_not_equal_requirement_fails() => (_samePerson is SemanticRejected { Category: SemanticRejectionCategory.Validation }).ShouldBeTrue();
    [Fact] void should_reject_a_non_canonical_input_before_any_requirement_runs() => (_nonCanonical is SemanticRejected { Category: SemanticRejectionCategory.Contract }).ShouldBeTrue();

    SemanticExecutionResult Execute(string requestedBy, string approvedBy)
    {
        var command = _plan.Commands.Values.Single(value => value.Name == "Update");
        SemanticPropertyValue Value(string name, string value) => new(command.Properties.Single(property => property.Name == name).Id, SemanticValue.Text(value));
        var request = SemanticExecutionRequest.Create(command.Id, [Value("id", "write"), Value("lookup", "read"), Value("requestedBy", requestedBy), Value("approvedBy", approvedBy)], []);

        return new SemanticEvaluator().Execute(_plan, _world, request);
    }
}
