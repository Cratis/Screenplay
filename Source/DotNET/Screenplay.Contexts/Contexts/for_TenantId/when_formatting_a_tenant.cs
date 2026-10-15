// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Contexts.for_TenantId;

public class when_formatting_a_tenant : Specification
{
    TenantId _tenant;
    GenerationContext _context;
    string _formatted;
    string _interpolated;
    string _generated;

    void Establish()
    {
        _tenant = new("Customer");
        _context = new(DateTimeOffset.UnixEpoch, _tenant);
    }

    void Because()
    {
        _formatted = _tenant.ToString();
        _interpolated = $"{_tenant}:x";
        _generated = $"{_context.Tenant}:x";
    }

    [Fact] void should_format_as_the_value() => _formatted.ShouldEqual("Customer");
    [Fact] void should_interpolate_the_value() => _interpolated.ShouldEqual("Customer:x");
    [Fact] void should_interpolate_the_tenant_value_from_the_generation_context() => _generated.ShouldEqual("Customer:x");
}
