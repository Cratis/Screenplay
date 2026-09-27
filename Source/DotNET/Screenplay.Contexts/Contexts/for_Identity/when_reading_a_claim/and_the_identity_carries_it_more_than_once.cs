// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Contexts.for_Identity.when_reading_a_claim;

public class and_the_identity_carries_it_more_than_once : given.an_identity
{
    string _value;
    IEnumerable<string> _values;

    void Because()
    {
        _value = _identity.ClaimValue("scope")!;
        _values = _identity.ClaimValues("scope");
    }

    [Fact] void should_give_the_first_value() => _value.ShouldEqual("invoices.read");
    [Fact] void should_give_every_value() => _values.ShouldContainOnly("invoices.read", "invoices.write");
}
