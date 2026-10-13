// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner;

public class when_running_generation_with_stored_event_names : given.a_v6_scenario
{
    const string UuidV4 = "00112233-4455-4677-8899-aabbccddeeff";
    const string UuidV7 = "00112233-4455-7677-8899-aabbccddeeff";
    const string ReuseMessage = "A generated event source already has facts; retry the command with a fresh generated value.";

    [Theory]
    [InlineData("")]
    [InlineData("  generate uuid\n")]
    void should_preserve_unguarded_reuse_and_legacy_uuid_fixture_shape_when_a_pin_selects_v16(string declaration)
    {
        foreach (var pinned in new[] { false, true })
        {
            Compile(Source(declaration, false, pinned, UuidV7, true, $"then Created\n          for \"{UuidV7}\""));
            _plan.Model.SemanticVersion.ShouldEqual(pinned ? SemanticVersion.V16 : SemanticVersion.V7);
            var run = Run("Check");
            run.Passed.ShouldBeTrue();
            run.Execution.ShouldBeOfExactType<SemanticAccepted>();
            run.Execution.World.Facts.Length.ShouldEqual(2);
        }
    }

    [Theory]
    [InlineData("", UuidV4)]
    [InlineData("  generate uuid\n", UuidV4)]
    [InlineData("  generate uuidv7\n", UuidV7)]
    void should_preserve_guarded_freshness_when_a_pin_selects_v16(string declaration, string fixture)
    {
        foreach (var pinned in new[] { false, true })
        {
            Compile(Source(declaration, true, pinned, fixture, true, $"then error \"{ReuseMessage}\""));
            _plan.Model.SemanticVersion.ShouldEqual(pinned ? SemanticVersion.V16 : SemanticVersion.V15);
            var run = Run("Check");
            run.Passed.ShouldBeTrue();
            ((SemanticRejected)run.Execution).Category.ShouldEqual(SemanticRejectionCategory.Concurrency);
            run.Execution.World.Facts.Length.ShouldEqual(1);
        }
    }

    [Theory]
    [InlineData("", UuidV7)]
    [InlineData("  generate uuid\n", UuidV7)]
    [InlineData("  generate uuidv7\n", UuidV4)]
    void should_preserve_exact_uuid_fixture_shape_checks_when_a_pin_selects_v16(string declaration, string fixture)
    {
        foreach (var pinned in new[] { false, true })
        {
            Compile(Source(declaration, true, pinned, fixture, false, "then error"));
            _plan.Model.SemanticVersion.ShouldEqual(pinned ? SemanticVersion.V16 : SemanticVersion.V15);
            var run = Run("Check");
            run.Passed.ShouldBeTrue();
            ((SemanticRejected)run.Execution).Category.ShouldEqual(SemanticRejectionCategory.Contract);
            run.Execution.World.Facts.ShouldBeEmpty();
        }
    }

    static string Source(string declaration, bool usesGeneration, bool pinned, string fixture, bool prior, string then)
    {
        var strategy = usesGeneration ? "concept Code : String\n  generate nanoid\n" : "";
        var pin = pinned ? "        id \"StoredCreated\"\n" : "";
        var given = prior ? $"given Prior\n          for \"{fixture}\"" : "";

        return $"concept Id : Uuid\n{declaration}{strategy}module M\n  feature F\n    slice StateChange S\n      event Created\n{pin}      event Prior\n      command RecordPrior\n        id Id identifier\n        produces Prior\n          for id\n      command Create\n        id Id generated identifier\n        produces Created\n          for id\n      specification Check\n        {given}\n        when Create\n          for \"{fixture}\"\n        {then}";
    }
}
