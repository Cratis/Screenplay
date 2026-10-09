// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_resolving_event_routes;

// Chronicle EventSequence.cs:406-407,773-781 stores routing and defaults empty ids; EventSources.cs:30-36,86-90
// rejects duplicate declarations. Arc CommandPipeline.cs:481 computes ids before authorization: the portable
// phase deliberately differs, preserving authorization and validation ahead of formatter-only failures (0036).
public class when_executing_routed_commands : given.event_route_models
{
    [Fact]
    void should_route_every_fact_without_changing_its_destination_or_response()
    {
        var command = Command with { Produces = [Command.Produces[0], Command.Produces[0]] };
        var result = Execute(Plan(command), SemanticValue.Text("p-1:2026-10"));
        result.ShouldBeOfExactType<SemanticAccepted>();
        var accepted = (SemanticAccepted)result;
        accepted.Facts.Length.ShouldEqual(2);
        foreach (var fact in accepted.Facts)
        {
            fact.Route.ShouldEqual(new SemanticEventRoute("stored-project", "stored-ledger", "p-1:2026-10"));
            fact.Destination.ShouldEqual(SemanticValue.Text("project-1"));
        }
        ((SemanticScalarExecutionResponse)accepted.Response!).Value.ShouldEqual(SemanticValue.Text("project-1"));
    }

