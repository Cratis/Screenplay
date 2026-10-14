// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_checking_reducer_identity;

public class with_seeded_event_provenance : given.a_reducer_identity_model
{
    const string Seed = "\nseed\n  for \"seeded\"\n    Recorded\n      resource = \"seeded\"";

    [Fact]
    void should_not_claim_complete_query_provenance_when_a_seed_also_appends_the_event()
    {
        var result = new ScreenplayCompiler().Compile(Model + Seed);
        result.Value!.Seeds!.Single().Groups.Single().Events.Single().Event.ShouldEqual("Recorded");
        Warnings(Model).Length.ShouldEqual(1);
        Warnings(Model + Seed).ShouldBeEmpty();
    }

    [Fact]
    void should_not_claim_complete_reducer_provenance_when_a_seed_also_appends_the_event()
    {
        Warnings(Model + SecondCommand).Length.ShouldEqual(1);
        Warnings(Model + SecondCommand + Seed).ShouldBeEmpty();
    }
}
