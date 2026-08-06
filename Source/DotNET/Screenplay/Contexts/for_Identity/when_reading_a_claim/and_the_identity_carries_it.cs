// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Contexts.for_Identity.when_reading_a_claim;

public class and_the_identity_carries_it : given.an_identity
{
    bool _carriesIt;
    string _value;

    void Because()
    {
        _carriesIt = _identity.HasClaim("department");
        _value = _identity.ClaimValue("department")!;
    }

    [Fact] void should_carry_the_claim() => _carriesIt.ShouldBeTrue();
    [Fact] void should_give_the_value_of_the_claim() => _value.ShouldEqual("Finance");
}
