// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_a_navigation_screen_target : Specification
{
    ApplicationSyntax _original;
    CompilationResult<ApplicationSyntax> _roundTrip;
    string _printed;

    void Establish() => _original = new ScreenplayCompiler().Compile(for_ScreenplayCompiler.when_compiling_a_navigation_screen_target.Source).Value;
    void Because()
    {
        _printed = new ScreenplayPrinter().Print(_original);
        _roundTrip = new ScreenplayCompiler().Compile(_printed);
    }

    [Fact] void should_print_target_before_screen() => _printed.ShouldContain("target Shop.Projects screen CreateProject");
    [Fact] void should_compile_the_printed_document() => _roundTrip.Success.ShouldBeTrue();
    [Fact] void should_keep_the_same_ast() => Syntax.Serialization.SyntaxJson.StructurallyEqual(_original, _roundTrip.Value).ShouldBeTrue();
}
