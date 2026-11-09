// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_compiling_a_dynamic_dictionary_key;

public class and_the_event_content_is_not_on_the_event : for_ScreenplayCompiler.given.a_compiler
{
    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile("""
        module Models
          feature Slots
            slice StateView Slots
              event SlotAssigned
                slot String
                seriesId Guid
              readmodel ModelView
                slots Guid
              projection Models => ModelView
                from SlotAssigned
                  slots.$eventContent.position = seriesId
                  slots.$eventContent.slot = seriesId
        """);

    [Fact] void should_compile_with_a_warning() => _result.Success.ShouldBeTrue();
    [Fact] void should_report_the_unknown_key_once() => _result.Diagnostics.Count(diagnostic => diagnostic.Code == DiagnosticCodes.UnknownEventContentKey).ShouldEqual(1);
    [Fact] void should_locate_the_mapping() => _result.Diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.UnknownEventContentKey).Location.Line.ShouldEqual(11);
    [Fact] void should_not_report_the_base_property() => _result.Diagnostics.ShouldNotContain(diagnostic => diagnostic.Code == DiagnosticCodes.UnknownReadModelProperty);
}
