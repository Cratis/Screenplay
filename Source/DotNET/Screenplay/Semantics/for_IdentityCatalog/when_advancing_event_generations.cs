// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_IdentityCatalog;

public class when_advancing_event_generations : Specification
{
    static readonly ApplicationIdentity Application = ApplicationIdentity.Parse("app1:20ccb167f2400bc55fae1597b1a0f4d19b40841f513bd013a7fa815e9e7f2994");
    static readonly SemanticAddress Slice = SemanticAddress.ForSlice(Application, "Projects", "Registration", "RegisterProject");
    static readonly SemanticAddress Event = SemanticAddress.ForEventContract(Slice, "ProjectRegistered");
    static readonly SemanticAddress Original = SemanticAddress.ForProperty(Event, "name");
    static readonly SemanticAddress First = SemanticAddress.ForEventProperty(Event, new(1), "name");
    static readonly SemanticAddress Second = SemanticAddress.ForEventProperty(Event, new(2), "name");
    static readonly SemanticAddress Third = SemanticAddress.ForEventProperty(Event, new(3), "name");

    [Fact]
    void should_keep_each_generation_identity_as_a_new_revision_arrives()
    {
        var initial = SemanticIdentityCatalog.Create(
            Application,
            [],
            [new(Original, SemanticId.Create(Original), SemanticIdentityOrigin.LegacyBootstrap),
             new(Event, SemanticId.Create(Event), SemanticIdentityOrigin.LegacyBootstrap)],
            [new(Event, EventContractId.CreateLegacy(Application, Event.Name), new(1), SemanticIdentityOrigin.LegacyBootstrap)]);
        var second = SemanticIdentityCatalog.PlanEventRevisionAdvancement(
            initial,
            initial.Revision,
            [],
            [Event, First, Second],
            [Event],
            [new(Event, new(2))]).Catalog;
        second.ResolveSemantic(First).ShouldEqual(initial.ResolveSemantic(Original));
        second.ResolveSemantic(Second).ShouldNotEqual(second.ResolveSemantic(First));
        second.ResolveSemanticAssignment(First).Origin.ShouldEqual(SemanticIdentityOrigin.Persisted);
        second.ResolveEventContract(Event).Revision.Value.ShouldEqual(2u);
        second.ResolveEventContract(Event).Id.ShouldEqual(initial.ResolveEventContract(Event).Id);

        var third = SemanticIdentityCatalog.PlanEventRevisionAdvancement(
            second,
            second.Revision,
            [],
            [Event, First, Second, Third],
            [Event],
            [new(Event, new(3))]).Catalog;
        third.ResolveSemantic(First).ShouldEqual(second.ResolveSemantic(First));
        third.ResolveSemantic(Second).ShouldEqual(second.ResolveSemantic(Second));
        third.ResolveSemantic(Third).ShouldNotEqual(second.ResolveSemantic(Second));
        third.ResolveEventContract(Event).Revision.Value.ShouldEqual(3u);
    }

    [Fact]
    void should_refuse_stale_or_backward_advancement()
    {
        var fresh = SemanticIdentityCatalog.Empty(Application);
        var second = SemanticIdentityCatalog.PlanEventRevisionAdvancement(
            fresh,
            fresh.Revision,
            [],
            [Event, First, Second],
            [Event],
            [new(Event, new(2))]).Catalog;
        second.ResolveEventContract(Event).Revision.Value.ShouldEqual(2u);
        second.Semantics.Select(value => value.Address).ShouldContain(First);
        second.Semantics.Select(value => value.Address).ShouldContain(Second);
        Catch.Exception(() => SemanticIdentityCatalog.PlanEventRevisionAdvancement(second, fresh.Revision, [], [Event, First, Second, Third], [Event], [new(Event, new(3))])).ShouldBeOfExactType<InvalidSemanticContract>();
        Catch.Exception(() => SemanticIdentityCatalog.PlanEventRevisionAdvancement(second, second.Revision, [], [Event, First], [Event], [new(Event, new(1))])).ShouldBeOfExactType<InvalidSemanticContract>();
    }
}
