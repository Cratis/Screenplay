// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_validating_invocation_mapping_paths;

public class and_a_declared_trigger_path_is_invalid : given.a_model
{
    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Model("figures.note", trigger: "External"));

    [Fact] void should_reject_collection_traversal_for_application_triggers_too() => _result.Success.ShouldBeFalse();
    [Fact] void should_use_the_mapping_diagnostic() => _result.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.InvalidInvocationMapping);
}
