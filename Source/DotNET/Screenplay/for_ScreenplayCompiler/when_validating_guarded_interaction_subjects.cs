// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_validating_guarded_interaction_subjects : given.a_compiler
{
    [Fact]
    void should_validate_named_behaviors_at_each_attachment()
    {
        var source = """
            behavior Pick
              on click
                when item.status == "open"
                  notify info "Open"
            module Work
              uses Pick
              feature Items
                slice StateView Details
                  readmodel Item
                    status String
                  query ItemDetails => Item
                  screen Details
                    data Item via query ItemDetails
                    uses Pick
            """;
        var findings = _compiler.Compile(source).Diagnostics.Where(diagnostic => diagnostic.Code == DiagnosticCodes.UnresolvedActionSubject).ToArray();
        findings.Length.ShouldEqual(1);
        findings[0].Location.Line.ShouldEqual(6);
    }

    [Fact]
    void should_not_infer_a_subject_from_component_context()
    {
        var source = """
            module Work
              feature Items
                slice StateView Details
                  readmodel Item
                    status String
                  query ItemDetails => Item
                  screen Details
                    component App.Table entries
                      context from query ItemDetails
                      on select
                        when item.status == "open"
                          notify info "Open"
                        otherwise
                          notify info "Closed"
            """;
        _compiler.Compile(source).Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.UnresolvedActionSubject).ShouldBeTrue();
    }

    [Fact]
    void should_resolve_child_collection_row_fields()
    {
        var source = """
            type Row
              status String
            module Work
              feature Items
                slice StateView Details
                  readmodel Item
                    rows Row[]
                  query ItemDetails => Item
                  screen Details
                    data Item via query ItemDetails
                    table rows
                      on select
                        when item.status == "open"
                          notify info "Open"
            """;
        _compiler.Compile(source).Diagnostics.ShouldBeEmpty();
    }
}
