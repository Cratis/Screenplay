// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Files;
using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax.Serialization;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Syntax.for_SourceOptions;

public class when_authoring_exact_documents : Specification
{
    readonly ScreenplayCompiler _compiler = new();
    readonly ScreenplayPrinter _printer = new();

    const string Model = "module M\n  feature F\n    slice StateChange S\n      command C\n        amount Decimal\n        produces E\n          amount = 9007199254740993\n      event E\n        amount Decimal\n      specification T\n        when C\n          amount = 9007199254740993\n        then E\n          amount = 9007199254740993\n";

    [Theory]
    [InlineData("numbers exact\n", true)]
    [InlineData("\uFEFF// comment\n\nnumbers exact // comment\n", true)]
    [InlineData("// numbers exact\n", false)]
    void should_select_options_before_any_application_values(string prefix, bool exact)
    {
        var result = _compiler.Compile(prefix + Model);
        result.Success.ShouldBeTrue();
        result.Value!.SourceOptions.NumericMode.ShouldEqual(exact ? NumericMode.Exact : NumericMode.Legacy);
        var literal = (LiteralExpressionSyntax)result.Value.Modules.Single().Features.Single().Slices.Single().Commands.Single().Produces.Single().Mappings.Single().Source;
        if (exact) ((ExactNumber)literal.Value!).CanonicalText.ShouldEqual("9007199254740993");
        else literal.Value.ShouldBeOfExactType<double>();
    }

    [Theory]
    [InlineData("numbers legacy\n", "PLAY0508", 1)]
    [InlineData("numbers exact\nnumbers exact\n", "PLAY0509", 2)]
    [InlineData("domain Billing\nnumbers exact\n", "PLAY0510", 2)]
    [InlineData("import Other.Amount\nnumbers exact\n", "PLAY0510", 2)]
    void should_refuse_unknown_duplicate_and_late_preambles(string prefix, string code, int line)
    {
        var result = _compiler.Parse(prefix + Model, "input.play");
        result.Success.ShouldBeFalse();
        var diagnostic = result.Diagnostics.Single(value => value.Code == code);
        diagnostic.Location.Line.ShouldEqual(line);
        diagnostic.Location.Column.ShouldEqual(1);
        diagnostic.Location.Path.ShouldEqual("input.play");
        Catch.Exception(() => _printer.Print(result.Value!)).ShouldBeOfExactType<InvalidSyntaxJson>();
        Catch.Exception(() => SyntaxJson.Serialize(result.Value!)).ShouldBeOfExactType<InvalidSyntaxJson>();
    }

    [Fact]
    void should_not_steal_a_property_named_numbers_or_fenced_text()
    {
        var result = _compiler.Compile("numbers exact\nconcept exact : String\nmodule M\n  feature F\n    slice StateChange S\n      command C\n        numbers exact\n        handler\n          ```csharp\nnumbers exact\n          ```\n");
        result.Diagnostics.Any(value => value.Code == DiagnosticCodes.InvalidNumericDirective || value.Code == DiagnosticCodes.DuplicateNumericDirective || value.Code == DiagnosticCodes.LateNumericDirective).ShouldBeFalse();
        result.Value!.Modules.Single().Features.Single().Slices.Single().Commands.Single().Properties.Single().Name.ShouldEqual("numbers");
        var nested = _compiler.Parse("module M\n  numbers exact\n");
        nested.Success.ShouldBeFalse();
        nested.Value!.SourceOptions.ShouldEqual(SourceOptions.Legacy);
    }

    [Theory]
    [InlineData("1e-29")]
    [InlineData("1e29")]
    [InlineData("1e308")]
    [InlineData("1.00000000000000000000000000001")]
    void should_refuse_complete_unrepresentable_tokens_and_refuse_printing_the_failed_tree(string token)
    {
        var result = _compiler.CompileProjection($"numbers exact\nprojection P\n  from E\n    amount = {token}\n");
        result.Success.ShouldBeFalse();
        result.Diagnostics.ShouldContain(value => value.Code == DiagnosticCodes.InexactNumericLiteral);
        Catch.Exception(() => _printer.Print(result.Value!)).ShouldBeOfExactType<InvalidSyntaxJson>();
    }

