// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_compiling_a_constraint;

public class with_a_composite_key_naming_an_unknown_property : given.a_compiler
{
    const string Source =
        """
        module Authors
          feature Registration
            slice StateChange RegisterAuthor
              event AuthorRegistered
                name String
                country String
              event AuthorImported
                name String
              constraint UniqueAuthor
                unique name, contry on AuthorRegistered
                unique nmae on AuthorImported
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    [Fact] void should_fail() => _result.Success.ShouldBeFalse();
    [Fact] void should_report_each_unknown_property() => Unknown.Length.ShouldEqual(2);
    [Fact] void should_report_the_misspelled_composite_member_and_not_the_declared_one() => Unknown[0].Message.ShouldEqual("Constraint 'UniqueAuthor' names property 'contry', which event 'AuthorRegistered' does not declare.");
    [Fact] void should_point_to_the_composite_rule() => Unknown[0].Location.Line.ShouldEqual(10);
    [Fact] void should_check_the_alternative_rule_on_the_other_event() => Unknown[1].Message.ShouldEqual("Constraint 'UniqueAuthor' names property 'nmae', which event 'AuthorImported' does not declare.");
    [Fact] void should_point_to_the_alternative_rule() => Unknown[1].Location.Line.ShouldEqual(11);

    Diagnostic[] Unknown => [.. _result.Diagnostics.Where(_ => _.Code == DiagnosticCodes.UnknownConstraintProperty)];
}
