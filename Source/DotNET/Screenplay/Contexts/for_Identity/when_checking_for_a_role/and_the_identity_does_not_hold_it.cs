// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Contexts.for_Identity.when_checking_for_a_role;

public class and_the_identity_does_not_hold_it : given.an_identity
{
    bool _result;

    void Because() => _result = _identity.HasRole("Auditor");

    [Fact] void should_not_hold_the_role() => _result.ShouldBeFalse();
}
