// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_a_child_explicitly_maps_fields_without_automap : given.a_consistent_model
{
    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source.Replace("children rows identified by rowId\n          from RowAdded", "children rows identified by rowId\n          no automap\n          from RowAdded\n            note = note\n            kind = kind", StringComparison.Ordinal));

    [Fact] void should_accept_explicit_mappings_identity_and_inherited_every() => _result.Success.ShouldBeTrue();
    [Fact] void should_report_no_consistency_error() => _result.Diagnostics.ShouldBeEmpty();
}
