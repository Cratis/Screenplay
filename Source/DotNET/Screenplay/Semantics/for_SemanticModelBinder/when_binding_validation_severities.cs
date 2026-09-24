// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_validation_severities : given.a_semantic_binder
{
    const string Source =
        """
        concept Label : String
          validate
            not empty severity information message "Required"
        module Sales
          feature Orders
            slice StateChange Submit
              command Submit
                label Label
                validate
                  label not empty severity warning message "Missing"
                  label max 10
                  require label == "okay"
                    severity information
                    message "Not okay"
        """;

    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Bind(Source);

    [Fact] void should_bind() => _result.Success.ShouldBeTrue();
    [Fact] void should_bind_the_concept_level() => _result.Value!.Model.Application.Concepts.Single().Validations.Single().Severity.ShouldEqual(SemanticValidationSeverity.Information);
    [Fact] void should_bind_the_rule_level() => Command.Validations[0].Severity.ShouldEqual(SemanticValidationSeverity.Warning);
    [Fact] void should_default_to_error() => Command.Validations[1].Severity.ShouldEqual(SemanticValidationSeverity.Error);
    [Fact] void should_bind_the_requirement_level() => Command.Requirements.Single().Severity.ShouldEqual(SemanticValidationSeverity.Information);

    SemanticCommand Command => _result.Value!.Model.Application.Modules.Single().Features.Single().Slices.Single().Commands.Single();
}
