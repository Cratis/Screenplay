// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_profile_with_a_quoted_theme_the_document_declares : given.a_compiler
{
    const string Source =
        """
        theme Aurora
          compatible with core

        ui profile Desktop
          packages
            PrimeReact

          theme "Aurora"
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    [Fact] void should_resolve_the_declared_theme() => _result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldNotContain(DiagnosticCodes.UnknownTheme);
    [Fact] void should_apply_the_compatibility_check() => _result.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.ThemeNotCompatibleWithPackage);
}
