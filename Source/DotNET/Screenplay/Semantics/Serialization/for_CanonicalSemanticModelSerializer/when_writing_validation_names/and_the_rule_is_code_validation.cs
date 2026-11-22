// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.given;

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer.when_writing_validation_names;

public class and_the_rule_is_code_validation : a_valid_semantic_model
{
    ExecutableSemanticModel _source;
    byte[] _serialized;

    void Establish()
    {
        var concept = _application.Concepts[0] with
        {
            Validations = [new(default, SemanticValidationRuleKind.CodeValidation, null, null) { Name = "Valid", RequirementId = "code" }]
        };
        _source = ExecutableSemanticModel.Create(LanguageVersion.V3, SemanticVersion.V3, _application with { Concepts = [_application.Concepts[1], concept] });
    }

    void Because() => _serialized = SemanticModelSerializer.Serialize(_source);

    [Fact] void should_write_the_exact_validation_bytes() => _serialized.AsSpan().IndexOf("""{"property":null,"kind":"codeValidation","operand":null,"message":null,"name":"Valid","requirementId":"code"}"""u8).ShouldNotEqual(-1);
}
