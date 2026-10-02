// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_a_command_reads_a_view_whose_query_is_keyed_on_its_header : given.a_compiler
{
    const string Source =
        """
        module M
          feature F
            slice StateView Rows
              readmodel Row
                rowId Uuid
                open  Bool
              query RowById => Row? by rowId Uuid
            slice StateChange CloseRow
              command CloseRow
                rowId Uuid identifier
                reads Row by rowId
                produces RowClosed
              event RowClosed
                note String
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    [Fact] void should_report_the_query_declaration() => _result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidQueryDeclaration);
    [Fact] void should_say_where_the_key_goes() => _result.Diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidQueryDeclaration).Message.ShouldContain("'by rowId Uuid'");
}
