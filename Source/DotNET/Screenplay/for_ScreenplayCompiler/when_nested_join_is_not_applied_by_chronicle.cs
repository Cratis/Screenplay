// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_nested_join_is_not_applied_by_chronicle : given.a_compiler
{
    const string Source =
        """
        type Detail
          explicitName String
          autoName String
        module Records
          feature Entries
            slice StateView Browse
              event DetailAdded
                other String
              event NameChanged
                name String
                autoName String
              readmodel EntryView
                detail Detail?
              projection Entries => EntryView
                nested detail
                  from DetailAdded
                  join User on other
                    with NameChanged
                      explicitName = name
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    [Fact] void should_report_both_fields_as_unpopulated() => _result.Diagnostics.Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}").ShouldContainOnly(
        "PLAY0284: Projection block 'detail' never populates field 'explicitName' of element type 'Detail' (Chronicle does not currently apply joins inside 'nested' blocks; Cratis/Chronicle#4125)",
        "PLAY0284: Projection block 'detail' never populates field 'autoName' of element type 'Detail' (Chronicle does not currently apply joins inside 'nested' blocks; Cratis/Chronicle#4125)");
    [Fact] void should_fail_compilation() => _result.Success.ShouldBeFalse();
}
