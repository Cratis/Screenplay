// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_checking_join_coverage_inside_elements : given.a_compiler
{
    const string Source =
        """
        type Row
          rowId Uuid
          joinedName String
        type Detail
          joinedName String
        module Records
          feature Entries
            slice StateView Browse
              event RowAdded
                rowId Uuid
              event DetailAdded
                joinedName String
              event NameChanged
                name String
              readmodel EntryView
                rows Row[]
                detail Detail?
              projection Entries => EntryView
                children rows identified by rowId
                  no automap
                  from RowAdded
                  join joinedName on rowId
                    with NameChanged
                      joinedName = name
                nested detail
                  no automap
                  from DetailAdded
                    joinedName = joinedName
                  join unrelatedLabel on joinedName
                    with NameChanged
                      joinedName = name
        """;

    CompilationResult<ApplicationSyntax> _result;
    CompilationResult<ApplicationSyntax> _labelOnly;

    void Because()
    {
        _result = _compiler.Compile(Source);
        _labelOnly = _compiler.Compile(Source.Replace("joinedName = name", "rowId = name", StringComparison.Ordinal));
    }

    [Fact] void should_cover_fields_by_join_mappings_not_labels() => _result.Success.ShouldBeTrue();
    [Fact] void should_not_count_a_join_label_as_a_mapped_field() => _labelOnly.Diagnostics.Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}").ShouldContainOnly("PLAY0284: Projection block 'rows' never populates field 'joinedName' of element type 'Row'");
}
