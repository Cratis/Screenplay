// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing.for_ProvideParser;

public class when_a_context_path_names_nothing_provided : Specification
{
    const string Source =
        """
        module Fleet
          provide label String from $context.region
          feature Quality
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = new ScreenplayCompiler().Compile(Source);

    [Fact] void should_warn_about_the_unknown_context_path() => _result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(DiagnosticCodes.UnknownContextPath);
}
