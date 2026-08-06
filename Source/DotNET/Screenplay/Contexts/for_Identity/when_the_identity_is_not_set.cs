// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Contexts.for_Identity;

public class when_the_identity_is_not_set : Specification
{
    Identity _identity;

    void Because() => _identity = Identity.NotSet;

    [Fact] void should_not_be_authenticated() => _identity.IsAuthenticated.ShouldBeFalse();
    [Fact] void should_hold_no_roles() => _identity.HasRole("Accountant").ShouldBeFalse();
    [Fact] void should_carry_no_claims() => _identity.HasClaim("department").ShouldBeFalse();
    [Fact] void should_give_no_claim_value() => _identity.ClaimValue("department").ShouldBeNull();
}