    [Fact]
    void should_encode_uuid_and_adjacent_integer_keys_at_both_bounds()
    {
        var uuid = ExecuteScalar(Uuid, SemanticValue.Text("3fa85f64-5717-4562-b3fc-2c963f66afa6"));
        uuid.Facts[0].Route!.StreamId.ShouldEqual("3fa85f64-5717-4562-b3fc-2c963f66afa6");
        foreach (var number in new[] { -9007199254740991m, -9007199254740990m, 9007199254740990m, 9007199254740991m })
        {
            ExecuteScalar(Integer, SemanticValue.Number(number)).Facts[0].Route!.StreamId.ShouldEqual(number.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }
    }

    [Fact]
    void should_leave_unkeyed_routes_without_an_id_and_unrouted_commands_without_a_route()
    {
        var unkeyed = Stream with { StreamIdType = null };
        var plan = Plan(Command with { Route = new(Source.Id, Stream.Id) }, Source with { Streams = [unkeyed] });
        ((SemanticAccepted)Execute(plan, SemanticValue.Text("ignored"))).Facts[0].Route.ShouldEqual(new SemanticEventRoute("stored-project", "stored-ledger", null));
        ((SemanticAccepted)Execute(Plan(Command with { Route = null }), SemanticValue.Text("ignored"))).Facts[0].Route.ShouldBeNull();
    }

    [Fact]
    void should_resolve_literal_routes_and_composite_parts_in_declaration_order()
    {
        var literal = Command with { Route = Command.Route! with { StreamId = SemanticExpression.FromValue(SemanticValue.Text("literal")) } };
        ((SemanticAccepted)Execute(Plan(literal), SemanticValue.Text("unused"))).Facts[0].Route!.StreamId.ShouldEqual("literal");
        var stream = Stream with { StreamIdType = null, StreamIdParts = [new("period", Text), new("project", Uuid)] };
        var command = Command with
        {
            Route = new(Source.Id, Stream.Id)
            {
                StreamIdParts = [new("period", Property(Key)), new("project", SemanticExpression.FromValue(SemanticValue.Text("3FA85F64-5717-4562-B3FC-2C963F66AFA6")))]
            }
        };
        ((SemanticAccepted)Execute(Plan(command, Source with { Streams = [stream] }), SemanticValue.Text("a|b%"))).Facts[0].Route!.StreamId.ShouldEqual("a%7Cb%25|3fa85f64-5717-4562-b3fc-2c963f66afa6");
        AssertRejected(Execute(Plan(command, Source with { Streams = [stream] }), SemanticValue.Text(string.Empty)), SemanticRejectionCategory.Contract,
            SemanticStreamIdFormatter.FailureMessage(StreamIdFormatFailure.Empty));
    }

    [Fact]
    void should_keep_authorization_ahead_of_an_unformattable_id()
    {
        AssertRejected(Execute(Plan(Command with { Authorization = new SemanticPolicyReference("Authenticated") }), SemanticValue.Text(string.Empty)),
            SemanticRejectionCategory.Unauthorized, "Caller is not authorized.");
    }

    [Fact]
    void should_keep_validation_and_requirements_ahead_of_an_unformattable_id()
    {
        var validation = Command with { Validations = [new(Key.Id, SemanticValidationRuleKind.NotEmpty, null, "key required")] };
        AssertRejected(Execute(Plan(validation), SemanticValue.Text(string.Empty)), SemanticRejectionCategory.Validation, "key required");
        var requirement = Command with
        {
            Requirements = [new(new SemanticComparison(new(Key.Id, null), SemanticComparisonOperator.NotEqual,
                new(default, SemanticValue.Text(string.Empty))), "requirement failed")]
        };
        AssertRejected(Execute(Plan(requirement), SemanticValue.Text(string.Empty)), SemanticRejectionCategory.Validation, "requirement failed");
    }

    [Fact]
    void should_reject_non_nfc_at_request_validation_before_declarative_rules()
    {
        var command = Command with { Validations = [new(Key.Id, SemanticValidationRuleKind.Equal, SemanticValue.Text("allowed"), "rule failed")] };
        var result = Execute(Plan(command), SemanticValue.Text("e\u0301"));
        result.ShouldBeOfExactType<SemanticRejected>();
        var rejection = (SemanticRejected)result;
        rejection.Category.ShouldEqual(SemanticRejectionCategory.Contract);
        rejection.Details.ShouldEqual("Canonical JSON field 'semantic text value' must use Unicode NFC text.");
    }

    [Fact]
    void should_fail_atomically_at_the_route_phase_before_missing_generation_or_allocation()
    {
        var generated = new SemanticProperty(SemanticId.Create(SemanticAddress.ForProperty(CommandAddress, "generated")), "generated", SemanticTypeReference.ForConcept(GeneratedId), false) { IsGenerated = true };
        foreach (var command in new[]
        {
            Command,
            Command with { Properties = [Identity, Key, generated] },
            Command with { Destination = null, Produces = [new(Event.Id, null, null, [])] }
        })
        {
            AssertRejected(Execute(Plan(command), SemanticValue.Text(string.Empty)), SemanticRejectionCategory.Contract,
                SemanticStreamIdFormatter.FailureMessage(StreamIdFormatFailure.Empty));
        }
        var property = Key with { Type = Integer };
        AssertRejected(Execute(Plan(Command with { Properties = [Identity, property] }, Source with { Streams = [Stream with { StreamIdType = Integer }] }), SemanticValue.Number(9007199254740992m)),
            SemanticRejectionCategory.Contract, SemanticStreamIdFormatter.FailureMessage(StreamIdFormatFailure.OutOfRange));
    }

    [Fact]
    void should_apply_routes_to_invoked_commands_but_not_direct_reaction_productions()
    {
        var plan = ReactionPlan("2026-10");
        var loop = new SemanticReactionLoop(new SemanticEvaluator(), plan, SemanticWorld.Empty);
        loop.Append(Fact(new("stored-project", "stored-ledger", "cause"), Before.Id), null).ShouldBeNull();
        loop.Settle().ShouldBeNull();
        loop.Facts.Length.ShouldEqual(3);
        loop.Facts[1].Route.ShouldBeNull();
        loop.Facts[2].Route.ShouldEqual(new SemanticEventRoute("stored-project", "stored-ledger", "2026-10"));
    }

    [Fact]
    void should_keep_earlier_cascade_facts_when_an_invoked_route_fails()
    {
        var plan = ReactionPlan(string.Empty);
        var loop = new SemanticReactionLoop(new SemanticEvaluator(), plan, SemanticWorld.Empty);
        loop.Append(Fact(null, Before.Id), null).ShouldBeNull();
        var failure = loop.Settle();
        failure.ShouldBeOfExactType<SemanticRejected>();
        var result = (SemanticRejected)failure!;
        result.Category.ShouldEqual(SemanticRejectionCategory.Contract);
        result.Details.ShouldEqual(SemanticStreamIdFormatter.FailureMessage(StreamIdFormatFailure.Empty));
        result.World.Facts.Length.ShouldEqual(2);
        result.World.Facts[1].Route.ShouldBeNull();
    }

    static SemanticExecutionPlan ReactionPlan(string key)
    {
        var trigger = new SemanticReactionTrigger(SemanticReactionTriggerKind.Event)
        {
            Source = Before.Id,
            Produces = [new(Event.Id, null, null, [])],
            Invokes = [new(Command.Id, [new(Identity.Id, SemanticExpression.FromValue(SemanticValue.Text("project-1"))), new(Key.Id, SemanticExpression.FromValue(SemanticValue.Text(key)))])]
        };
        var reaction = new SemanticReaction(SemanticId.Create(SemanticAddress.ForReaction(AutomationAddress, "Follow")), "Follow", [trigger]);

        return Plan(reactions: [reaction]);
    }

    static SemanticAccepted ExecuteScalar(SemanticTypeReference type, SemanticValue value) =>
        (SemanticAccepted)Execute(Plan(Command with { Properties = [Identity, Key with { Type = type }] }, Source with { Streams = [Stream with { StreamIdType = type }] }), value);

    static SemanticExecutionResult Execute(SemanticExecutionPlan plan, SemanticValue key) => new SemanticEvaluator().Execute(plan, SemanticWorld.Empty, Request(key));

    static void AssertRejected(SemanticExecutionResult result, SemanticRejectionCategory category, string message)
    {
        result.ShouldBeOfExactType<SemanticRejected>();
        var rejected = (SemanticRejected)result;
        rejected.Category.ShouldEqual(category);
        rejected.Details.ShouldEqual(message);
        ReferenceEquals(result.World, SemanticWorld.Empty).ShouldBeTrue();
    }
}
