// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner.given;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator;

public class when_preserving_command_phase_precedence : a_v6_scenario
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    void should_preserve_denial_and_opaque_admission_before_malformed_inputs(bool v7, bool opaque)
    {
        var validation = opaque ? "        validate\n          ```csharp\n            return true;\n            ```\n" : string.Empty;
        Compile("policy SignedIn\n  require authenticated\nmodule Billing\n  feature F\n    slice StateChange S\n      command C\n        id String identifier\n        authorize SignedIn\n" + validation + (v7 ? "        returns id\n" : string.Empty) + "    slice Automation A\n      reaction R\n        when Unrelated\n          produces Followed\n      event Unrelated\n      event Followed");
        _plan.Model.SemanticVersion.ShouldEqual(v7 ? SemanticVersion.V7 : SemanticVersion.V6);
        var request = SemanticExecutionRequest.Create(_plan.Commands.Values.Single().Id, [null!], []) with { Caller = opaque ? new(true, [], []) : null, GeneratedValues = [null!] };
        var result = new SemanticEvaluator().Execute(_plan, SemanticWorld.Empty, request);
        if (opaque) ((SemanticUnsupported)result).Capability.ShouldEqual(SemanticExecutionCapability.Command);
        else ((SemanticRejected)result).Category.ShouldEqual(SemanticRejectionCategory.Unauthorized);
        var invalidEnvelope = new SemanticEvaluator().Execute(_plan, SemanticWorld.Empty, request with { Caller = null, Queries = default });
        ((SemanticRejected)invalidEnvelope).Category.ShouldEqual(SemanticRejectionCategory.Contract);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_reject_malformed_inputs_on_authorized_admitted_commands(bool v7)
    {
        Compile("policy SignedIn\n  require authenticated\nmodule Billing\n  feature F\n    slice StateChange S\n      command C\n        id String identifier\n        authorize SignedIn\n" + (v7 ? "        returns id\n" : string.Empty) + "    slice Automation A\n      reaction R\n        when Unrelated\n          produces Followed\n      event Unrelated\n      event Followed");
        var request = SemanticExecutionRequest.Create(_plan.Commands.Values.Single().Id, [null!], []) with { Caller = new(true, [], []) };
        ((SemanticRejected)new SemanticEvaluator().Execute(_plan, SemanticWorld.Empty, request)).Category.ShouldEqual(SemanticRejectionCategory.Contract);
    }

    [Theory]
    [InlineData(1U)]
    [InlineData(2U)]
    [InlineData(3U)]
    [InlineData(4U)]
    [InlineData(5U)]
    [InlineData(6U)]
    [InlineData(7U)]
    void should_reject_a_default_queries_envelope_before_denial_at_every_version(uint version)
    {
        var construct = version switch
        {
            2 => "        produces Created\n          for id\n      event Created",
            3 => "        validate\n          ```csharp\n            return true;\n            ```",
            4 => "      event Created\n        name String\n      event Created generation 2\n        name String",
            5 => "      readmodel View\n        id String\n      query ById => View?\n        by id String\n      specification Absent\n        then no readmodel View for \"missing\"",
            6 => "    slice Automation A\n      reaction R\n        when Startup\n          produces Started\n            for \"start\"\n      event Started",
            7 => "        returns id",
            _ => string.Empty
        };
        Compile("policy SignedIn\n  require authenticated\nmodule Billing\n  feature F\n    slice StateChange S\n      command C\n        id String identifier\n        authorize SignedIn\n" + construct);
        _plan.Model.SemanticVersion.Major.ShouldEqual(version);
        var request = SemanticExecutionRequest.Create(_plan.Commands.Values.Single().Id, [], default);
        ((SemanticRejected)new SemanticEvaluator().Execute(_plan, SemanticWorld.Empty, request)).Category.ShouldEqual(SemanticRejectionCategory.Contract);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(6)]
    [InlineData(7)]
    void should_preserve_occurrence_check_for_an_unreached_context_production(int version)
    {
        var reaction = version >= 6 ? "    slice Automation A\n      reaction R\n        when Unrelated\n          produces Followed\n      event Unrelated\n      event Followed" : string.Empty;
        Compile("module Billing\n  feature F\n    slice StateChange S\n      command C\n        id String identifier\n        enabled Bool\n        produces when enabled == true\n          Created\n            for id\n            at = $context.occurred\n" + (version == 7 ? "        returns id\n" : string.Empty) + "      event Created\n        at DateTime\n" + reaction);
        var command = _plan.Commands.Values.Single();
        var request = SemanticExecutionRequest.Create(command.Id, [new(command.Properties.Single(property => property.Name == "id").Id, SemanticValue.Text("hello")), new(command.Properties.Single(property => property.Name == "enabled").Id, SemanticValue.Boolean(false))], []);
        var result = new SemanticEvaluator().Execute(_plan, SemanticWorld.Empty, request);
        if (version == 2) ((SemanticRejected)result).Category.ShouldEqual(SemanticRejectionCategory.Contract);
        else result.ShouldBeOfExactType<SemanticAccepted>();
    }
}
