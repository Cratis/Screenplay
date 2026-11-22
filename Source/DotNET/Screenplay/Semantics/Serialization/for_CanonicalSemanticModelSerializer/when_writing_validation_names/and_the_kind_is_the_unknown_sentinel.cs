// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.given;

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer.when_writing_validation_names;

public class and_the_kind_is_the_unknown_sentinel : a_valid_semantic_model
{
    Exception _exception;

    void Establish() => _application = _application with
    {
        Concepts = [_application.Concepts[0] with { Validations = [new(default, SemanticValidationRuleKind.Unknown, null, null)] }]
    };

    void Because() => _exception = Catch.Exception(() => SemanticModelCanonicalJson.SerializeWithoutRevision(LanguageVersion.V3, SemanticVersion.V3, _application));

    [Fact] void should_refuse_the_kind() => _exception.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_keep_the_unknown_kind_error() => _exception.Message.ShouldEqual("Unknown SemanticValidationRuleKind value 'Unknown'.");
}
