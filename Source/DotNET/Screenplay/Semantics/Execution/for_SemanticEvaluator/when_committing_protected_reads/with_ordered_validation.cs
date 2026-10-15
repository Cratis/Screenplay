// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_committing_protected_reads;

public class with_ordered_validation : given.a_protected_command
{
    [Fact]
    void should_reject_absence_before_inspecting_generation_fixtures_or_requirements()
    {
        Compile("concept Receipt : Uuid\n" + Source.Replace("command Update\n", "command Update\n        receipt Receipt generated\n", StringComparison.Ordinal));
        var request = Request() with { GeneratedValues = [null!] };
        var result = (SemanticRejected)_evaluator.Execute(_plan, SemanticWorld.Empty, request);
        result.Details.ShouldEqual("View 'read' does not exist.");
        result.ValidationFailures.Single().Message.ShouldEqual(result.Details);
    }

    [Fact]
    void should_validate_properties_before_reading_or_evaluating_requirements()
    {
        Compile(Source.Replace("require View.value != \"\"\n            message \"Value is empty.\"", "lookup min 5 message \"Lookup is short.\"\n          require View.value != \"\"", StringComparison.Ordinal));
        var result = (SemanticRejected)_evaluator.Execute(_plan, SemanticWorld.Empty, Request());
        result.Details.ShouldEqual("Lookup is short.");
        result.ValidationFailures.Length.ShouldEqual(1);
    }

    [Fact]
    void should_authorize_before_reading()
    {
        Compile("policy SignedIn\n  require authenticated\n" + Source.Replace("command Update\n", "command Update\n        authorize SignedIn\n", StringComparison.Ordinal));
        ((SemanticRejected)_evaluator.Execute(_plan, SemanticWorld.Empty, Request())).Category.ShouldEqual(SemanticRejectionCategory.Unauthorized);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_use_read_paths_on_both_sides_of_requirements(bool right)
    {
        Compile(Source.Replace("require View.value != \"\"", right ? "require lookup == View.value" : "require View.value == lookup", StringComparison.Ordinal));
        var world = World(Fact("Recorded", "read", "read"));
        var result = (SemanticAccepted)_evaluator.Execute(_plan, world, Request());
        result.Facts.Single().Values.Single().Value.ShouldEqual(SemanticValue.Text("read"));
    }
}
