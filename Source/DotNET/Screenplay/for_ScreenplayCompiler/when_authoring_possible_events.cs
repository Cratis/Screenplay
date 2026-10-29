// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_authoring_possible_events : given.a_compiler
{
    const string Prefix = "module M\n  feature F\n    slice StateChange S\n      command C\n        handler\n";
    const string Declaration = "          may produce\n            Added\n            Removed for accountId\n            Updated for one of accountIds\n";

    [Theory]
    [InlineData("          file C.cs")]
    [InlineData("          implementation")]
    [InlineData("          implementation\n            hint \"Keep the cutoff\"\n            file C.cs")]
    [InlineData("          ```csharp\n          return null;\n          ```")]
    void should_preserve_the_set_and_payload_through_printing_and_transport(string payload)
    {
        var parsed = _compiler.Parse(Prefix + Declaration + payload);
        parsed.Success.ShouldBeTrue();
        var handler = parsed.Value!.Modules.Single().Features.Single().Slices.Single().Commands.Single().Handler!;
        handler.PossibleEvents!.Events.Select(entry => entry.Event).ShouldEqual(["Added", "Removed", "Updated"]);
        handler.PossibleEvents.Events.Last().OneOf.ShouldBeTrue();
        var printed = new ScreenplayPrinter().Print(parsed.Value!);
        var reparsed = _compiler.Parse(printed);
        reparsed.Success.ShouldBeTrue();
        SyntaxJson.StructurallyEqual(parsed.Value!, reparsed.Value!).ShouldBeTrue();
        SyntaxJson.StructurallyEqual(parsed.Value!, SyntaxJson.Deserialize(SyntaxJson.Serialize(parsed.Value!))).ShouldBeTrue();
    }

    [Theory]
    [InlineData("Added when active")]
    [InlineData("Added\n            Added for other")]
    [InlineData("Added for cmd.accountId")]
    [InlineData("Added for 123")]
    [InlineData("Added for one of ids\n              value = accountId")]
    [InlineData("Added\n              for accountId")]
    void should_reject_non_contract_entry_shapes(string entry) =>
        _compiler.Parse(Prefix + "          may produce\n            " + entry + "\n          file C.cs").Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidPossibleEventsDeclaration).ShouldBeTrue();

    [Theory]
    [InlineData("", 6)]
    [InlineData(Declaration, 10)]
    void should_reject_an_inline_body_without_a_language_at_parse_time(string declaration, int line)
    {
        var parsed = _compiler.Parse(Prefix + declaration + "          ```\n          return null;\n          ```");
        parsed.Success.ShouldBeFalse();
        var diagnostic = parsed.Diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.ExpectedCodeFence);
        diagnostic.Message.ShouldEqual("Expected a registered language on the opening fence, not '```'");
        diagnostic.Severity.ShouldEqual(DiagnosticSeverity.Error);
        diagnostic.Location.Line.ShouldEqual(line);
    }

    [Fact]
    void should_reject_a_mixed_legacy_contract() =>
        _compiler.Parse(Prefix + Declaration + "          file C.cs\n        produces Added").Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidPossibleEventsDeclaration).ShouldBeTrue();

    [Theory]
    [InlineData("          file C.cs\n          may produce\n            Added")]
    [InlineData("          may produce\n            Added\n          may produce\n            Removed\n          file C.cs")]
    [InlineData("          implementation\n            may produce\n              Added")]
    void should_reject_misplaced_or_repeated_sets(string body) => _compiler.Parse(Prefix + body).Success.ShouldBeFalse();

    [Fact]
    void should_keep_absent_and_empty_sets_out_of_legacy_transport()
    {
        var handler = new HandlerSyntax(new("C.cs", SourceLocation.Start), null, SourceLocation.Start);
        var baseline = SyntaxJson.Serialize(handler).GetRawText();
        SyntaxJson.Serialize(handler with { PossibleEvents = new([], SourceLocation.Start) }).GetRawText().ShouldEqual(baseline);
        baseline.Contains("possibleEvents", StringComparison.Ordinal).ShouldBeFalse();
    }

    [Fact]
    void should_visit_each_contract_edge_once()
    {
        var parsed = _compiler.Parse(Prefix + Declaration + "          implementation\n            hint \"Keep order\"\n            file C.cs");
        var walker = new CountingWalker();
        walker.VisitApplication(parsed.Value!);
        walker.Nodes.OfType<PossibleEventsSyntax>().Count().ShouldEqual(1);
        walker.Nodes.OfType<PossibleEventSyntax>().Count().ShouldEqual(3);
        walker.Nodes.OfType<ImplementationHintSyntax>().Count().ShouldEqual(1);
        walker.Nodes.OfType<FileReferenceSyntax>().Count().ShouldEqual(1);
    }

    sealed class CountingWalker : ScreenplaySyntaxWalker
    {
        internal List<SyntaxNode> Nodes { get; } = [];
        public override void VisitNode(SyntaxNode node) => Nodes.Add(node);
    }
}