    [Fact]
    void should_keep_recursive_tokens_and_envelope_shaped_business_members_lossless()
    {
        const string Payload = "{\"literalType\":\"ExactNumber\",\"value\":\"1e29\",\"amount\":9007199254740993,\"items\":[-0,1e-28,\"9007199254740993\"]}";
        var parsed = _compiler.CompileProjection("numbers exact\nprojection P\n  from E\n    amount = " + Payload + "\n");
        parsed.Success.ShouldBeTrue();
        var json = SyntaxJson.Serialize(parsed.Value!);
        json.GetRawText().ShouldContain("9007199254740993");
        var restored = (Projections.ProjectionSyntax)SyntaxJson.Deserialize(json);
        var printed = _printer.Print(restored);
        printed.StartsWith("numbers exact\n", StringComparison.Ordinal).ShouldBeTrue();
        printed.ShouldContain("\"value\":\"1e29\"");
        var reparsed = _compiler.CompileProjection(printed);
        reparsed.Success.ShouldBeTrue();
        SyntaxJson.StructurallyEqual(restored, reparsed.Value!).ShouldBeTrue();
        var bad = _compiler.CompileProjection("numbers exact\nprojection P\n  from E\n    amount = {\"items\":[1e-29]}\n");
        bad.Diagnostics.Single(value => value.Code == DiagnosticCodes.InexactNumericLiteral).Location.Column.ShouldEqual(24);
    }

    [Fact]
    void should_preserve_standalone_projection_specification_and_capture_options_through_typed_json_and_printing()
    {
        var roots = new SyntaxNode[]
        {
            _compiler.CompileProjection("\uFEFF# comment\nnumbers exact\nprojection P\n  from E\n    amount = literal 1e+3\n").Value!,
            _compiler.CompileSpecification("numbers exact\nspecification T\n  when C\n    amount = 1e-28\n  then E\n    amount = 1e-28\n").Value!,
            _compiler.CompileCapture("numbers exact\ncapture C\n  source api\n  append E\n    when added\n    amount = 9223372036854775808\n").Value!
        };
        foreach (var root in roots)
        {
            ((ISourceSyntax)root).SourceOptions.ShouldEqual(SourceOptions.Exact);
            var restored = SyntaxJson.Deserialize(SyntaxJson.Serialize(root));
            var printed = restored switch
            {
                Projections.ProjectionSyntax projection => _printer.Print(projection),
                Specifications.SpecificationSyntax specification => _printer.Print(specification),
                Captures.CaptureSyntax capture => _printer.Print(capture),
                _ => string.Empty
            };
            printed.StartsWith("numbers exact\n", StringComparison.Ordinal).ShouldBeTrue();
            printed.Split("numbers exact", StringSplitOptions.None).Length.ShouldEqual(2);
        }
    }

    [Fact]
    void should_preserve_application_and_expanded_folder_provenance_and_omit_all_legacy_option_fields()
    {
        var legacy = _compiler.Parse(Model).Value!;
        SyntaxJson.Serialize(legacy).GetRawText().ShouldNotContain("sourceOptions");
        var exact = _compiler.Parse("numbers exact\n" + Model).Value!;
        var restored = (ApplicationSyntax)SyntaxJson.Deserialize(SyntaxJson.Serialize(exact));
        SyntaxJson.StructurallyEqual(restored, _compiler.Parse(_printer.Print(restored)).Value!).ShouldBeTrue();
        var files = WorkspaceFolderLayout.Expand(restored, _printer).ToArray();
        files.All(file => file.Content.StartsWith("numbers exact\n", StringComparison.Ordinal)).ShouldBeTrue();
        var documents = files.Select(file => _compiler.Parse(file.Content, file.RelativePath)).ToArray();
        var merged = PlayFolderMerge.Merge(documents);
        merged.Success.ShouldBeTrue();
        merged.Value!.SourceOptions.ShouldEqual(SourceOptions.Exact);
        SyntaxJson.StructurallyEqual(restored, merged.Value).ShouldBeTrue();
    }

