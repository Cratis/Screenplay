// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_unsupported_match_patterns;

public class and_the_owner_is_a_command_or_a_concept : given.a_semantic_binder
{
    const string Command =
        """
        module Projects
          feature Registration
            slice StateChange RegisterProject
              command RegisterProject
                projectId Uuid identifier
                name String
                validate
                  name matches "(?=a)"
        """;

    const string Concept =
        """
        concept Code : String
          validate
            matches "["
        """;

    Diagnostic _command;
    Diagnostic _concept;

    void Because()
    {
        _command = Bind(Command).Diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.PatternOutsidePortableMatchDialect);
        _concept = Bind(Concept).Diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidMatchPattern);
    }

    [Fact] void should_name_the_command() => _command.Arguments["subject"].ShouldEqual(DiagnosticArgument.Declaration("command", ["Projects", "Registration", "RegisterProject", "RegisterProject"]));
    [Fact] void should_name_the_command_property() => _command.Arguments["property"].ShouldEqual(DiagnosticArgument.Text("name"));
    [Fact] void should_name_the_command_pattern() => _command.Arguments["pattern"].ShouldEqual(DiagnosticArgument.Text("(?=a)"));
    [Fact] void should_name_the_offset() => _command.Arguments["offset"].ShouldEqual(DiagnosticArgument.Index(0));
    [Fact] void should_name_the_concept() => _concept.Arguments["subject"].ShouldEqual(DiagnosticArgument.Declaration("concept", ["Code"]));
    [Fact] void should_leave_the_property_off_a_concept() => _concept.Arguments.ContainsKey("property").ShouldBeFalse();
}
