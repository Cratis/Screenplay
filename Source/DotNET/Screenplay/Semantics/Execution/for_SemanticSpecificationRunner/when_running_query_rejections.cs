// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.given;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner;

public class when_running_query_rejections : Specification
{
    SemanticExecutionPlan _plan;
    SemanticId _specification;

    void Establish()
    {
        var compilation = query_validation_source.Compile();
        Assert.True(compilation.Success, string.Join('\n', compilation.Diagnostics.Select(diagnostic => diagnostic.Message)));
        _plan = SemanticExecutionPlan.Compile(compilation.Value!.Model).Plan!;
        _specification = _plan.Specifications.Values.Single().Id;
    }

    [Fact] void should_pass_a_validation_rejection_with_a_message() => new SemanticSpecificationRunner().Run(_plan, _specification).Passed.ShouldBeTrue();
    [Fact] void should_return_the_validation_category() => ((SemanticRejected)new SemanticSpecificationRunner().Run(_plan, _specification).Execution).Category.ShouldEqual(SemanticRejectionCategory.Validation);

    [Fact]
    void should_pass_a_bare_validation_rejection()
    {
        var compilation = query_validation_source.Compile(query_validation_source.Source.Replace("then error \"Invoice number is too short\"", "then error", StringComparison.Ordinal));
        Assert.True(compilation.Success, string.Join('\n', compilation.Diagnostics.Select(diagnostic => diagnostic.Message)));
        var plan = SemanticExecutionPlan.Compile(compilation.Value!.Model).Plan!;
        new SemanticSpecificationRunner().Run(plan, plan.Specifications.Values.Single().Id).Passed.ShouldBeTrue();
    }

    [Theory]
    [InlineData(SemanticRejectionCategory.Contract)]
    [InlineData(SemanticRejectionCategory.Unauthorized)]
    void should_not_pass_another_rejection_category(SemanticRejectionCategory category)
    {
        var evaluator = Substitute.For<ISemanticEvaluator>();
        evaluator.Execute(Arg.Any<SemanticExecutionPlan>(), Arg.Any<SemanticWorld>(), Arg.Any<SemanticExecutionRequest>()).Returns(new SemanticRejected(SemanticWorld.Empty, category, null, "Invoice number is too short"));
        new SemanticSpecificationRunner(evaluator).Run(_plan, _specification).Passed.ShouldBeFalse();
    }

    [Fact]
    void should_not_pass_an_unsupported_query()
    {
        var evaluator = Substitute.For<ISemanticEvaluator>();
        evaluator.Execute(Arg.Any<SemanticExecutionPlan>(), Arg.Any<SemanticWorld>(), Arg.Any<SemanticExecutionRequest>()).Returns(new SemanticUnsupported(SemanticWorld.Empty, SemanticExecutionCapability.Query, "opaque"));
        new SemanticSpecificationRunner(evaluator).Run(_plan, _specification).Passed.ShouldBeFalse();
    }

    [Fact]
    void should_preserve_command_error_comparison_and_version()
    {
        const string source = """
            concept InvoiceNumber : String
              validate
                min 2 message "Invoice number is too short"
            module Billing
              feature Invoices
                slice StateChange Register
                  command RegisterInvoice
                    number InvoiceNumber
                  specification MalformedNumber
                    when RegisterInvoice
                      number = "A"
                    then error "Invoice number is too short"
            """;
        var compilation = query_validation_source.Compile(source);
        Assert.True(compilation.Success, string.Join('\n', compilation.Diagnostics.Select(diagnostic => diagnostic.Message)));
        compilation.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V1);
        var plan = SemanticExecutionPlan.Compile(compilation.Value.Model).Plan!;
        var specification = plan.Specifications.Values.Single().Id;
        new SemanticSpecificationRunner().Run(plan, specification).Passed.ShouldBeTrue();
        var evaluator = Substitute.For<ISemanticEvaluator>();
        evaluator.Execute(Arg.Any<SemanticExecutionPlan>(), Arg.Any<SemanticWorld>(), Arg.Any<SemanticExecutionRequest>()).Returns(new SemanticRejected(SemanticWorld.Empty, SemanticRejectionCategory.Contract, null, "Invoice number is too short"));
        new SemanticSpecificationRunner(evaluator).Run(plan, specification).Passed.ShouldBeTrue();
    }

    [Fact]
    void should_not_pass_a_successful_absent_lookup()
    {
        var compilation = query_validation_source.Compile(query_validation_source.Source.Replace("number = \"A\"", "number = \"AB\"", StringComparison.Ordinal));
        var plan = SemanticExecutionPlan.Compile(compilation.Value!.Model).Plan!;
        new SemanticSpecificationRunner().Run(plan, plan.Specifications.Values.Single().Id).Passed.ShouldBeFalse();
    }
}
