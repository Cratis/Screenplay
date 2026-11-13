// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Screenplay.Contexts;

namespace Cratis.Screenplay.for_ProvideContext;

public class when_reading_provided_values_from_the_policy_context : Specification
{
    PolicyContext _context;
    PolicyContext _withValues;

    void Establish()
    {
        _context = new(new Invoice(), "subject", Identity.NotSet, TenantId.Default, DateTimeOffset.UtcNow);
        dynamic values = new ExpandoObject();
        values.Vessel = new Vessel("Polaris");
        _withValues = _context with { Values = values };
    }

    [Fact] void should_have_no_values_by_default() => ((IDictionary<string, object?>)_context.Values).ShouldBeEmpty();
    [Fact] void should_read_a_value_by_name() => ((string)_withValues.Values.Vessel.Name).ShouldEqual("Polaris");
    [Fact] void should_read_the_values_statically_typed() => _withValues.ValuesAs<IDictionary<string, object?>>().Keys.ShouldContainOnly("Vessel");

    record Invoice;
    record Vessel(string Name);
}
