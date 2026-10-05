// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_ReactionsCorpus;

public class when_reaching_audit_effects_in_order : Specification
{
    [Fact]
    void should_preserve_an_earlier_append_when_a_later_production_needs_audit_identity() => Check(false, false);

    [Fact]
    void should_preserve_an_earlier_append_when_a_later_invocation_mapping_needs_audit_identity() => Check(false, true);

    [Fact]
    void should_report_an_earlier_constraint_rejection_before_an_unreached_audit_production() => Check(true, false);

    [Fact]
    void should_report_an_earlier_constraint_rejection_before_an_unreached_audit_invocation() => Check(true, true);

    [Fact]
    void should_not_check_any_audit_effect_when_the_guard_excludes_the_trigger()
    {
        foreach (var run in given.v6_regression_models.Runs(Model(false, false, false)))
        {
            run.Execution.ShouldBeOfExactType<SemanticAccepted>();
            run.Execution.World.Facts.Length.ShouldEqual(1);
        }
    }

    [Fact]
    void should_keep_a_callerless_invoked_command_atomic_when_its_own_later_production_needs_audit_identity()
    {
        var model = given.v6_regression_models.Compile(Source(false, true, true)
            .Replace("produces Prior\n          invokes Audit\n            id = \"root\"\n            user = $context.causedBy.userName", "invokes Audit\n            id = \"root\"\n            user = \"unused\"", StringComparison.Ordinal)
            .Replace("produces Audited\n          for id", "produces Prior\n          for id\n        produces Audited\n          for id", StringComparison.Ordinal)
            .Replace("user = user", "user = $context.causedBy.userName", StringComparison.Ordinal));
        foreach (var run in given.v6_regression_models.Runs(model))
        {
            run.Execution.ShouldBeOfExactType<SemanticUnsupported>();
            run.Passed.ShouldBeFalse();
            run.Execution.World.Facts.Length.ShouldEqual(1);
        }
    }

    static void Check(bool rejected, bool invocation)
    {
        foreach (var run in given.v6_regression_models.Runs(Model(rejected, invocation, true)))
        {
            run.Execution.World.Facts.Length.ShouldEqual(2);
            if (rejected)
            {
                var failure = (SemanticRejected)run.Execution;
                failure.Category.ShouldEqual(SemanticRejectionCategory.Constraint);
                failure.Code.ShouldEqual("Once");
                run.Passed.ShouldBeTrue();
            }
            else
            {
                var failure = (SemanticUnsupported)run.Execution;
                failure.Capability.ShouldEqual(SemanticExecutionCapability.Reaction);
                failure.Details.ShouldContain("$context.causedBy");
                run.Passed.ShouldBeFalse();
            }
        }
    }

    static ExecutableSemanticModel Model(bool rejected, bool invocation, bool enabled) =>
        given.v6_regression_models.Compile(Source(rejected, invocation, enabled));

    static string Source(bool rejected, bool invocation, bool enabled) => $$"""
        module Billing
          feature Effects
            slice Automation Effects
              command Audit
                id String identifier
                user String
                produces Audited
                  for id
                  user = user
              reaction Ordered
                where enabled == true
                when Started
                  produces Prior
                  {{(invocation ? "invokes Audit\n            id = \"root\"\n            user = $context.causedBy.userName" : "produces Audited\n            user = $context.causedBy.userName")}}
              event Started
                enabled Bool
              event Prior
              event Audited
                user String
              {{(rejected ? "constraint Once\n        unique event Prior" : "")}}
              specification Ordered
                given clock "2026-10-02T09:00:00Z"
                {{(rejected ? "given Prior" : "")}}
                when append Started
                  enabled = {{(enabled ? "true" : "false")}}
                then error
        """;
}
