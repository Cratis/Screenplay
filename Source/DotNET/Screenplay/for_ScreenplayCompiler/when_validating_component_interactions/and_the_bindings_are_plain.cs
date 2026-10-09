// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_validating_component_interactions;

public class and_the_bindings_are_plain : given.a_compiler
{
    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile("""
        behavior Plain
          parameter message
          on click
            notify info message
        module Work
          feature Items
            slice StateView Details
              screen Details
                component App.Card card
                  on event MissingEvent
                    execute MissingCommand
                  on MissingTrigger
                    refresh MissingQuery
                  uses Plain
                    unexpected "Hello"
                  uses MissingBehavior
        """);

    [Fact] void should_preserve_existing_plain_component_validation() => _result.Success.ShouldBeTrue();
    [Fact] void should_not_introduce_reference_or_argument_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
}
