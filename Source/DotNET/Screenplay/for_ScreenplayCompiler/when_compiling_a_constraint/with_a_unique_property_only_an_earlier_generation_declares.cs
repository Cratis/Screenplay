// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_compiling_a_constraint;

// The binder names the revision the property was removed in, so the syntax check leaves it alone.
public class with_a_unique_property_only_an_earlier_generation_declares : given.a_compiler
{
    const string Source =
        """
        module Authors
          feature Registration
            slice StateChange RegisterAuthor
              event AuthorRegistered generation 1
                name String
              event AuthorRegistered generation 2
                fullName String
              constraint UniqueAuthorName
                unique name on AuthorRegistered
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    [Fact] void should_not_report_an_unknown_constraint_property() => _result.Diagnostics.ShouldNotContain(_ => _.Code == DiagnosticCodes.UnknownConstraintProperty);
}
