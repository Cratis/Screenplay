// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Files;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_a_placed_file_declares_another_module : given.a_compiler
{
    const string Source =
        """
        module Ordering
          description "Restating the placement is fine"
        module Billing
          description "Another module is not"
        layout Shell
          content
        screen template Misplaced
          content
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Parse(Source, "Ordering.play", new PlayPlacement(["Ordering"]));

    [Fact] void should_join_the_restated_module_to_the_placement() => _result.Value!.Modules.Single().Description.ShouldEqual("Restating the placement is fine");
    [Fact] void should_report_the_other_module() => _result.Diagnostics.Count(diagnostic => diagnostic.Code == DiagnosticCodes.ModuleInPlacedFile).ShouldEqual(1);
    [Fact] void should_keep_application_declarations() => _result.Value!.Layouts!.Single().Name.ShouldEqual("Shell");
    [Fact] void should_take_a_module_body_construct() => _result.Value!.Modules.Single().ScreenTemplates.Single().Name.ShouldEqual("Misplaced");
}
