// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Semantics.for_SemanticSystems;

public class when_a_system_declares_no_ability : Specification
{
    Exception _error;

    void Because() => _error = Catch.Exception(() => property_order_binding.AdmitScreens("system Mailer\n  description \"Mail\"\n"));

    [Fact] void should_not_admit_the_system() => _error.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_say_what_makes_a_system_executable() => _error.Message.ShouldContain("declares an ability");
}
#endif
