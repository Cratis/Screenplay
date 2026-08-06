// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Contexts.for_Identity.when_reading_a_claim;

public class and_the_identity_does_not_carry_it : given.an_identity
{
    bool _carriesIt;
    string? _value;
    IEnumerable<string> _values;

    void Because()
    {
        _carriesIt = _identity.HasClaim("dateOfBirth");
        _value = _identity.ClaimValue("dateOfBirth");
        _values = _identity.ClaimValues("dateOfBirth");
    }

    [Fact] void should_not_carry_the_claim() => _carriesIt.ShouldBeFalse();
    [Fact] void should_not_give_a_value() => _value.ShouldBeNull();
    [Fact] void should_give_no_values() => _values.ShouldBeEmpty();
}
