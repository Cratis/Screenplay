// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
            command with { Properties = command.Properties.Append(candidate.PropertyCandidate! with { Location = SourceLocation.Start }) },
            command with { StreamCandidates = [new("Account", "Onboarding", candidate.Location)] }
        })
        {
            Catch.Exception(() => SyntaxJson.Serialize(invalid)).ShouldBeOfExactType<InvalidSyntaxJson>();
            var replacement = parsed with { Modules = [parsed.Modules.Single() with { Features = [parsed.Modules.Single().Features.Single() with { Slices = [parsed.Modules.Single().Features.Single().Slices.Single() with { Commands = [invalid] }] }] }] };
            Catch.Exception(() => new ScreenplayPrinter().Print(replacement)).ShouldBeOfExactType<InvalidSyntaxJson>();
        }
    }

    static CommandSyntax Command(ApplicationSyntax application) => application.Modules.Single().Features.Single().Slices.Single().Commands.Single();

    sealed class Collector : ScreenplaySyntaxWalker
    {
        internal List<SyntaxNode> Nodes { get; } = [];
        public override void VisitNode(SyntaxNode node) => Nodes.Add(node);
    }
}
