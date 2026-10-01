// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_compiling_a_constraint;

public class with_an_unknown_unique_property : given.a_compiler
{
    const string Source =
        """
        module Authors
          feature Registration
            slice StateChange RegisterAuthor
              event AuthorRegistered
                name String
              constraint UniqueAuthorName
                unique nme on AuthorRegistered
                message "An author with this name is already registered."
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    [Fact] void should_fail() => _result.Success.ShouldBeFalse();
    [Fact] void should_report_the_unknown_constraint_property_once() => _result.Diagnostics.Count(_ => _.Code == DiagnosticCodes.UnknownConstraintProperty).ShouldEqual(1);
    [Fact] void should_report_an_error() => Unknown.Severity.ShouldEqual(DiagnosticSeverity.Error);
    [Fact] void should_point_to_the_unique_rule() => Unknown.Location.Line.ShouldEqual(7);
    [Fact] void should_name_the_property_and_its_event() => Unknown.Message.ShouldEqual("Constraint 'UniqueAuthorName' names property 'nme', which event 'AuthorRegistered' does not declare.");

    Diagnostic Unknown => _result.Diagnostics.Single(_ => _.Code == DiagnosticCodes.UnknownConstraintProperty);
}
