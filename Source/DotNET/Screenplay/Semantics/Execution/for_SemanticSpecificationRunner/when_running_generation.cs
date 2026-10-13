// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner;

public class when_running_generation : given.a_v6_scenario
{
    const string Identity = "00112233-4455-7677-8899-aabbccddeeff";
    const string ReuseMessage = "A generated event source already has facts; retry the command with a fresh generated value.";

    [Fact]
    void should_accept_several_initial_facts_for_one_generated_source()
    {
        Compile(Source("", $"then Started\n          for \"{Identity}\"\n        then Initialized\n          for \"{Identity}\""));
        var run = Run("Check");
        run.Passed.ShouldBeTrue();
        ((SemanticAccepted)run.Execution).Facts.Length.ShouldEqual(2);
    }

    [Theory]
    [InlineData("Started")]
    [InlineData("Unrelated")]
    void should_reject_any_existing_fact_on_the_generated_source_atomically(string existing)
    {
        Compile(Source($"given {existing}\n          for \"{Identity}\"", $"then error \"{ReuseMessage}\""));
        var run = Run("Check");
        run.Passed.ShouldBeTrue();
        ((SemanticRejected)run.Execution).Category.ShouldEqual(SemanticRejectionCategory.Concurrency);
        run.Execution.World.Facts.Length.ShouldEqual(1);
    }

    [Fact]
    void should_reject_existing_facts_from_an_unrelated_stream()
    {
        Compile($"concept Id : Uuid\n  generate uuidv7\neventsource Records\n  identifier Id\n  stream History\n  stream Initial\nmodule M\n  feature F\n    slice StateChange S\n      event Prior\n      command RecordPrior\n        id Id identifier\n        stream Records.History\n        produces Prior\n          for id\n      command Create\n        id Id generated identifier\n        stream Records.Initial\n        produces event Created\n      specification Check\n        given Prior\n          for \"{Identity}\"\n          stream Records.History\n        when Create\n          for \"{Identity}\"\n        then error \"{ReuseMessage}\"");
        var run = Run("Check");
        run.Passed.ShouldBeTrue();
        ((SemanticRejected)run.Execution).Category.ShouldEqual(SemanticRejectionCategory.Concurrency);
        run.Execution.World.Facts.Length.ShouldEqual(1);
    }

    [Fact]
    void should_ignore_unrelated_sources()
    {
        Compile(Source("given Unrelated\n          for \"00112233-4455-7677-8899-aabbccddeefa\"", $"then Started\n          for \"{Identity}\"\n        then Initialized\n          for \"{Identity}\""));
        Run("Check").Passed.ShouldBeTrue();
    }

    [Fact]
    void should_ignore_unrouted_prior_facts()
    {
        Compile(Source("given Unrelated", $"then Started\n          for \"{Identity}\"\n        then Initialized\n          for \"{Identity}\""));
        _plan.Model.SemanticVersion.IsAtLeast(SemanticVersion.V15).ShouldBeTrue();
        Run("Check").Passed.ShouldBeTrue();
    }

    [Fact]
    void should_not_claim_freshness_for_skipped_productions()
    {
        var source = Source($"given Unrelated\n          for \"{Identity}\"", "then returns \"" + Identity + "\"")
            .Replace("      event Unrelated", "      event Unrelated\n      event Started\n      event Initialized", StringComparison.Ordinal)
            .Replace("produces event Started", "produces when enabled == true\n          Started\n            for id", StringComparison.Ordinal)
            .Replace("produces event Initialized", "produces when enabled == true\n          Initialized\n            for id", StringComparison.Ordinal)
            .Replace("id Id generated identifier", "id Id generated identifier\n        enabled Bool", StringComparison.Ordinal)
            .Replace("      specification Check", "        returns id\n      specification Check", StringComparison.Ordinal)
            .Replace($"for \"{Identity}\"\n        then", $"for \"{Identity}\"\n          enabled = false\n        then", StringComparison.Ordinal);
        Compile(source);
        Run("Check").Passed.ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("  generate uuid\n")]
    void should_keep_default_uuid_models_unguarded_and_allow_any_canonical_uuid(string declaration)
    {
        Compile(Source($"given Unrelated\n          for \"{Identity}\"", $"then Started\n          for \"{Identity}\"\n        then Initialized\n          for \"{Identity}\"")
            .Replace("  generate uuidv7\n", declaration, StringComparison.Ordinal));
        _plan.Model.SemanticVersion.IsAtLeast(SemanticVersion.V15).ShouldBeFalse();
        Run("Check").Passed.ShouldBeTrue();
    }

    [Fact]
    void should_apply_v15_shape_checks_to_implicit_uuid_fixtures()
    {
        Compile(Source("", "then error")
            .Replace("  generate uuidv7", "concept Code : String\n  generate nanoid", StringComparison.Ordinal));
        ((SemanticRejected)Run("Check").Execution).Category.ShouldEqual(SemanticRejectionCategory.Contract);
    }

    [Theory]
    [InlineData("nanoid 21", "ABCDEFGHIJKLMNOPQRSTU", true)]
    [InlineData("nanoid 21", "too-short", false)]
    [InlineData("bytes 1 base64url", "AA", true)]
    [InlineData("bytes 1 base64url", "AB", false)]
    [InlineData("typeid \"\"", "0000000000e008000000000000", true)]
    [InlineData("uuid case upper prefix \"ref_\"", "ref_00112233-4455-4677-8899-AABBCCDDEEFF", true)]
    void should_validate_string_generation_fixtures_in_source_specifications(string strategy, string fixture, bool valid)
    {
        var assertion = valid ? $"then returns \"{fixture}\"" : "then error";
        Compile($"concept Value : String\n  generate {strategy}\nmodule M\n  feature F\n    slice StateChange S\n      command Create\n        value Value generated\n        returns value\n      specification Check\n        when Create\n          generated value = \"{fixture}\"\n        {assertion}");
        var run = Run("Check");
        run.Passed.ShouldBeTrue();
        if (!valid) ((SemanticRejected)run.Execution).Category.ShouldEqual(SemanticRejectionCategory.Contract);
    }

    static string Source(string given, string then) =>
        $"concept Id : Uuid\n  generate uuidv7\nmodule M\n  feature F\n    slice StateChange S\n      event Unrelated\n      command RecordPrior\n        id Id identifier\n        produces Unrelated\n          for id\n      command Create\n        id Id generated identifier\n        produces event Started\n        produces event Initialized\n      specification Check\n        {given}\n        when Create\n          for \"{Identity}\"\n        {then}";
}
