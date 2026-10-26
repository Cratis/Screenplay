// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Semantics.Serialization.for_StrictSemanticDocument;

public class when_mutating_the_input_after_reading : Specification
{
    byte[] _input;
    StrictSemanticDocument _document;
    SemanticRevision _expectedRevision;

    void Establish()
    {
        _input = [.. canonical_serialization_golden_vectors.EsmV34Bytes];
        using var json = JsonDocument.Parse(_input);
        _expectedRevision = SemanticRevision.Parse(json.RootElement.GetProperty("revision").GetString()!);
    }

    void Because()
    {
        _document = StrictSemanticDocument.Read(_input);
        Array.Fill(_input, (byte)0);
    }

    [Fact] void should_keep_the_original_bytes() => _document.CopyBytes().ShouldEqual(canonical_serialization_golden_vectors.EsmV34Bytes);
    [Fact] void should_keep_the_document_revision() => _document.Revision.ShouldEqual(_expectedRevision);
}
