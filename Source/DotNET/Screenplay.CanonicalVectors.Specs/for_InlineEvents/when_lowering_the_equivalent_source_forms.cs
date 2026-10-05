// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.CanonicalVectors.for_InlineEvents;

public class when_lowering_the_equivalent_source_forms : Specification
{
    byte[] _inline = [];
    byte[] _explicit = [];

    void Because()
    {
        _inline = Compile("inline");
        _explicit = Compile("explicit");
    }

    [Fact] void should_have_identical_canonical_bytes() => _inline.SequenceEqual(_explicit).ShouldBeTrue();
    [Fact] void should_round_trip_without_a_new_version() => SemanticModelSerializer.Serialize(SemanticModelSerializer.Deserialize(_inline)).SequenceEqual(_explicit).ShouldBeTrue();

    static byte[] Compile(string form)
    {
        using var stream = typeof(RegisterProjectCorpus).Assembly.GetManifestResourceStream($"Cratis.Screenplay.CanonicalCorpus.Corpus.InlineEvents.{form}.play")!;
        using var reader = new StreamReader(stream);
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("source"), "source", "application.play", reader.ReadToEnd());
        var result = new SemanticModelCompiler().Compile("Projects", SemanticDocumentSet.Create([document], catalog));
        result.Success.ShouldBeTrue();
        return SemanticModelSerializer.Serialize(result.Value!.Model);
    }
}
