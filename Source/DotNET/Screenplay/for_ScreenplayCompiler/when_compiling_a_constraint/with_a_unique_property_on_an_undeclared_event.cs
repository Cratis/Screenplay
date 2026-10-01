// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_compiling_a_constraint;

// The unknown event is what is wrong here; with no event there are no properties to compare with.
public class with_a_unique_property_on_an_undeclared_event : given.a_compiler
{
    const string Source =
        """
        module Authors
          feature Registration
            slice StateChange RegisterAuthor
              constraint UniqueAuthorName
                unique name on AuthorRegistered
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    [Fact] void should_warn_about_the_unknown_event() => _result.Diagnostics.ShouldContain(_ => _.Code == DiagnosticCodes.UnknownEvent);
    [Fact] void should_not_report_an_unknown_constraint_property() => _result.Diagnostics.ShouldNotContain(_ => _.Code == DiagnosticCodes.UnknownConstraintProperty);
}
