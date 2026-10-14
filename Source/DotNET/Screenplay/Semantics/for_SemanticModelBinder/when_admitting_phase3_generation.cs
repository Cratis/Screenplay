// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_admitting_phase3_generation : given.a_semantic_binder
{
    [Theory]
    [InlineData("String", "pattern \"ORD-#{7}\"")]
    [InlineData("Int", "random -9223372036854775808..9223372036854775807")]
    [InlineData("String", "rule Make\n    ```csharp\n    return value;\n    ```")]
    void should_select_v21_only_for_phase3(string primitive, string strategy)
    {
        var result = Bind($"concept Reference : {primitive}\n  generate {strategy}");
        result.Success.ShouldBeTrue();
        result.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V21);
    }

    [Fact]
    void should_report_empty_conditioned_support_as_a_hard_error()
    {
        var result = Bind("concept Reference : String\n  generate pattern \"000000006-[AB]\" checksum mod11");
        result.Success.ShouldBeFalse();
        result.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.EmptyGenerationSupport).ShouldBeTrue();
    }

    [Fact]
    void should_report_generation_budget_exhaustion_as_a_hard_error()
    {
        var result = Bind("concept Reference : String\n  generate pattern \"*{64}*{64}*{64}*{63}\" checksum mod11");
        result.Success.ShouldBeFalse();
        result.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.GenerationSupportBudgetExceeded).ShouldBeTrue();
    }

    [Fact]
    void should_refute_exact_numeric_rules_with_an_int64_witness()
    {
        var result = Bind("concept Ticket : Int\n  generate random 9007199254740992..9007199254740993\n  validate\n    max 9007199254740992");
        Assert.True(result.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.RefutedGeneratedConceptRule && diagnostic.Message.Contains("9007199254740993", StringComparison.Ordinal)), string.Join('\n', result.Diagnostics.Select(diagnostic => diagnostic.Message)));
    }

    [Fact]
    void should_reject_orphan_or_incompatible_generation_attachments()
    {
        var bound = Bind("concept Reference : String\n  generate rule Make\n    ```csharp\n    return value;\n    ```");
        var application = bound.Value!.Model.Application;
        var requirement = bound.ImplementationRequirements.Single();
        Catch.Exception(() => SemanticTypedContextCatalog.Create(application, [], true)).ShouldBeOfExactType<InvalidSemanticContract>();
        Catch.Exception(() => SemanticTypedContextCatalog.Create(application, [requirement with { Role = SemanticImplementationRole.RulePredicate }], true)).ShouldBeOfExactType<InvalidSemanticContract>();
        Catch.Exception(() => SemanticTypedContextCatalog.Create(application, [requirement with { RequiredCapability = "pure" }], true)).ShouldBeOfExactType<InvalidSemanticContract>();
    }
}
