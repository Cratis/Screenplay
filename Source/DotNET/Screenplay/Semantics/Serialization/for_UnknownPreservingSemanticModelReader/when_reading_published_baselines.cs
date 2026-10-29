// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Cratis.Screenplay.Semantics.Serialization.for_UnknownPreservingSemanticModelReader;

public class when_reading_published_baselines : Specification
{
    UnknownPreservingSemanticDocument[] _documents;
    byte[][] _bytes;

    void Establish() => _bytes = [.. Enumerable.Range(1, 34).Select(major => given.documents.Frozen((uint)major))];

    void Because() => _documents = [.. _bytes.Select((bytes, index) => UnknownPreservingSemanticModelReader.Read(bytes, given.documents.Range((uint)index + 1)))];

    [Fact] void should_preserve_every_published_byte() => _documents.Select((document, index) => document.CopyBytes().SequenceEqual(_bytes[index])).All(value => value).ShouldBeTrue();
    [Fact] void should_discover_no_unknowns() => _documents.All(document => document.Unknowns.IsEmpty).ShouldBeTrue();
    [Fact] void should_preserve_the_published_projection_revision() => _documents.All(document => document.Revision == document.Model.Revision).ShouldBeTrue();
}
#endif
