// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.Parsing.for_ScreenActionPresentation;

public class when_parsing_action_presentation : Specification
{
    internal const string Source = "concept ItemId : Uuid\nmodule Work\n  feature Items\n    slice StateView List\n      readmodel Row\n        rowId ItemId key\n      command Retry\n        itemId ItemId identifier\n      query Rows => Row[]\n      screen List\n        data Row[] via query Rows\n        action Retry for item label \"Retry\"\n          with itemId from item.rowId\n          navigate to List\n          presentation style \"quiet\"\n          presentation placement \"row-menu\"\n        action Retry for screen";
    ApplicationSyntax _syntax;
    ScreenActionSyntax _action;
    ApplicationSyntax _reparsed;

    void Because()
    {
        _syntax = new ScreenplayCompiler().Compile(Source).Value!;
        _action = Action(_syntax);
        _reparsed = new ScreenplayCompiler().Compile(new ScreenplayPrinter().Print(_syntax)).Value!;
    }

    [Fact] void should_retain_authored_order() => _action.Presentation.Select(value => value.Name).ShouldContainOnly(["style", "placement"]);
    [Fact] void should_preserve_the_hint() => _action.Presentation.Last().Value.ShouldEqual("row-menu");
    [Fact] void should_preserve_the_subject() => _action.Subject.ShouldEqual(ScreenActionSubjectKind.Item);
    [Fact] void should_preserve_the_label() => _action.Label.ShouldEqual("Retry");
    [Fact] void should_preserve_the_navigation() => _action.Navigate!.Screen.ShouldEqual("List");
    [Fact] void should_preserve_the_binding() => _action.Inputs.Single().Binding.ShouldEqual("item.rowId");
    [Fact] void should_keep_the_authored_location() => _action.Presentation.Last().Location.Line.ShouldEqual(16);
    [Fact] void should_round_trip_through_the_printer() => SyntaxJson.StructurallyEqual(_syntax, _reparsed).ShouldBeTrue();
    [Fact] void should_leave_unhinted_actions_empty() => _syntax.Modules.Single().Features.Single().Slices.Single().Screens.Single().Directives.OfType<ScreenActionSyntax>().Last().Presentation.ShouldBeEmpty();

    [Theory]
    [InlineData("presentation placement \"future-menu\"")]
    [InlineData("presentation placement \"row-menu\"\n          presentation extension \"custom\"")]
    void should_preserve_unknown_advisory_values(string body) => new ScreenplayCompiler().Compile(Source.Replace("presentation style \"quiet\"\n          presentation placement \"row-menu\"", body, StringComparison.Ordinal)).Diagnostics.ShouldNotContain(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

    [Theory]
    [InlineData("presentation placement \"\"")]
    [InlineData("presentation placement \"  \"")]
    [InlineData("presentation placement \"row-menu\"\n          presentation placement \"other\"")]
    void should_reject_invalid_values(string body) => new ScreenplayCompiler().Compile(Source.Replace("presentation style \"quiet\"\n          presentation placement \"row-menu\"", body, StringComparison.Ordinal)).Diagnostics.ShouldContain(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidActionPresentation);

    [Theory]
    [InlineData("presentation")]
    [InlineData("presentation placement")]
    [InlineData("placement \"row-menu\"")]
    void should_reject_malformed_children(string body) => new ScreenplayCompiler().Compile(Source.Replace("presentation style \"quiet\"\n          presentation placement \"row-menu\"", body, StringComparison.Ordinal)).Diagnostics.ShouldContain(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

    [Theory]
    [InlineData("for screen")]
    [InlineData("")]
    void should_reject_hints_without_explicit_item_scope(string scope) => new ScreenplayCompiler().Compile(Source.Replace("for item", scope, StringComparison.Ordinal)).Diagnostics.ShouldContain(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidActionPresentation);

    internal static ScreenActionSyntax Action(ApplicationSyntax syntax) => syntax.Modules.Single().Features.Single().Slices.Single().Screens.Single().Directives.OfType<ScreenActionSyntax>().First();
}
