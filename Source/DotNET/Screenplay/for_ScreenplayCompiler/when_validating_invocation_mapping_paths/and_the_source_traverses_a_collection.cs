// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_validating_invocation_mapping_paths;

public class and_the_source_traverses_a_collection : given.a_model
{
    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Model("figures.note"));

    [Fact] void should_fail_without_requesting_an_executable_model() => _result.Success.ShouldBeFalse();
    [Fact] void should_not_infer_collection_fan_out() => _result.Diagnostics.Single().Message.ShouldEqual("Trigger path 'figures.note' traverses collection 'figures'; invocation mappings do not fan out collections.");
    [Fact] void should_use_the_mapping_diagnostic() => _result.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.InvalidInvocationMapping);
}
