// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner;

public class when_preserving_freshness_with_phase_three_values : given.a_v6_scenario
{
    const string Uuid = "00112233-4455-4677-8899-aabbccddeeff";
    const string ReuseMessage = "A generated event source already has facts; retry the command with a fresh generated value.";

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    void should_preserve_guarded_and_unguarded_reuse_independently_of_version_selection(bool phaseThree, bool guarded)
    {
        var generation = guarded ? "  generate uuid\nconcept Code : String\n  generate nanoid\n" : "";
        var phase = phaseThree ? "concept StoredReference : String\n  validate checksum luhn\n" : "";
        var fixture = guarded ? Uuid : "00112233-4455-7677-8899-aabbccddeeff";
        var then = guarded ? $"then error \"{ReuseMessage}\"" : $"then First\n          for \"{fixture}\"\n        then Second\n          for \"{fixture}\"";
        Compile($"concept Id : Uuid\n{generation}{phase}{Body(fixture, $"given Prior\n          for \"{fixture}\"", then)}");
        var legacyVersion = guarded ? SemanticVersion.V15 : SemanticVersion.V7;
        _plan.Model.SemanticVersion.ShouldEqual(phaseThree ? SemanticVersion.V21 : legacyVersion);
        var run = Run("Check");
        run.Passed.ShouldBeTrue();
        if (guarded) ((SemanticRejected)run.Execution).Category.ShouldEqual(SemanticRejectionCategory.Concurrency);
        else run.Execution.ShouldBeOfExactType<SemanticAccepted>();
        run.Execution.World.Facts.Length.ShouldEqual(guarded ? 1 : 3);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_compare_generated_string_identities_exactly_and_keep_before_first_scopes_immutable(bool sameSource)
    {
        const string Fixture = "ABCDEFGHIJKLMNOP";
        var prior = sameSource ? Fixture : "abcdefghijklmnop";
        var then = sameSource ? $"then error \"{ReuseMessage}\"" : $"then First\n          for \"{Fixture}\"\n        then Second\n          for \"{Fixture}\"";
        Compile($"concept Id : String\n  generate pattern \"?{{16}}\"\n{Body(Fixture, $"given Prior\n          for \"{prior}\"", then)}");
        var run = Run("Check");
        run.Passed.ShouldBeTrue();
        if (sameSource) ((SemanticRejected)run.Execution).Category.ShouldEqual(SemanticRejectionCategory.Concurrency);
        else ((SemanticAccepted)run.Execution).Facts.Length.ShouldEqual(2);
        run.Execution.World.Facts.Length.ShouldEqual(sameSource ? 1 : 3);
    }

    static string Body(string fixture, string given, string then) =>
        $"module M\n  feature F\n    slice StateChange S\n      event Prior\n      command RecordPrior\n        id Id identifier\n        produces Prior\n          for id\n      command Create\n        id Id generated identifier\n        produces event First\n        produces event Second\n      specification Check\n        {given}\n        when Create\n          for \"{fixture}\"\n        {then}";
}
