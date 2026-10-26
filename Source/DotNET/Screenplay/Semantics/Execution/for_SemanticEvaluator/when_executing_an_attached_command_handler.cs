// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner.given;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator;

public class when_executing_an_attached_command_handler : a_v6_scenario
{
    void Establish() => Compile("policy Member\n  require authenticated\nmodule M\n  feature F\n    slice StateChange S\n      event Chosen\n        value String\n      command Choose\n        value String\n        authorize Member\n        validate\n          value not empty\n        produces Chosen\n        handler\n          ```csharp\n          throw new Exception(\"Never execute\");\n          ```\n      specification Denied\n        given caller\n          role \"Guest\"\n        when Choose\n          value = \"valid\"\n        then denied\n      specification Invalid\n        given caller\n          authenticated\n        when Choose\n          value = \"\"\n        then error\n      specification Reached\n        given caller\n          authenticated\n        when Choose\n          value = \"valid\"\n        then Chosen\n          value = \"chosen\"\n");

    [Fact] void should_keep_denial_before_the_body() => ((SemanticRejected)Run("Denied").Execution).Category.ShouldEqual(SemanticRejectionCategory.Unauthorized);
    [Fact] void should_keep_validation_before_the_body() => ((SemanticRejected)Run("Invalid").Execution).Category.ShouldEqual(SemanticRejectionCategory.Validation);
    [Fact]
    void should_never_execute_the_body_or_claim_success()
    {
        var run = Run("Reached");
        run.Passed.ShouldBeFalse();
        var unsupported = (SemanticUnsupported)run.Execution;
        unsupported.Capability.ShouldEqual(SemanticExecutionCapability.ImplementationBody);
        unsupported.Details.ShouldContain(_plan.Commands.Values.Single().Handler!.RequirementId);
        unsupported.World.Facts.ShouldBeEmpty();
    }
}
