// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_validating_invocation_mapping_paths;

public class and_the_source_field_is_unknown : given.a_model
{
    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Model("detail.missing"));

    [Fact] void should_fail_ordinary_compilation() => _result.Success.ShouldBeFalse();
    [Fact] void should_report_the_mapping_line() => _result.Diagnostics.Single().Location.Line.ShouldEqual(22);
    [Fact] void should_identify_the_unknown_path() => _result.Diagnostics.Single().Message.ShouldEqual("Trigger declares no field 'detail.missing'.");
    [Fact] void should_use_the_mapping_diagnostic() => _result.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.InvalidInvocationMapping);
}
