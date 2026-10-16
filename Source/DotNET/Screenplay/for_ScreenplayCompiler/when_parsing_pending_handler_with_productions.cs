// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_parsing_pending_handler_with_productions : given.a_compiler
{
    CompilationResult<ApplicationSyntax> _result;
    CompilationResult<ApplicationSyntax> _roundtrip;

    void Because()
    {
        _result = _compiler.Parse("""
            module M
              feature F
                slice StateChange S
                  command C
                    handler
                      implementation
                        hint "first"
                        hint "second"
                    produces Recorded
                  event Recorded
            """);
        _roundtrip = _compiler.Parse(new ScreenplayPrinter().Print(_result.Value!));
    }

    [Fact] void should_parse_without_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_reparse_canonical_source() => _roundtrip.Success.ShouldBeTrue();
    [Fact] void should_preserve_the_syntax() => SyntaxJson.StructurallyEqual(_result.Value!, _roundtrip.Value!).ShouldBeTrue();
    [Fact] void should_preserve_hint_order() => _roundtrip.Value!.Modules.Single().Features.Single().Slices.Single().Commands.Single().Handler!.Implementation!.Hints.Select(hint => hint.Text).ShouldEqual(["first", "second"]);
}
