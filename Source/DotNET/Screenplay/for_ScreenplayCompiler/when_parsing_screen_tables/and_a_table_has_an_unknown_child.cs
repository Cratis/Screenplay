// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_parsing_screen_tables;

public class and_a_table_has_an_unknown_child : given.a_compiler
{
    CompilationResult<ApplicationSyntax> _result;
    ScreenTableSyntax _table;
    Diagnostic[] _unknown;

    void Because()
    {
        _result = _compiler.Compile(
            """
            module Work
              feature Items
                slice StateView Details
                  screen Details
                    table Items
                      sparkle
                      column name
            """);
        _table = _result.Value!.Modules.Single().Features.Single().Slices.Single().Screens.Single().Directives.OfType<ScreenTableSyntax>().Single();
        _unknown = [.. _result.Diagnostics.Where(diagnostic => diagnostic.Code == DiagnosticCodes.UnknownTableDirective)];
    }

    [Fact] void should_report_one_unknown_table_directive() => _unknown.Length.ShouldEqual(1);
    [Fact] void should_report_the_unexpected_child() => _unknown[0].Message.ShouldEqual("Unexpected 'sparkle' in table - expected 'column ...', 'on row-click navigate to ...', 'on <trigger>' or 'uses <Behavior>'");
    [Fact] void should_still_read_the_following_column() => _table.Columns.Single().Property.ShouldEqual("name");
}
