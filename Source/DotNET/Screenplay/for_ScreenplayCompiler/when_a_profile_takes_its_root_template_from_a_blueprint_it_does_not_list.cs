// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_a_profile_takes_its_root_template_from_a_blueprint_it_does_not_list : given.a_compiler
{
    const string Source =
        """
        ui profile Desktop
          packages
            Cratis.Components

          root template blueprint Cratis.Blueprint.Default AppShell

          new screens use template blueprint crm Workspace
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    [Fact] void should_report_each_unlisted_blueprint() => _result.Diagnostics.Where(diagnostic => diagnostic.Code == DiagnosticCodes.UnknownRootBlueprint).Select(diagnostic => diagnostic.Location.Line).ShouldContainOnly(5, 7);
    [Fact] void should_name_the_blueprint() => _result.Diagnostics.Where(diagnostic => diagnostic.Code == DiagnosticCodes.UnknownRootBlueprint).First().Message.ShouldContain("'Cratis.Blueprint.Default'");
    [Fact] void should_not_ask_for_an_own_template() => _result.Diagnostics.Where(diagnostic => diagnostic.Code == DiagnosticCodes.UnknownRootTemplate).ShouldBeEmpty();
}
