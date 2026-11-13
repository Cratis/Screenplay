// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing.for_ProvideParser;

public class when_a_provided_value_is_read : Specification
{
    const string Source =
        """
        module Fleet
          provide region String from "a"
          provide label String from $context.region
          feature Quality
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = new ScreenplayCompiler().Compile(Source);

    [Fact] void should_compile_without_findings() => _result.Diagnostics.ShouldBeEmpty();
}
