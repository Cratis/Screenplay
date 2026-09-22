// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_consistent_model : given.a_consistent_model
{
    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    [Fact] void should_accept_declared_validation_targets_including_implemented_rules() => _result.Success.ShouldBeTrue();
    [Fact] void should_accept_differently_named_read_keys_of_the_same_nominal_type() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_count_identity_automap_and_inherited_mappings_for_child_fields() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_accept_specification_values_produced_by_declared_property_copies() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_resolve_same_named_enum_fields_from_their_own_declarations() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_accept_declared_event_assignment_targets() => _result.Diagnostics.ShouldBeEmpty();
}
