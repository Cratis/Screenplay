// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_validating_guarded_outlet_bindings;

public class and_a_named_binding_inherits_a_subject : given.a_component_outlet
{
    void Because() => CompileOutlet(named: true, screenData: "data Item via query ItemDetails");

    [Fact] void should_compile() => _result.Success.ShouldBeTrue();
    [Fact] void should_resolve_the_inherited_subject() => _result.Diagnostics.ShouldBeEmpty();
}
