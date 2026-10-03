// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Files;
using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_authoring_operations : given.a_compiler
{
    const string Prefix = "system Mailer\n  description \"External mail\"\nmodule M\n  feature F\n    slice StateChange S\n";
    const string Declaration = "      operation Send\n        uses Mailer\n        recipient String\n        execute\n          implementation\n            hint \"Use the adapter\"\n        compensate\n          description \"Undo\"\n";
    const string Command = "      command C\n        recipient String\n        produces Send\n          recipient = recipient\n";

    [Theory]
    [InlineData(Declaration + Command)]
    [InlineData(Command + Declaration)]
    [InlineData("      command C\n        recipient String\n        produces operation Send\n          uses Mailer\n          recipient String = recipient\n          compensate\n            description \"Undo\"\n")]
    void should_roundtrip_pending_operations(string body)
    {
        var parsed = _compiler.Compile(Prefix + body);
        parsed.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        var reparsed = _compiler.Parse(new ScreenplayPrinter().Print(parsed.Value!));
        reparsed.Success.ShouldBeTrue();
        SyntaxJson.StructurallyEqual(parsed.Value!, reparsed.Value!).ShouldBeTrue();
        SyntaxJson.StructurallyEqual(parsed.Value!, SyntaxJson.Deserialize(SyntaxJson.Serialize(parsed.Value!))).ShouldBeTrue();
    }

    [Fact]
    void should_not_consume_phase_siblings()
    {
        var parsed = _compiler.Compile(Prefix + Declaration + "        extra String optional\n" + Command);
        parsed.Success.ShouldBeTrue();
        var operation = parsed.Value!.Modules.Single().Features.Single().Slices.Single().Operations.Single();
        operation.Inputs.Count().ShouldEqual(2);
        operation.Compensate.ShouldNotBeNull();
    }

    [Theory]
    [InlineData("      operation Send\n        recipient String\n", "PLAY0500")]
    [InlineData("      operation Send\n        uses Unknown\n", "PLAY0500")]
    [InlineData("      operation Send\n        uses Mailer\n        uses Mailer\n", "PLAY0500")]
    [InlineData(Declaration + "      event Send\n", "PLAY0498")]
    [InlineData(Declaration + "      command C\n        produces Send\n", "PLAY0501")]
    [InlineData(Declaration + "      command C\n        produces Send\n          recipient = 42\n", "PLAY0501")]
    [InlineData(Declaration + "      specification T\n        when C\n        then compensated Send\n          ignored = 1\n", "PLAY0502")]
    [InlineData(Declaration + "      reaction R\n        when Recorded\n          produces Send\n", "PLAY0499")]
    void should_reject_invalid_intent(string body, string code) => _compiler.Compile(Prefix + body).Diagnostics.Any(diagnostic => diagnostic.Code == code).ShouldBeTrue();

    [Fact]
    void should_keep_event_destination_defaults_separate()
    {
        var parsed = _compiler.Compile(Prefix + Declaration + "      command C\n        id Uuid identifier\n        recipient String\n        produces event Recorded\n          recipient String = recipient\n        produces Send\n          recipient = recipient\n");
        parsed.Success.ShouldBeTrue();
        parsed.Diagnostics.Any(diagnostic => diagnostic.Code == "PLAY0470" || diagnostic.Code == "PLAY0478").ShouldBeFalse();
    }

    [Fact]
    void should_walk_each_new_node()
    {
        var parsed = _compiler.Compile(Prefix + Declaration + Command + "      specification T\n        given operation Send fails\n        when C\n          recipient = \"Ada\"\n        then operation Send\n          recipient = \"Ada\"\n        then compensated Send\n");
        parsed.Success.ShouldBeTrue();
        var walker = new CountingWalker();
        walker.VisitApplication(parsed.Value!);
        walker.Nodes.Count(node => node.GetType().Name == "SpecificationOperationSyntax").ShouldEqual(1);
        walker.Nodes.OfType<OperationPhaseSyntax>().Count().ShouldEqual(2);
    }

    [Theory]
    [InlineData("system")]
    [InlineData("operation")]
    [InlineData("uses")]
    [InlineData("execute")]
    [InlineData("compensate")]
    void should_preserve_keyword_properties_with_legacy_deeper_indentation(string name)
    {
        var result = _compiler.Compile(Prefix + $"      command C\n        {name} String\n          deeper String\n");
        result.Success.ShouldBeTrue();
        result.Value!.Modules.Single().Features.Single().Slices.Single().Commands.Single().Properties.Select(property => property.Name).ShouldEqual(name, "deeper");
    }

    [Fact]
    void should_retain_exact_unicode_mapping_and_comment_spans_across_crlf()
    {
        const string Source = "system Mailer\r\nmodule M\r\n  feature F\r\n    slice StateChange S\r\n      command C\r\n        produces operation Send\r\n          uses Mailer\r\n          recipient String = \"α𝄞\" // Keep\r\n";
        var result = _compiler.Compile(Source);
        result.Success.ShouldBeTrue();
        var production = result.Value!.Modules.Single().Features.Single().Slices.Single().Commands.Single().Produces.Single();
        var mapping = production.Mappings.Single();
        mapping.SourceLocation!.Line.ShouldEqual(8);
        mapping.SourceLength!.Value.ShouldEqual(5);
        var printed = new ScreenplayPrinter().Print(result.Value!);
        printed.ShouldContain("// Keep");
        _compiler.Compile(printed).Success.ShouldBeTrue();
    }

    [Fact]
    void should_reject_two_inline_declaration_kinds_in_typed_transport()
    {
        var location = SourceLocation.Start;
        var production = new ProducesSyntax("Send", null, [], location)
        {
            InlineEvent = new("Send", [], location),
            InlineOperation = new("Send", "Mailer", [], location)
        };
        Catch.Exception(() => SyntaxJson.Serialize(production)).ShouldBeOfExactType<InvalidSyntaxJson>();
    }

    [Fact]
    void should_default_old_json_members_without_changing_constructor_signatures()
    {
        var syntax = _compiler.Parse("module M\n  feature F\n    slice StateChange S\n      command C\n        produces Recorded\n").Value!;
        var json = SyntaxJson.Serialize(syntax).GetRawText().Replace("\"systems\":[],", string.Empty, StringComparison.Ordinal)
            .Replace("\"operations\":[],", string.Empty, StringComparison.Ordinal).Replace("\"inlineOperation\":null,", string.Empty, StringComparison.Ordinal);
        using var document = JsonDocument.Parse(json);
        var roundtrip = (ApplicationSyntax)SyntaxJson.Deserialize(document.RootElement);
        roundtrip.Systems.ShouldBeEmpty();
        roundtrip.Modules.Single().Features.Single().Slices.Single().Operations.ShouldBeEmpty();
        typeof(ProducesSyntax).GetConstructors().Single().GetParameters().Length.ShouldEqual(6);
    }

    [Fact]
    void should_resolve_placed_operation_declarations_and_application_systems_before_their_producers()
    {
        var result = PlayFolderMerge.Merge([
            _compiler.Parse("slice StateChange Here\n  command C\n    recipient String\n    produces Other.Send\n      recipient = recipient", "a-command.play", new PlayPlacement(["M", "F"])),
            _compiler.Parse("system Mailer\nslice StateChange Other\n  operation Send\n    uses Mailer\n    recipient String", "z-operation.play", new PlayPlacement(["M", "F"]))
        ]);
        result.Success.ShouldBeTrue();
        var resolver = new AuthoringProductionResolver(result.Value!);
        var slice = result.Value!.Modules.Single().Features.Single().Slices.Single(slice => slice.Name == "Here");
        var resolution = resolver.Resolve("Other.Send", slice);
        resolution.Kind.ShouldEqual(AuthoringProductionKind.Operation);
        resolution.Declaration!.Scope.ShouldEqual("M", "F", "Other");
        result.Value.Systems.Single().Name.ShouldEqual("Mailer");
    }

    [Fact]
    void should_choose_the_nearest_combined_kind_and_refuse_ambiguous_qualified_suffixes()
    {
        const string Source = "system Mailer\nmodule First\n  feature F\n    slice StateChange Other\n      operation Send\n        uses Mailer\n      command C\n        produces Send\nmodule Second\n  feature F\n    slice StateChange Other\n      event Send\n";
        var syntax = _compiler.Parse(Source).Value!;
        var owner = syntax.Modules.First().Features.Single().Slices.Single();
        var resolver = new AuthoringProductionResolver(syntax);
        resolver.Resolve("Send", owner).Kind.ShouldEqual(AuthoringProductionKind.Operation);
        resolver.Resolve("Send", owner with { }).Kind.ShouldEqual(AuthoringProductionKind.Unresolved);
        var ambiguous = resolver.Resolve("Other.Send", owner);
        ambiguous.Kind.ShouldEqual(AuthoringProductionKind.Ambiguous);
        ambiguous.Candidates.Count.ShouldEqual(2);
        ambiguous.Candidates.Select(candidate => candidate.Scope[0]).ShouldEqual("First", "Second");
    }

    [Theory]
    [InlineData("{}", false)]
    [InlineData("{\"recipient\":\"Ada\"}", true)]
    void should_require_complete_known_composite_production_inputs(string value, bool success)
    {
        var result = _compiler.Compile("type Contact\n  recipient String\n" + Prefix + "      operation Send\n        uses Mailer\n        contact Contact\n      command C\n        produces Send\n          contact = " + value);
        result.Success.ShouldEqual(success);
    }

    [Fact]
    void should_not_compute_event_defaults_for_ambiguous_operation_targets()
    {
        const string Source = "system Mailer\nmodule M\n  feature F\n    slice StateChange A\n      operation Send\n        uses Mailer\n    slice StateChange B\n      operation Send\n        uses Mailer\n    slice StateChange Here\n      command C\n        id Uuid identifier\n        produces event Recorded\n        produces Send\n";
        var result = _compiler.Compile(Source);
        result.Diagnostics.Any(diagnostic => diagnostic.Code == "PLAY0497").ShouldBeTrue();
        result.Diagnostics.Any(diagnostic => diagnostic.Code == "PLAY0470" || diagnostic.Code == "PLAY0478" || diagnostic.Code == "PLAY0166").ShouldBeFalse();
    }

    sealed class CountingWalker : ScreenplaySyntaxWalker
    {
        internal List<SyntaxNode> Nodes { get; } = [];
        public override void VisitNode(SyntaxNode node) => Nodes.Add(node);
    }
}
