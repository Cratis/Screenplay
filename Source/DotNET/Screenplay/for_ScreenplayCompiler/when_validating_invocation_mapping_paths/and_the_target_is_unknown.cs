// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_validating_invocation_mapping_paths;

public class and_the_target_is_unknown : given.a_model
{
    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Model("note", "missing"));

    [Fact] void should_reject_the_unknown_command_field() => _result.Success.ShouldBeFalse();
    [Fact] void should_identify_the_target() => _result.Diagnostics.Single().Message.ShouldEqual("Command 'Record' declares no field 'missing'.");
    [Fact] void should_use_the_mapping_diagnostic() => _result.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.InvalidInvocationMapping);
}
