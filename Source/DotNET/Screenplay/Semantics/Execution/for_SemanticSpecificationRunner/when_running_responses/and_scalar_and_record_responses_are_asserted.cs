// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner.when_running_responses;

public class and_scalar_and_record_responses_are_asserted : given.a_v6_scenario
{
    [Fact]
    void should_return_a_generated_identifier_as_destination_payload_and_scalar_response()
    {
        Compile("""
            concept Id : Uuid
            module Billing
              feature F
                slice StateChange S
                  command C
                    id Id generated identifier
                    produces event Created
                      id Id = id
                    returns id
                  specification Accepted
                    when C
                      for "11111111-1111-1111-1111-111111111111"
                    then Created
                      for "11111111-1111-1111-1111-111111111111"
                      id = "11111111-1111-1111-1111-111111111111"
                    then returns "11111111-1111-1111-1111-111111111111"
            """);
        var run = Run("Accepted");
        run.Passed.ShouldBeTrue();
        ((SemanticScalarExecutionResponse)((SemanticAccepted)run.Execution).Response!).Value.ShouldEqual(SemanticValue.Text("11111111-1111-1111-1111-111111111111"));
    }

    [Theory]
    [InlineData("then returns\n          z = \"hello\"", true)]
    [InlineData("then returns\n          a = \"note\"", true)]
    [InlineData("then returns\n          a = \"wrong\"\n          z = \"wrong\"", false)]
    void should_compare_record_subsets_in_response_authored_order(string assertion, bool passes)
    {
        Compile("module Billing\n  feature F\n    slice StateChange S\n      command C\n        name String\n        note String?\n        returns\n          z = name\n          a = note\n      specification Accepted\n        when C\n          name = \"hello\"\n          note = \"note\"\n        " + assertion);
        var run = Run("Accepted");
        run.Passed.ShouldEqual(passes);
        var response = (SemanticRecordExecutionResponse)((SemanticAccepted)run.Execution).Response!;
        response.Fields.Select(field => field.Name).ShouldEqual(["z", "a"]);
        if (!passes)
        {
            run.Failures.ShouldEqual(new[] { "Response field 'z' does not match the expected value.", "Response field 'a' does not match the expected value." });
        }
    }

    [Theory]
    [InlineData("hello", true)]
    [InlineData("different", false)]
    void should_compare_a_scalar_response(string expected, bool passes)
    {
        Compile($"module Billing\n  feature F\n    slice StateChange S\n      command C\n        name String\n        returns name\n      specification Accepted\n        when C\n          name = \"hello\"\n        then returns \"{expected}\"");
        var run = Run("Accepted");
        run.Passed.ShouldEqual(passes);
        ((SemanticAccepted)run.Execution).Facts.ShouldBeEmpty();
        if (!passes) run.Failures.ShouldEqual(new[] { "Scalar response does not match the expected value." });
    }

    [Fact]
    void should_keep_no_expected_events_comparison_for_response_only_assertions()
    {
        Compile("module Billing\n  feature F\n    slice StateChange S\n      command C\n        name String identifier\n        produces event Created\n          name String = name\n        returns name\n      specification Accepted\n        when C\n          name = \"hello\"\n        then returns \"hello\"");
        Run("Accepted").Failures.ShouldEqual(new[] { "Expected 0 fact(s), got 1." });
    }

    [Fact]
    void should_fail_a_return_assertion_when_validation_rejects_before_generation()
    {
        Compile("concept Id : Uuid\nmodule Billing\n  feature F\n    slice StateChange S\n      command C\n        id Id generated identifier\n        name String\n        validate\n          name min 1\n        returns id\n      specification Rejected\n        when C\n          name = \"\"\n        then returns \"11111111-1111-1111-1111-111111111111\"");
        var run = Run("Rejected");
        run.Passed.ShouldBeFalse();
        ((SemanticRejected)run.Execution).Category.ShouldEqual(SemanticRejectionCategory.Validation);
        run.Execution.World.Facts.ShouldBeEmpty();
    }

    [Fact]
    void should_report_missing_reached_fixture_as_unsupported()
    {
        Compile("concept Id : Uuid\nmodule Billing\n  feature F\n    slice StateChange S\n      command C\n        id Id generated identifier\n        returns id\n      specification Missing\n        when C\n        then returns \"11111111-1111-1111-1111-111111111111\"");
        var run = Run("Missing");
        run.Passed.ShouldBeFalse();
        ((SemanticUnsupported)run.Execution).Capability.ShouldEqual(SemanticExecutionCapability.IdentityAllocation);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(6)]
    [InlineData(7)]
    void should_keep_the_legacy_blanket_destination_assertion_without_a_generated_identifier(int version)
    {
        var reaction = version >= 6 ? "    slice Automation A\n      reaction R\n        when Unrelated\n          produces Followed\n      event Unrelated\n      event Followed" : string.Empty;
        Compile("module Billing\n  feature F\n    slice StateChange S\n      command C\n        id String identifier\n        produces Created\n          for id\n" + (version == 7 ? "        returns id\n" : string.Empty) + "      event Created\n      specification Mismatch\n        when C\n          for \"elsewhere\"\n          id = \"here\"\n        then Created\n          for \"here\"\n" + reaction);
        Run("Mismatch").Failures.ShouldEqual(new[] { "Produced fact destination does not match the specification command event source." });
    }

    [Fact]
    void should_not_assert_the_generated_identifier_as_every_production_destination()
    {
        Compile("concept Id : Uuid\nmodule Billing\n  feature F\n    slice StateChange S\n      command C\n        id Id generated identifier\n        other String\n        returns id\n        produces Created\n          for other\n      event Created\n      specification Accepted\n        when C\n          for \"11111111-1111-1111-1111-111111111111\"\n          other = \"elsewhere\"\n        then Created\n          for \"elsewhere\"\n        then returns \"11111111-1111-1111-1111-111111111111\"");
        Run("Accepted").Passed.ShouldBeTrue();
    }
}
