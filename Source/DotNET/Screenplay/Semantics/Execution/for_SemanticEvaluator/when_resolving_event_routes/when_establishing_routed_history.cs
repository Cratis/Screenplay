// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_resolving_event_routes;

public class when_establishing_routed_history : given.event_route_models
{
    [Fact]
    void should_accept_history_without_a_producer_and_with_a_conflicting_producer()
    {
        var fact = Fact(new("stored-project", "stored-ledger", "period"));
        new SemanticEvaluator().EstablishWorld(Plan(noCommands: true), [fact]).ShouldBeOfExactType<SemanticAccepted>();
        var otherIdentifier = Identity with { Type = Uuid };
        var command = Command with { Properties = [otherIdentifier, Key], Destination = new(Uuid, Property(otherIdentifier)), Response = new SemanticScalarCommandResponse(Identity.Id, Uuid), Route = null };
        new SemanticEvaluator().EstablishWorld(Plan(command), [fact]).ShouldBeOfExactType<SemanticAccepted>();
        new SemanticEvaluator().EstablishWorld(Plan(Command, Source with { IdentifierType = null }), [fact]).ShouldBeOfExactType<SemanticAccepted>();
    }

    [Fact]
    void should_reject_unknown_stored_names_noncanonical_keys_and_mistyped_sources()
    {
        AssertRejected(Plan(), Fact(new("foreign", "stored-ledger", "period")));
        AssertRejected(Plan(), Fact(new("stored-project", "foreign", "period")));
        AssertRejected(Plan(), Fact(new("stored-project", "stored-ledger", null)));
        AssertRejected(Plan(), Fact(new("stored-project", "stored-ledger", string.Empty)));
        var integer = Source with { Streams = [Stream with { StreamIdType = Integer }] };
        foreach (var key in new[] { "01", "-0", "9007199254740992" }) AssertRejected(Plan(Command with { Route = null }, integer), Fact(new("stored-project", "stored-ledger", key)));
        var uuid = Source with { Streams = [Stream with { StreamIdType = Uuid }] };
        AssertRejected(Plan(Command with { Route = null }, uuid), Fact(new("stored-project", "stored-ledger", "3FA85F64-5717-4562-B3FC-2C963F66AFA6")));
        AssertRejected(Plan(), Fact(new("stored-project", "stored-ledger", "period")) with { Context = new(new(Uuid, SemanticValue.Text("3fa85f64-5717-4562-b3fc-2c963f66afa6"))) });
        AssertRejected(Plan(), Fact(new("stored-project", "stored-ledger", "period")) with { Context = null });
    }

    [Fact]
    void should_require_a_canonical_composite_under_its_declared_schema()
    {
        var source = Source with { Streams = [Stream with { StreamIdType = null, StreamIdParts = [new("first", Text), new("second", Integer)] }] };
        var plan = Plan(Command with { Route = null }, source);
        new SemanticEvaluator().EstablishWorld(plan, [Fact(new("stored-project", "stored-ledger", "a%7Cb|1"))]).ShouldBeOfExactType<SemanticAccepted>();
        foreach (var key in new[] { "a%7cb|1", "a%|1", "a|01", "a|1|extra", "a|", "e\u0301|1" }) AssertRejected(plan, Fact(new("stored-project", "stored-ledger", key)));
    }

    [Fact]
    void should_require_no_key_for_unkeyed_streams_and_reject_routes_before_admission()
    {
        var source = Source with { Streams = [Stream with { StreamIdType = null }] };
        var plan = Plan(Command with { Route = new(Source.Id, Stream.Id) }, source);
        new SemanticEvaluator().EstablishWorld(plan, [Fact(new("stored-project", "stored-ledger", null))]).ShouldBeOfExactType<SemanticAccepted>();
        AssertRejected(plan, Fact(new("stored-project", "stored-ledger", "unexpected")));
        AssertRejected(Plan(Command with { Route = null }, legacy: true), Fact(new("stored-project", "stored-ledger", "period")));
    }

    static void AssertRejected(SemanticExecutionPlan plan, SemanticFact fact)
    {
        var result = new SemanticEvaluator().EstablishWorld(plan, [fact]);
        result.ShouldBeOfExactType<SemanticRejected>();
        ((SemanticRejected)result).Category.ShouldEqual(SemanticRejectionCategory.Contract);
        ReferenceEquals(result.World, SemanticWorld.Empty).ShouldBeTrue();
    }
}
