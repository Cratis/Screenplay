// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_a_localized_message : given.a_semantic_binder
{
    const string Prefix =
        """
        module Projects
          feature Registration
            slice StateChange RegisterProject
              command RegisterProject
                name String
                validate
                  name not empty message
        """;

    CompilationResult<SemanticCompilation> _valid;
    CompilationResult<SemanticCompilation> _invalid;

    void Because()
    {
        _valid = Bind(Prefix + " $strings.projects.name_required");
        _invalid = Bind(Prefix + " $strings.projects..required");
    }

    [Fact] void should_bind_a_dotted_key() => _valid.Success.ShouldBeTrue();
    [Fact] void should_reject_an_empty_key_segment() => _invalid.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidSemanticStringKey).ShouldBeTrue();
    [Fact] void should_report_the_message_line() => _invalid.Diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidSemanticStringKey).Location.Line.ShouldEqual(7);
}
