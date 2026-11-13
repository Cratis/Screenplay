// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Screenplay.Contexts;

namespace Cratis.Screenplay.for_ProvideContext;

public class when_reading_the_values_resolved_so_far : Specification
{
    ProvideContext _context;

    void Establish()
    {
        dynamic values = new ExpandoObject();
        values.Region = "Nordics";
        _context = new ProvideContext(new Arguments("v-1"), TenantId.Default, Identity.NotSet, DateTimeOffset.UtcNow) { Values = values };
    }

    [Fact] void should_carry_the_arguments() => _context.ArgumentsAs<Arguments>().VesselId.ShouldEqual("v-1");
    [Fact] void should_carry_the_resolved_values() => ((string)_context.Values.Region).ShouldEqual("Nordics");
    [Fact] void should_start_with_no_values() => ((IDictionary<string, object?>)new ProvideContext(new Arguments("v-1"), TenantId.Default, Identity.NotSet, DateTimeOffset.UtcNow).Values).ShouldBeEmpty();

    record Arguments(string VesselId);
}