    [Fact]
    void should_require_independent_modes_for_real_declarations_but_not_synthetic_placement_barrels()
    {
        var neutral = _compiler.Parse("import \"a.play\"\n", "barrel.play", new(["M", "F"]));
        var exact = _compiler.Parse("numbers exact\nconcept Amount : Decimal\n", "a.play");
        PlayFolderMerge.Merge([neutral, exact]).Value!.SourceOptions.ShouldEqual(SourceOptions.Exact);
        var authored = _compiler.Parse("module M\n", "module.play");
        PlayFolderMerge.Merge([authored, exact]).Diagnostics.ShouldContain(value => value.Code == DiagnosticCodes.MixedNumericModes);
        var asserted = _compiler.Parse("numbers exact\nimport \"a.play\"\n", "barrel.play");
        var unmarked = _compiler.Parse("concept Amount : Decimal\n", "a.play");
        PlayFolderMerge.Merge([asserted, unmarked]).Diagnostics.ShouldContain(value => value.Code == DiagnosticCodes.MixedNumericModes);
        ScreenplayCompiler.DiscoverImports("numbers exact\nmodule M\n  feature F\n    import \"*.play\"\n", "root.play").Count.ShouldEqual(1);
    }

    [Fact]
    void should_refuse_unknown_options_plain_numeric_transport_and_old_numeric_envelopes_in_exact_roots()
    {
        var exact = _compiler.Parse("numbers exact\n" + Model).Value!;
        var text = SyntaxJson.Serialize(exact).GetRawText();
        var plain = text.Replace("{\"literalType\":\"ExactNumber\",\"value\":\"9007199254740993\"}", "9007199254740993", StringComparison.Ordinal);
        using var plainJson = JsonDocument.Parse(plain);
        Catch.Exception(() => SyntaxJson.Deserialize(plainJson.RootElement)).ShouldBeOfExactType<InvalidSyntaxJson>();
        var oldEnvelope = text.Replace("\"literalType\":\"ExactNumber\"", "\"literalType\":\"Decimal\"", StringComparison.Ordinal);
        using var oldJson = JsonDocument.Parse(oldEnvelope);
        Catch.Exception(() => SyntaxJson.Deserialize(oldJson.RootElement)).ShouldBeOfExactType<InvalidSyntaxJson>();
        foreach (var invalid in new[] { "{}", "{\"numericMode\":\"EXACT\"}", "{\"numericMode\":\"exact\",\"other\":true}", "null" })
        {
            var changed = JsonNode.Parse(text)!.AsObject();
            changed["sourceOptions"] = JsonNode.Parse(invalid);
            Catch.Exception(() => SyntaxJson.Deserialize(JsonSerializer.SerializeToElement(changed))).ShouldBeOfExactType<InvalidSyntaxJson>();
        }

        var malformed = exact with { SourceOptions = new((NumericMode)99) };
        Catch.Exception(() => SyntaxJson.Serialize(malformed)).ShouldBeOfExactType<InvalidSyntaxJson>();
        Catch.Exception(() => _printer.Print(malformed)).ShouldBeOfExactType<InvalidSyntaxJson>();
        Catch.Exception(() => SyntaxJson.Serialize(exact with { SourceOptions = SourceOptions.Legacy })).ShouldBeOfExactType<InvalidSyntaxJson>();
    }

