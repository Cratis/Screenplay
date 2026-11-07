// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_validating_guarded_slot_bindings;

public class and_an_inline_binding_inherits_a_subject : given.a_component_slot
{
    void Because() => CompileSlot(named: false, screenData: "data Item via query ItemDetails");

    [Fact] void should_compile() => _result.Success.ShouldBeTrue();
    [Fact] void should_resolve_the_inherited_subject() => _result.Diagnostics.ShouldBeEmpty();
}
