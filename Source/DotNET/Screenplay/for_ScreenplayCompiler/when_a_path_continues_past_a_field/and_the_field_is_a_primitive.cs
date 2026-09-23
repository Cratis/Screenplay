// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_a_path_continues_past_a_field;

public class and_the_field_is_a_primitive : given.a_model_with_fields
{
    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Model("note.missing"));

    [Fact] void should_report_the_unknown_event_field() => _result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContainOnly(DiagnosticCodes.UnknownEventField);
    [Fact] void should_name_the_whole_path() => _result.Diagnostics.Single().Message.ShouldEqual("Event 'EntryRecorded' declares no field 'note.missing'");
}
