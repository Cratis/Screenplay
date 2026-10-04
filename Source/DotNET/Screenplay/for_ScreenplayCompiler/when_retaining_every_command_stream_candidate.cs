// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Files;
using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_retaining_every_command_stream_candidate
{
    const string Declarations = "import Account.Transactions\ntype Transactions\n  value String\neventsource Account\n  stream Onboarding\n  stream Transactions\n";
    const string Prefix = "module M\n  feature F\n    slice StateChange S\n      command C\n";
    const string Ambiguous = "        stream Account.Transactions // ambiguous\n          deeper String // legacy\n";
    const string Resolved = "        stream Account.Onboarding // resolved\n";

    [Theory]
    [InlineData(Resolved + Ambiguous, 1, true)]
    [InlineData(Ambiguous + Resolved, 1, true)]
    [InlineData(Ambiguous + Ambiguous + Ambiguous, 3, false)]
    [InlineData(Resolved + Resolved, 1, true)]
    void should_transport_every_rejected_header_and_refuse_lossy_export(string body, int candidates, bool hasRoute)
    {
        var compiler = new ScreenplayCompiler();
        var result = compiler.Compile((Declarations + Prefix + body).Replace("\n", "\r\n", StringComparison.Ordinal));
        result.Success.ShouldBeFalse();
        var command = Command(result.Value!);
        command.StreamCandidates.Count().ShouldEqual(candidates);
        (command.Stream is not null).ShouldEqual(hasRoute);
        if (body.Contains(Ambiguous, StringComparison.Ordinal)) result.Diagnostics.Any(diagnostic => diagnostic.Code == "PLAY0505").ShouldBeTrue();
        else result.Diagnostics.Any(diagnostic => diagnostic.Code == "PLAY0504").ShouldBeTrue();
        command.Properties.Count().ShouldEqual(command.StreamCandidates.Count(candidate => candidate.PropertyCandidate is not null));
        command.Properties.All(property => property.Name == "deeper").ShouldBeTrue();

        var walker = new Collector();
        walker.VisitCommand(command);
        var properties = walker.Nodes.OfType<PropertySyntax>().ToArray();
        properties.Length.ShouldEqual(command.Properties.Count() + command.StreamCandidates.Count(candidate => candidate.PropertyCandidate is not null));
        properties.Select(property => property.Location.Line).Distinct().Count().ShouldEqual(properties.Length);
        properties.SelectMany(property => property.SourceComments).Count(comment => comment.Text.Contains("legacy", StringComparison.Ordinal)).ShouldEqual(command.Properties.Count());
        var locations = command.StreamCandidates.Select(candidate => candidate.Location.Line).ToArray();
        locations.Distinct().Count().ShouldEqual(locations.Length);

        var decoded = (ApplicationSyntax)SyntaxJson.Deserialize(SyntaxJson.Serialize(result.Value!));
        SyntaxJson.StructurallyEqual(result.Value!, decoded).ShouldBeTrue();
        Command(decoded).StreamCandidates.Count().ShouldEqual(candidates);
        Catch.Exception(() => new ScreenplayPrinter().Print(decoded)).ShouldBeOfExactType<InvalidSyntaxJson>();
        Catch.Exception(() => _ = new PlayFileWriter().Expand(decoded).ToArray()).ShouldBeOfExactType<InvalidSyntaxJson>();

        var texts = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["application.play"] = Declarations + "module M\n  feature F\n    import \"barrel.play\"",
            ["barrel.play"] = "import \"command.play\"",
            ["command.play"] = "slice StateChange S\n  command C\n" + body.Replace("        ", "    ", StringComparison.Ordinal)
        };
        foreach (var roots in new[] { texts.Keys.AsEnumerable(), texts.Keys.Reverse() })
        {
            var assembled = PlayApplicationAssembly.Compile(compiler, roots, new InMemoryPlayDocumentSource(texts)).Result;
            assembled.Success.ShouldBeFalse();
            Command(assembled.Value!).StreamCandidates.Count().ShouldEqual(candidates);
            Catch.Exception(() => _ = new PlayFileWriter().Expand((ApplicationSyntax)SyntaxJson.Deserialize(SyntaxJson.Serialize(assembled.Value!))).ToArray()).ShouldBeOfExactType<InvalidSyntaxJson>();
        }
    }

    [Fact]
    void should_refuse_programmatic_authoritative_ambiguity_and_duplicate_property_ownership()
    {
        var parsed = new ScreenplayCompiler().Compile(Declarations + Prefix + Ambiguous).Value!;
        var command = Command(parsed);
        var candidate = command.StreamCandidates.Single();
        foreach (var invalid in new[]
        {
            command with { Stream = candidate, StreamCandidates = [] },
            command with { Properties = command.Properties.Append(candidate.PropertyCandidate!) },
            command with { StreamCandidates = [new("Account", "Onboarding", candidate.Location)] }
        })
        {
            Catch.Exception(() => SyntaxJson.Serialize(invalid)).ShouldBeOfExactType<InvalidSyntaxJson>();
            var replacement = parsed with { Modules = [parsed.Modules.Single() with { Features = [parsed.Modules.Single().Features.Single() with { Slices = [parsed.Modules.Single().Features.Single().Slices.Single() with { Commands = [invalid] }] }] }] };
            Catch.Exception(() => new ScreenplayPrinter().Print(replacement)).ShouldBeOfExactType<InvalidSyntaxJson>();
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_transport_independent_equal_properties_in_either_header_order(bool reverse)
    {
        const string Escaped = "        @stream Account.Transactions\n";
        const string Bare = "        stream Account.Transactions\n";
        var result = new ScreenplayCompiler().Compile(Declarations + Prefix + (reverse ? Bare + Escaped : Escaped + Bare));
        result.Success.ShouldBeFalse();
        result.Diagnostics.Any(diagnostic => diagnostic.Code == "PLAY0505").ShouldBeTrue();
        var command = Command(result.Value!);
        command.Stream.ShouldBeNull();
        var property = command.Properties.Single();
        var candidate = command.StreamCandidates.Single().PropertyCandidate!;
        ReferenceEquals(property, candidate).ShouldBeFalse();
        property.NameWasEscaped.ShouldBeTrue();
        SyntaxJson.StructurallyEqual(property, candidate).ShouldBeTrue();
        var json = SyntaxJson.Serialize(result.Value!);
        json.GetRawText().ShouldNotContain("nameWasEscaped");
        var decoded = (ApplicationSyntax)SyntaxJson.Deserialize(json);
        SyntaxJson.StructurallyEqual(result.Value!, decoded).ShouldBeTrue();
        RefuseExport(decoded);

        // Even record-equal copies with identical source metadata have separate ownership.
        var copy = candidate with { };
        (copy == candidate).ShouldBeTrue();
        ReferenceEquals(copy, candidate).ShouldBeFalse();
        var independent = command with { Properties = [copy] };
        SyntaxJson.StructurallyEqual(independent, SyntaxJson.Deserialize(SyntaxJson.Serialize(independent))).ShouldBeTrue();
    }

    [Fact]
    void should_deserialize_and_reserialize_supplied_json_with_two_equal_physical_properties()
    {
        using var supplied = JsonDocument.Parse("""
            {"kind":"CommandSyntax","name":"C","authorize":null,"handler":null,
             "properties":[{"kind":"PropertySyntax","name":"stream","type":{"kind":"TypeRefSyntax","name":"Account.Transactions","isCollection":false,"isOptional":false},"isGenerated":false,"isIdentifier":false}],
             "stream":null,"streamCandidates":[{"kind":"CommandStreamSyntax","eventSource":"Account","stream":"Transactions","streamId":null,
             "propertyCandidate":{"kind":"PropertySyntax","name":"stream","type":{"kind":"TypeRefSyntax","name":"Account.Transactions","isCollection":false,"isOptional":false},"isGenerated":false,"isIdentifier":false}}]}
            """);
        var decoded = (CommandSyntax)SyntaxJson.Deserialize(supplied.RootElement);
        var property = decoded.Properties.Single();
        var candidate = decoded.StreamCandidates.Single().PropertyCandidate!;
        (property == candidate).ShouldBeTrue();
        ReferenceEquals(property, candidate).ShouldBeFalse();
        var reserialized = SyntaxJson.Serialize(decoded);
        SyntaxJson.StructurallyEqual(decoded, SyntaxJson.Deserialize(reserialized)).ShouldBeTrue();
        var draft = new ScreenplayCompiler().Compile(Declarations + Prefix + Ambiguous);
        draft.Success.ShouldBeFalse();
        draft.Diagnostics.Any(diagnostic => diagnostic.Code == "PLAY0505").ShouldBeTrue();
        RefuseExport(ReplaceCommand(draft.Value!, decoded));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_preserve_repeated_candidates_without_deduplicating_or_promoting_them(bool sameInstance)
    {
        var draft = new ScreenplayCompiler().Compile(Declarations + Prefix + Ambiguous + Ambiguous);
        draft.Success.ShouldBeFalse();
        draft.Diagnostics.Count(diagnostic => diagnostic.Code == "PLAY0505").ShouldEqual(2);
        var command = Command(draft.Value!);
        var candidates = command.StreamCandidates.ToArray();
        ReferenceEquals(candidates[0], candidates[1]).ShouldBeFalse();
        ReferenceEquals(candidates[0].PropertyCandidate, candidates[1].PropertyCandidate).ShouldBeFalse();
        SyntaxJson.StructurallyEqual(candidates[0], candidates[1]).ShouldBeTrue();
        var repeated = command with
        {
            Properties = [candidates[0].PropertyCandidate! with { }],
            StreamCandidates = [candidates[0], sameInstance ? candidates[0] : candidates[0] with { PropertyCandidate = candidates[0].PropertyCandidate! with { } }]
        };
        var decoded = (CommandSyntax)SyntaxJson.Deserialize(SyntaxJson.Serialize(repeated));
        decoded.Stream.ShouldBeNull();
        decoded.StreamCandidates.Count().ShouldEqual(2);
        SyntaxJson.StructurallyEqual(repeated, decoded).ShouldBeTrue();
        RefuseExport(ReplaceCommand(draft.Value!, decoded));
    }

    static void RefuseExport(ApplicationSyntax application)
    {
        Catch.Exception(() => new ScreenplayPrinter().Print(application)).ShouldBeOfExactType<InvalidSyntaxJson>();
        Catch.Exception(() => _ = new PlayFileWriter().Expand(application).ToArray()).ShouldBeOfExactType<InvalidSyntaxJson>();
    }

    static ApplicationSyntax ReplaceCommand(ApplicationSyntax application, CommandSyntax command)
    {
        var module = application.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Single();

        return application with { Modules = [module with { Features = [feature with { Slices = [slice with { Commands = [command] }] }] }] };
    }

    static CommandSyntax Command(ApplicationSyntax application) => application.Modules.Single().Features.Single().Slices.Single().Commands.Single();

    sealed class Collector : ScreenplaySyntaxWalker
    {
        internal List<SyntaxNode> Nodes { get; } = [];
        public override void VisitNode(SyntaxNode node) => Nodes.Add(node);
    }
}
