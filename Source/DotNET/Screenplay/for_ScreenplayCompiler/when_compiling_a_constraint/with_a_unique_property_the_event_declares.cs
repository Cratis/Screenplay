// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_compiling_a_constraint;

public class with_a_unique_property_the_event_declares : given.a_compiler
{
    const string Source =
        """
        module Authors
          feature Registration
            slice StateChange RegisterAuthor
              event AuthorRegistered
                name String
                country String
              constraint UniqueAuthor
                unique name, country on AuthorRegistered
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    [Fact] void should_compile() => _result.Success.ShouldBeTrue();
    [Fact] void should_report_nothing() => _result.Diagnostics.ShouldBeEmpty();
}
