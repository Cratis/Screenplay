// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_description_with_an_empty_fence : given.a_compiler
{
    const string Source =
        """
        module Invoicing
          description
            ```
            ```
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    [Fact] void should_not_succeed() => _result.Success.ShouldBeFalse();
    [Fact] void should_report_the_empty_description() => _result.Diagnostics.Single().Message.ShouldEqual("Module 'Invoicing' declares an empty description - the fenced block must contain text");
    [Fact] void should_have_no_description() => _result.Value!.Modules.Single().Description.ShouldBeNull();
}
