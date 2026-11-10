// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_validating_mixed_guarded_directives : given.a_compiler
{
    const string Source =
        """
        type Row
          state String
        behavior Pick
          on click
            when item.missing == "open"
              execute Retry
                with nothing from item.status
        module Work
          feature Items
            slice StateView Details
              readmodel Item
                status String
                rows Row[]
              query ItemDetails => Item
              command Retry
                itemId Uuid
              screen Details
                data Item via query ItemDetails
                action "Retry"
                  when item.absent == "open" execute Retry
                    with extra from item.status
                  when item.unknown == "failed" execute Retry
                    with other from item.none
                  otherwise execute Retry
                    with surplus from item.status
                table rows
                  on select
                    when item.gone == "x"
                      notify info "x"
                  uses Pick
                component App.Card card
                  on click
                    when item.vanished == "y"
                      notify info "y"
                  uses Pick
                  slot content
                    section inner
                      action "Again"
                        when item.lost == "z" execute Retry
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    [Fact]
    void should_report_findings_in_directive_order() =>
        _result.Diagnostics.Select(diagnostic => $"{diagnostic.Code}@{diagnostic.Location.Line}:{diagnostic.Location.Column}")
            .ShouldEqual(Expected);

    static readonly string[] Expected =
    [
        "PLAY0345@20:11", "PLAY0348@21:13", "PLAY0345@22:11", "PLAY0348@23:13", "PLAY0345@23:13", "PLAY0348@25:13",
        "PLAY0345@28:13", "PLAY0345@5:5", "PLAY0348@7:9", "PLAY0345@7:9", "PLAY0345@33:13", "PLAY0345@39:17"
    ];
}
