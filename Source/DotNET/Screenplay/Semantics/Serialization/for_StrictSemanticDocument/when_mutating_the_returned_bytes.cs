// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Semantics.Serialization.for_StrictSemanticDocument;

public class when_mutating_the_returned_bytes : Specification
{
    StrictSemanticDocument _document;
    byte[] _first;
    byte[] _second;
    SemanticRevision _expectedRevision;

    void Establish()
    {
        using var json = JsonDocument.Parse(canonical_serialization_golden_vectors.EsmV34Bytes);
        _expectedRevision = SemanticRevision.Parse(json.RootElement.GetProperty("revision").GetString()!);
    }

    void Because()
    {
        _document = StrictSemanticDocument.Read(canonical_serialization_golden_vectors.EsmV34Bytes);
        _first = _document.CopyBytes();
        Array.Fill(_first, (byte)0);
        _second = _document.CopyBytes();
    }

    [Fact] void should_keep_the_original_bytes() => _second.ShouldEqual(canonical_serialization_golden_vectors.EsmV34Bytes);
    [Fact] void should_return_a_new_array() => _second.ShouldNotBeSame(_first);
    [Fact] void should_keep_the_document_revision() => _document.Revision.ShouldEqual(_expectedRevision);
}
