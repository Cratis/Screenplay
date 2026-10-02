// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Files;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_a_file_placed_in_a_feature_holds_a_module_construct : given.a_compiler
{
    const string Source =
        """
        screen template Misplaced
          content
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Parse(Source, "Orders.play", new PlayPlacement(["Ordering", "Orders"]));

    [Fact] void should_report_it() => _result.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.UnexpectedInPlacedFile);
    [Fact] void should_name_the_placement() => _result.Diagnostics.Single().Message.ShouldContain("feature 'Ordering.Orders'");
}
