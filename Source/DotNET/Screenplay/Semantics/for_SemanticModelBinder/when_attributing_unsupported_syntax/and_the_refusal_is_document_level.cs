// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_attributing_unsupported_syntax;

public class and_the_refusal_is_document_level : given.a_semantic_binder
{
    const string Source =
        """
        numbers exact
        module Projects
          feature Registration
            slice StateChange RegisterProject
              command RegisterProject
                projectId Uuid identifier
        """;

    Diagnostic _diagnostic;

    void Because() => _diagnostic = Bind(Source).Diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.UnsupportedSemanticSyntax);

    [Fact] void should_keep_the_issue_the_site_passed() => _diagnostic.Arguments["issue"].ShouldEqual(DiagnosticArgument.Text("Cratis/Screenplay#285"));
    [Fact] void should_carry_no_subject() => _diagnostic.Arguments.ContainsKey("subject").ShouldBeFalse();
}
