// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_validating_guarded_action_paths
{
    const string Declarations =
        """
        type Attempt
          status String
        module Work
          feature Items
            slice StateView Details
              readmodel Item
                itemId Uuid
                ids Uuid[]
                status String
                attempt Attempt optional
                attempts Attempt[]
              query ItemDetails => Item
              command Retry
                itemId Uuid
                ids Uuid[]
              screen Details
        """;

    [Theory]
    [InlineData("data Item via query ItemDetails\naction \"Again\"\n  when item.missing == null execute Retry", DiagnosticCodes.UnknownActionSubjectField)]
    [InlineData("data Item via query ItemDetails\naction \"Again\"\n  when item.attempt.missing == null execute Retry", DiagnosticCodes.UnknownActionSubjectField)]
    [InlineData("data Item via query ItemDetails\naction \"Again\"\n  when item.status.missing == null execute Retry", DiagnosticCodes.UnknownActionSubjectField)]
    [InlineData("data Item via query ItemDetails\naction \"Again\"\n  when item.attempts == null execute Retry", DiagnosticCodes.UnknownActionSubjectField)]
    [InlineData("data Item via query ItemDetails\naction \"Again\"\n  when item.attempts.status == null execute Retry", DiagnosticCodes.UnknownActionSubjectField)]
    [InlineData("data Item via query ItemDetails\naction \"Again\"\n  when item.ids == null execute Retry", DiagnosticCodes.UnknownActionSubjectField)]
    [InlineData("data Item via query ItemDetails\naction \"Again\"\n  when item.status == null execute Retry\n    with ids from item.attempts.status", DiagnosticCodes.UnknownActionSubjectField)]
    [InlineData("data Item via query ItemDetails\naction \"Again\"\n  when item.status == null execute Retry\n  otherwise execute Retry\n    with ids from item.ids.value", DiagnosticCodes.UnknownActionSubjectField)]
    [InlineData("data Item via query ItemDetails\naction \"Again\"\n  when item.status == null execute Retry\n    with itemId from item.missing", DiagnosticCodes.UnknownActionSubjectField)]
    [InlineData("data Item via query ItemDetails\naction \"Again\"\n  when item.status == null execute Retry\n    with missing from item.itemId", DiagnosticCodes.UnknownActionArgumentProperty)]
    [InlineData("data Item via query ItemDetails\naction \"Again\"\n  when item.status == null execute Retry\n  otherwise execute Retry\n    with missing from item.itemId", DiagnosticCodes.UnknownActionArgumentProperty)]
    [InlineData("data Item via query ItemDetails\naction \"Again\"\n  when item.status == null execute Unknown", DiagnosticCodes.UnknownCommand)]
    [InlineData("data Item via query ItemDetails\naction \"Again\"\n  when item.status == null execute Retry\n  otherwise execute Unknown", DiagnosticCodes.UnknownCommand)]
    [InlineData("data Item via query ItemDetails\naction \"Again\"\n  when item.status == null execute Retry\n  navigate to Unknown", DiagnosticCodes.UnknownScreen)]
    [InlineData("action \"Again\"\n  when item.status == null execute Retry", DiagnosticCodes.UnresolvedActionSubject)]
    [InlineData("data Item via query ItemDetails\ndata Item via query ItemDetails\naction \"Again\"\n  when item.missing == null execute Retry", DiagnosticCodes.UnresolvedActionSubject)]
    public void should_report_a_precise_warning(string directives, string code)
    {
        var result = Compile(directives);
        result.Success.ShouldBeTrue();
        result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContainOnly(code);
        result.Diagnostics.Single().Severity.ShouldEqual(DiagnosticSeverity.Warning);
    }

    [Theory]
    [InlineData("data Item via query ItemDetails\nsection actions\n  action \"Again\"\n    when item.attempt.status == \"failed\" execute Retry\n      with itemId from item.itemId")]
    [InlineData("data Item[] via query ItemDetails\naction \"Again\"\n  when item.status == \"failed\" execute Work.Items.Details.Retry")]
    [InlineData("data Item via query ItemDetails\nsection actions\n  data Item via query ItemDetails\n  action \"Again\"\n    when item.status == \"failed\" execute Retry")]
    [InlineData("data Item via query ItemDetails\naction \"Again\"\n  when item.status == \"failed\" execute Retry\n    with ids from item.ids")]
    [InlineData("data Item via query ItemDetails\naction \"Again\"\n  when item.status == \"failed\" execute Retry\n  otherwise execute Retry\n    with ids from item.ids")]
    public void should_resolve_single_selected_and_nearest_items(string directives) => Compile(directives).Diagnostics.ShouldBeEmpty();

    static CompilationResult<Syntax.ApplicationSyntax> Compile(string directives) => new ScreenplayCompiler().Compile(Declarations + "\n        " + directives.Replace("\n", "\n        ", StringComparison.Ordinal));
}
