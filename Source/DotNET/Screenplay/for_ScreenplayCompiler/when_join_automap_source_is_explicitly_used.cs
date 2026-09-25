// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_join_automap_source_is_explicitly_used : given.a_compiler
{
    const string Source =
        """
        type Row
          rowId Uuid
          name String
          alias String
        module Records
          feature Entries
            slice StateView Browse
              event RowAdded
                rowId Uuid
              event NameChanged
                name String
              readmodel EntryView
                rows Row[]
              projection Entries => EntryView
                children rows identified by rowId
                  from RowAdded
                  join User on rowId
                    with NameChanged
                      alias = name
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    [Fact] void should_report_the_unpopulated_name_field() => _result.Diagnostics.Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}").ShouldContainOnly("PLAY0284: Projection block 'rows' never populates field 'name' of element type 'Row'");
    [Fact] void should_fail_compilation() => _result.Success.ShouldBeFalse();
}
