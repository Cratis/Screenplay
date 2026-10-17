// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_attributing_unsupported_syntax;

public class and_the_refusal_is_inside_a_command : given.a_semantic_binder
{
    const string Source =
        """
        module Projects
          feature Registration
            slice StateChange RegisterProject
              command RegisterProject
                projectId Uuid identifier
                produces ProjectRegistered
                  for projectId
                  audit = $identity.roles
              event ProjectRegistered
                audit String
        """;

    Diagnostic _diagnostic;

    void Because() => _diagnostic = Bind(Source).Diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.UnsupportedSemanticSyntax);

    [Fact] void should_attach_the_owning_command_as_the_subject() => _diagnostic.Arguments["subject"].ShouldEqual(DiagnosticArgument.Declaration("command", ["Projects", "Registration", "RegisterProject", "RegisterProject"]));
    [Fact] void should_not_invent_an_issue() => _diagnostic.Arguments.ContainsKey("issue").ShouldBeFalse();
}
