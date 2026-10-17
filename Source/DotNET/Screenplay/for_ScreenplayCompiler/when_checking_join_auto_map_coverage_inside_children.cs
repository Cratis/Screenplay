// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

// A joined event's own auto-map decides what it populates, as in Chronicle (#613).
public class when_checking_join_auto_map_coverage_inside_children : given.a_compiler
{
    const string Source =
        """
        type Row
          rowId Uuid
          name String
        module Records
          feature Entries
            slice StateView Browse
              event RowAdded
                rowId Uuid
              event RowNamed
                name String
              readmodel EntryView
                rows Row[]
              projection Entries => EntryView
                children rows identified by rowId
                  LEVEL
                  from RowAdded
                  join named on rowId
                    with RowNamed
                      JOINED
        """;

    CompilationResult<ApplicationSyntax> _joinedWithoutAutoMap;
    CompilationResult<ApplicationSyntax> _joinedWithAutoMap;

    void Because()
    {
        _joinedWithoutAutoMap = _compiler.Compile(Source.Replace("LEVEL", "automap", StringComparison.Ordinal).Replace("JOINED", "no automap", StringComparison.Ordinal));
        _joinedWithAutoMap = _compiler.Compile(Source.Replace("LEVEL", "no automap", StringComparison.Ordinal).Replace("JOINED", "automap", StringComparison.Ordinal));
    }

    [Fact] void should_report_the_field_a_joined_event_without_auto_map_leaves_unpopulated() =>
        _joinedWithoutAutoMap.Diagnostics.Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}").ShouldContainOnly("PLAY0284: Projection block 'rows' never populates field 'name' of element type 'Row'");
    [Fact] void should_count_the_field_a_joined_event_with_auto_map_populates() =>
        _joinedWithAutoMap.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldNotContain("PLAY0284");
}
