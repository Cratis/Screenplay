// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_AttachedCommandHandlersCorpus;

public class when_loading_attached_handlers : Specification
{
    [Theory]
    [InlineData("inline.play")]
    [InlineData("file.play")]
    void should_bind_the_attached_contract(string fixture)
    {
        var result = Compile(Read(fixture), ImmutableDictionary<string, string>.Empty.Add("Place.ts", Read("Place.ts")));
        Assert.True(result.Success, string.Join('\n', result.Diagnostics.Select(diagnostic => diagnostic.Message)));
        result.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V23);
        result.ImplementationRequirements.Single().AttachmentResolution.ShouldEqual(SemanticAttachmentResolution.Resolved);
        result.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.DescribedCommandNotImplemented).ShouldBeFalse();
    }

    [Fact]
    void should_keep_pending_v23_model_bytes()
    {
        var attached = Read("inline.play");
        var pending = attached[..attached.IndexOf("          ```", StringComparison.Ordinal)] + "          implementation\n";
        SemanticModelSerializer.Serialize(Compile(attached).Value!.Model).ShouldEqual(SemanticModelSerializer.Serialize(Compile(pending).Value!.Model));
    }

    [Fact]
    void should_refuse_a_file_the_host_did_not_supply() => Compile(Read("file.play")).Diagnostics.ShouldContain(diagnostic => diagnostic.Code == DiagnosticCodes.CommandHandlerRequiresAttachment);

    [Theory]
    [InlineData("        produces Placed\n", "")]
    [InlineData("produces Placed", "produces Placed when true")]
    void should_refuse_unconstrained_results(string before, string after) => Compile(Read("inline.play").Replace(before, after, StringComparison.Ordinal)).Success.ShouldBeFalse();

    static CompilationResult<SemanticCompilation> Compile(string source, ImmutableDictionary<string, string>? contents = null)
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Attached"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("fixture"), "fixture", "fixture.play", source);
        return new SemanticModelCompiler().Compile("Attached", SemanticDocumentSet.Create([document], catalog, contents));
    }

    static string Read(string name)
    {
        using var stream = typeof(DescribedCommandHandlersCorpus).Assembly.GetManifestResourceStream("Cratis.Screenplay.CanonicalCorpus.Corpus.AttachedCommandHandlers." + name)!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