    [Fact]
    void should_keep_legacy_rounded_equality_but_distinguish_exact_outcomes_and_whole_numbers()
    {
        var source = Model.Replace("9007199254740993", "100000000000000016", StringComparison.Ordinal)
            .Replace("produces E\n          amount = 100000000000000016", "produces E\n          amount = 100000000000000020", StringComparison.Ordinal);
        _compiler.Compile(source).Success.ShouldBeTrue();
        _compiler.Compile("numbers exact\n" + source).Diagnostics.ShouldContain(value => value.Code == DiagnosticCodes.UnreachableSpecificationOutcome);
        var types = new Parsing.ResponseValueTypes(_compiler.Parse("concept Whole : Int\n").Value!);
        var whole = new TypeRefSyntax("Whole", false, false, SourceLocation.Start);
        ExactNumber.TryParse("9223372036854775808", out var integral).ShouldBeTrue();
        ExactNumber.TryParse("1.0000000000000000000000000001", out var fraction).ShouldBeTrue();
        types.Compatible(new LiteralExpressionSyntax(integral, SourceLocation.Start), whole).ShouldBeTrue();
        types.Compatible(new LiteralExpressionSyntax(fraction, SourceLocation.Start), whole).ShouldBeFalse();
        types.Compatible(new LiteralExpressionSyntax(1d, SourceLocation.Start), whole).ShouldBeTrue();
    }

    [Fact]
    void should_assemble_quoted_import_diamonds_and_globs_without_inheriting_mode()
    {
        var source = new InMemoryPlayDocumentSource(new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["root.play"] = "import \"a.play\"\nimport \"b.play\"\n",
            ["a.play"] = "numbers exact\nimport \"shared.play\"\n",
            ["b.play"] = "import \"shared.play\"\n",
            ["shared.play"] = "numbers exact\nconcept Amount : Decimal\n"
        });
        var (_, diamond) = PlayApplicationAssembly.Compile(_compiler, ["root.play"], source);
        diamond.Success.ShouldBeTrue();
        diamond.Value!.Concepts.Count().ShouldEqual(1);
        diamond.Value.SourceOptions.ShouldEqual(SourceOptions.Exact);
        var globs = new InMemoryPlayDocumentSource(new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["root.play"] = "numbers exact\nimport \"parts/*.play\"\n",
            ["parts/a.play"] = "numbers exact\nconcept A : Decimal\n",
            ["parts/b.play"] = "concept B : Decimal\n"
        });
        var (_, mixed) = PlayApplicationAssembly.Compile(_compiler, ["root.play"], globs);
        mixed.Diagnostics.ShouldContain(value => value.Code == DiagnosticCodes.MixedNumericModes);
    }

    [Fact]
    void should_refuse_all_exact_documents_at_binding_even_without_numeric_values()
    {
        foreach (var syntax in new[] { _compiler.Parse("numbers exact\nconcept Amount : Decimal\n").Value!, _compiler.Parse("concept Amount : Decimal\n").Value! with { SourceOptions = SourceOptions.Exact } })
        {
            var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Numbers"));
            var document = SemanticSourceDocument.Create(catalog.ResolveDocument("input"), "input", "input.play", "concept Amount : Decimal\n");
            var result = new SemanticModelBinder().Bind("Numbers", syntax, SemanticDocumentSet.Create([document], catalog));
            result.Success.ShouldBeFalse();
            result.Diagnostics.Single().Code.ShouldEqual("PLAY0268");
            result.Diagnostics.Single().Message.ShouldContain("ESM v7");
        }
    }

    [Theory]
    [InlineData("1e+3", true)]
    [InlineData("1e20foo", false)]
    [InlineData("1e3-4", false)]
    void should_read_only_complete_numeric_condition_operands(string token, bool numeric)
    {
        var syntax = _compiler.Parse("numbers exact\n" + Model.Replace("produces E", $"produces when amount == {token}\n          E", StringComparison.Ordinal));
        syntax.Success.ShouldBeTrue();
        var comparison = (ComparisonConditionSyntax)syntax.Value!.Modules.Single().Features.Single().Slices.Single().Commands.Single().Produces.Single().When!;
        (comparison.Right is LiteralExpressionSyntax { Value: ExactNumber }).ShouldEqual(numeric);
        var policy = _compiler.Parse($"numbers exact\npolicy P\n  require claim \"amount\" matches {token}\n");
        policy.Success.ShouldBeTrue();
        var claim = (ClaimConditionSyntax)policy.Value!.Policies.Single().Condition!;
        (claim.Matches is LiteralExpressionSyntax { Value: ExactNumber }).ShouldEqual(numeric);
    }
}
