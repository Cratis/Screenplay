// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_a_child_disables_automap : given.a_consistent_model
{
    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source.Replace("children rows identified by rowId", "children rows identified by rowId\n          no automap", StringComparison.Ordinal).Replace("every\n", "every\n          no automap\n", StringComparison.Ordinal));

    [Fact] void should_require_the_non_identity_fields_to_be_mapped() => _result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContainOnly(DiagnosticCodes.UnpopulatedProjectionField, DiagnosticCodes.UnpopulatedProjectionField);
    [Fact] void should_fail_compilation() => _result.Success.ShouldBeFalse();
}
