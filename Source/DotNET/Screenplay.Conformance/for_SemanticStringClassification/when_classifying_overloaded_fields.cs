// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Conformance.for_SemanticStringClassification;

public class when_classifying_overloaded_fields : Specification
{
    SemanticStringKind[] _literalKinds;
    SemanticStringKind[] _identityKinds;
    string _stripped;

    void Because()
    {
        using var literal = JsonDocument.Parse("""{"kind":"eventContext"}""");
        using var reference = JsonDocument.Parse("""{"kind":"eventProperty","property":"sem1:reference"}""");
        _literalKinds =
        [
            SemanticStringClassification.Classify(literal.RootElement, "map", "source"),
            SemanticStringClassification.Classify(literal.RootElement, "captures", "key"),
            SemanticStringClassification.Classify(literal.RootElement, "children", "identifiedBy"),
            SemanticStringClassification.Classify(literal.RootElement, "source", "path"),
            SemanticStringClassification.Classify(literal.RootElement, "generation", "pattern"),
            SemanticStringClassification.Classify(literal.RootElement, "claims", "type"),
            SemanticStringClassification.Classify(literal.RootElement, "map", "targets"),
        ];
        _identityKinds =
        [
            SemanticStringClassification.Classify(reference.RootElement, "route", "source"),
            SemanticStringClassification.Classify(reference.RootElement, "reads", "key"),
            SemanticStringClassification.Classify(reference.RootElement, "children", "identifiedBy"),
            SemanticStringClassification.Classify(reference.RootElement, "source", "path"),
            SemanticStringClassification.Classify(reference.RootElement, "targets", "properties"),
            SemanticStringClassification.Classify(reference.RootElement, string.Empty, "sourceEvents"),
            SemanticStringClassification.Classify(reference.RootElement, string.Empty, "releasedBy"),
        ];
        using var text = JsonDocument.Parse($$"""{"pattern":"catrev1:{{new string('0', 64)}}","name":"rev1:{{new string('1', 64)}}","label":"sem1:{{new string('a', 64)}}"}""");
        _stripped = SemanticSiblingSegments.StrippedText(text.RootElement, "generation");
    }

    [Fact] void should_preserve_literal_fields() => Assert.True(_literalKinds.All(kind => kind == SemanticStringKind.Literal));
    [Fact] void should_classify_reference_fields_and_arrays() => Assert.True(_identityKinds.All(kind => kind == SemanticStringKind.Identity));
    [Fact] void should_preserve_identity_shaped_text_in_fallback_ranking() => Assert.Contains($"catrev1:{new string('0', 64)}", _stripped);
    [Fact] void should_preserve_names_in_fallback_ranking() => Assert.Contains($"rev1:{new string('1', 64)}", _stripped);
    [Fact] void should_preserve_labels_in_fallback_ranking() => Assert.Contains($"sem1:{new string('a', 64)}", _stripped);
}
