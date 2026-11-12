// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing.for_ProvideParser;

public class when_an_override_keeps_the_type : Specification
{
    const string Source =
        """
        module Fleet
          provide region String from "a"
          feature Inner
            provide region String from "b"
          feature Quality
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = new ScreenplayCompiler().Compile(Source);

    [Fact] void should_compile_without_findings() => _result.Diagnostics.ShouldBeEmpty();
}
