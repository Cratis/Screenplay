// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Files;
using Cratis.Screenplay.Parsing;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_holding_operation_regressions_to_shared_vectors : given.a_compiler
{
    static JsonElement Vectors()
    {
        var root = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "AGENTS.md"))) root = root.Parent;
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(root!.FullName, "Source", "Screenplay", "Compiler", "Conformance", "operation-regressions.json")));

        return document.RootElement.Clone();
    }

    [Fact]
    void should_preserve_each_shared_resolution_mapping_and_event_outcome_contract()
    {
        var vectors = Vectors();
        foreach (var vector in vectors.GetProperty("cases").EnumerateArray())
        {
            var before = vector.TryGetProperty("before", out var prefix) ? prefix.GetString() : string.Empty;
            var result = _compiler.Compile(before + vectors.GetProperty("prefix").GetString() + vector.GetProperty("body").GetString());
            var codes = result.Diagnostics.Select(diagnostic => diagnostic.Code).ToHashSet(StringComparer.Ordinal);
            var expected = vector.GetProperty("codes").EnumerateArray().Select(code => code.GetString()!);
            if (vector.TryGetProperty("csharpCodes", out var csharp)) expected = expected.Concat(csharp.EnumerateArray().Select(code => code.GetString()!));
            foreach (var code in expected) codes.Contains(code).ShouldBeTrue();
            foreach (var code in vector.GetProperty("absent").EnumerateArray()) codes.Contains(code.GetString()!).ShouldBeFalse();
        }
    }

    [Fact]
    void should_retain_parser_owned_target_spans_identically_to_typescript()
    {
        var vectors = Vectors();
        foreach (var vector in vectors.GetProperty("targetSpans").EnumerateArray())
        {
            var source = vectors.GetProperty("prefix").GetString() + vector.GetProperty("body").GetString();
            var result = _compiler.Compile(source);
            result.Success.ShouldBeTrue();
            var production = result.Value!.Modules.Single().Features.Single().Slices.Single().Commands.Single().Produces.Single();
            production.TargetLocation.Line.ShouldEqual(vector.GetProperty("line").GetInt32());
            production.TargetLocation.Column.ShouldEqual(vector.GetProperty("column").GetInt32());
            source.Split('\n')[production.TargetLocation.Line - 1].Substring(production.TargetLocation.Column - 1, production.Event.Length).ShouldEqual(production.Event);
        }
    }

    [Fact]
    void should_retain_both_declaration_occurrences_and_exclude_mixed_ambiguity_from_event_consumers()
    {
        var vectors = Vectors();
        var result = _compiler.Compile(vectors.GetProperty("prefix").GetString() + vectors.GetProperty("cases")[0].GetProperty("body").GetString());
        var slice = result.Value!.Modules.Single().Features.Single().Slices.Single();
        var resolver = new AuthoringProductionResolver(result.Value);
        var resolution = resolver.Resolve("Send", slice);
        resolution.Candidates.Select(candidate => candidate.Kind).ShouldEqual(AuthoringProductionKind.Event, AuthoringProductionKind.Operation);
        resolver.IsEventProduction(slice.Commands.Single().Produces.Last(), slice).ShouldBeFalse();
    }

    [Fact]
    void should_validate_cross_file_mixed_productions_only_after_assembly()
    {
        var documents = Vectors().GetProperty("documents").EnumerateArray().ToDictionary(document => document.GetProperty("path").GetString()!, document => document.GetProperty("source").GetString()!, StringComparer.Ordinal);
        var (_, result) = PlayApplicationAssembly.Compile(_compiler, ["root.play"], new InMemoryPlayDocumentSource(documents));
        result.Diagnostics.ShouldBeEmpty();
    }

    [Fact]
    void should_reject_programmatic_nonconcrete_assertions_even_when_the_target_shape_is_unknown()
    {
        var syntax = _compiler.Parse("import External.Payload\nsystem Mailer\nmodule M\n  feature F\n    slice StateChange S\n      operation Send\n        uses Mailer\n        payload Payload\n      command C\n        produces Send\n          payload = {}\n      specification T\n        when C\n        then operation Send\n          payload.unknown = null\n").Value!;
        var module = syntax.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Single();
        var specification = slice.Specifications.Single();
        var assertion = specification.ThenOperations.Single();
        var mapping = assertion.Values.Single();
        var updated = syntax with { Modules = [module with { Features = [feature with { Slices = [slice with { Specifications = [specification with { ThenOperations = [assertion with { Values = [mapping with { Source = new RawExpressionSyntax("missing", mapping.Location) }] }] }] }] }] }] };
        var context = new ParserContext(new LineReader([]));
        var declarations = new ConsistencyDeclarations(updated, [(updated.Modules.Single().Features.Single().Slices.Single(), new(["M", "F", "S"]))]);
        OperationValidator.Validate(updated, declarations, context);
        context.Diagnostics.Any(diagnostic => diagnostic.Code == "PLAY0502").ShouldBeTrue();
    }

    [Fact]
    void should_retain_exact_assertion_rhs_spans()
    {
        const string Source = "system Mailer\nmodule M\n  feature F\n    slice StateChange S\n      operation Send\n        uses Mailer\n        name String\n      command C\n        produces Send\n          name = \"Ada\"\n      specification T\n        when C\n        then operation Send\n          name = \"α𝄞\"\n";
        var mapping = _compiler.Compile(Source).Value!.Modules.Single().Features.Single().Slices.Single().Specifications.Single().ThenOperations.Single().Values.Single();
        mapping.Source.Location.Line.ShouldEqual(14);
        mapping.Source.Location.Column.ShouldEqual(18);
        mapping.SourceLocation!.Column.ShouldEqual(18);
        mapping.SourceLength!.Value.ShouldEqual(5);
    }
}
