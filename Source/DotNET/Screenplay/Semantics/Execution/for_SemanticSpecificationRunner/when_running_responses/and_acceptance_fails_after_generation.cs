// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner.when_running_responses;

public class and_acceptance_fails_after_generation : given.a_v6_scenario
{
    const string Prefix = "concept Id : Uuid\nmodule Billing\n  feature F\n    slice StateChange S\n      command C\n        id Id generated identifier\n        produces event Created\n          id Id = id\n        returns id\n";

    [Fact]
    void should_keep_the_input_world_when_a_constraint_rejects_after_generation()
    {
        Compile(Prefix + "      constraint Once\n        unique event Created\n      specification Collision\n        given Created\n          for \"11111111-1111-1111-1111-111111111111\"\n          id = \"11111111-1111-1111-1111-111111111111\"\n        when C\n          for \"11111111-1111-1111-1111-111111111111\"\n        then returns \"11111111-1111-1111-1111-111111111111\"");
        var run = Run("Collision");
        ((SemanticRejected)run.Execution).Category.ShouldEqual(SemanticRejectionCategory.Constraint);
        run.Execution.World.Facts.Length.ShouldEqual(1);
        run.Passed.ShouldBeFalse();
    }

    [Fact]
    void should_report_projection_inability_without_accepting_the_command()
    {
        Compile(Prefix + "      specification Opaque\n        when C\n          for \"11111111-1111-1111-1111-111111111111\"\n        then returns \"11111111-1111-1111-1111-111111111111\"\n    slice StateView View\n      readmodel Details\n        id Id\n      query ById => Details optional\n        by id Id\n      reducer Derived => Details\n        on Created\n          ```csharp\n            return current;\n            ```");
        var run = Run("Opaque");
        ((SemanticUnsupported)run.Execution).Capability.ShouldEqual(SemanticExecutionCapability.Projection);
        run.Execution.World.Facts.ShouldBeEmpty();
    }
}
