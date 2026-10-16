// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_CompositeReadModelKeysCorpus;

public class when_projecting_typed_key_parts : given.a_flat_key_model
{
    [Theory]
    [InlineData("String", "$eventSourceId", "\"ABCDEF12-3456-4789-ABCD-0123456789AB\"")]
    [InlineData("String", "$eventContext.eventSourceId", "\"ABCDEF12-3456-4789-ABCD-0123456789AB\"")]
    [InlineData("String", "$eventContext.eventSourceId.value", "\"ABCDEF12-3456-4789-ABCD-0123456789AB\"")]
    [InlineData("Uuid", "\"abcdef12-3456-4789-abcd-0123456789ab\"", "\"abcdef12-3456-4789-abcd-0123456789ab\"")]
    [InlineData("Int", "42", "42")]
    [InlineData("Bool", "true", "true")]
    [InlineData("Date", "\"2026-10-16\"", "\"2026-10-16\"")]
    [InlineData("Enum", "\"card\"", "\"card\"")]
    public void should_bind_round_trip_and_execute_target_typed_parts(string type, string expression, string expected)
    {
        var compiled = Compile(Source(type, expression) + "\n" + $$"""
                  specification Project
                    given clock "2026-10-16T12:00:00Z"
                    when append Arrived
                      for "ABCDEF12-3456-4789-ABCD-0123456789AB"
                    then readmodel Result
                      first = {{expected}}
                      second = "bucket"
            """);
        Assert.True(compiled.Success, string.Join('\n', compiled.Diagnostics.Select(diagnostic => diagnostic.Message)));
        var bytes = SemanticModelSerializer.Serialize(compiled.Value!.Model);
        var model = SemanticModelSerializer.Deserialize(bytes);
        SemanticModelSerializer.Serialize(model).ShouldEqual(bytes);
        var specification = model.Application.Modules.Single().Features.Single().Slices.Single().Specifications.Single();
        var plan = SemanticExecutionPlan.Compile(model);
        Assert.True(plan.Success, string.Join('\n', plan.Issues.Select(issue => issue.Details)));
        var run = new SemanticSpecificationRunner().Run(plan.Plan!, specification.Id);
        Assert.True(run.Passed, string.Join('\n', run.Failures));
    }

    [Theory]
    [InlineData("DateTime", "$eventContext.occurred")]
    [InlineData("Bool", "$eventContext.eventType.tombstone")]
    public void should_preserve_the_reference_evaluator_limit_for_other_context_parts(string type, string expression)
    {
        var compiled = Compile(Source(type, expression));
        Assert.True(compiled.Success, string.Join('\n', compiled.Diagnostics.Select(diagnostic => diagnostic.Message)));
        var bytes = SemanticModelSerializer.Serialize(compiled.Value!.Model);
        var model = SemanticModelSerializer.Deserialize(bytes);
        SemanticModelSerializer.Serialize(model).ShouldEqual(bytes);
        var plan = SemanticExecutionPlan.Compile(model);
        plan.Success.ShouldBeFalse();
        plan.Plan.ShouldBeNull();
        plan.Issues.Select(issue => issue.Kind).ShouldContainOnly(SemanticPlanIssueKind.UnsupportedEventContext);
    }

    [Theory]
    [InlineData("Uuid", "\"not-a-uuid\"")]
    [InlineData("Int", "$eventContext.occurred")]
    [InlineData("Bool", "$eventContext.sequenceNumber.value")]
    [InlineData("Enum", "\"unknown\"")]
    public void should_refuse_incompatible_contexts_and_literals(string type, string expression)
    {
        var result = Compile(Source(type, expression));
        result.Success.ShouldBeFalse();
        result.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).Select(diagnostic => diagnostic.Code).ShouldContainOnly(DiagnosticCodes.InvalidReadModelKeyLookup);
    }

    static string Source(string type, string expression) => $"concept KeyPart : {type}\n" + (type == "Enum" ? "  card\n  cash\n" : string.Empty) + $$"""
        module M
          feature F
            slice StateView S
              event Arrived
              command Record
                id String identifier
                produces Arrived
                  for id
              readmodel Result
                first KeyPart key
                second String key
              projection Rows => Result
                no automap
                from Arrived
                  key
                    first = {{expression}}
                    second = "bucket"
        """;
}
