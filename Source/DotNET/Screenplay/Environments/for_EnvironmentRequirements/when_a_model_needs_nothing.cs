// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Environments.for_EnvironmentRequirements;

public class when_a_model_needs_nothing : Specification
{
    EnvironmentRequirements _requirements;

    void Because() => _requirements = EnvironmentRequirements.For(new ScreenplayCompiler().Compile("persona Person\n").Value!);

    [Fact] void should_list_no_keys() => _requirements.Keys.ShouldBeEmpty();
    [Fact] void should_list_no_secrets() => _requirements.Secrets.ShouldBeEmpty();
}
