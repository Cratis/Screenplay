// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Extensions.for_SystemConfigurationValidator;

public class when_a_system_declares_abilities_beside_an_extension
{
    [Fact]
    void should_be_an_error_at_the_ability()
    {
        var diagnostics = new ScreenplayCompiler().Compile("system Payments\n  extension \"cratis/stripe\"\n  ability Charge\n    amount String\n").Diagnostics;
        diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(DiagnosticCodes.SystemAbilitiesBesideExtension);
    }

    [Fact]
    void should_accept_abilities_without_an_extension() =>
        new ScreenplayCompiler().Compile("system Payments\n  ability Charge\n    amount String\n").Diagnostics.Select(diagnostic => diagnostic.Code).ShouldNotContain(DiagnosticCodes.SystemAbilitiesBesideExtension);
}
