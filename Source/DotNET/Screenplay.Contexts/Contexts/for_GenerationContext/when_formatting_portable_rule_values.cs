// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Contexts.for_GenerationContext;

public class when_formatting_portable_rule_values : Specification
{
    static readonly DateTimeOffset _now = new(2026, 10, 15, 8, 30, 0, TimeSpan.Zero);

    string _defaultTenant = null!;
    string _defaultTenantReference = null!;
    string _namedTenant = null!;
    string _namedTenantReference = null!;
    string _notSetTenant = null!;
    string _notSetTenantReference = null!;

    void Because()
    {
        _defaultTenant = Format(new GenerationContext(_now, TenantId.Default));
        _defaultTenantReference = Format(_now, TenantId.Default.Value);
        _namedTenant = Format(new GenerationContext(_now, new TenantId("acme")));
        _namedTenantReference = Format(_now, "acme");
        _notSetTenant = Format(new GenerationContext(_now, TenantId.NotSet));
        _notSetTenantReference = Format(_now, string.Empty);
    }

    [Fact] void should_format_the_default_tenant_like_a_plain_string() => _defaultTenant.ShouldEqual(_defaultTenantReference);
    [Fact] void should_format_a_named_tenant_like_a_plain_string() => _namedTenant.ShouldEqual(_namedTenantReference);
    [Fact] void should_format_an_unset_tenant_like_a_plain_string() => _notSetTenant.ShouldEqual(_notSetTenantReference);
    [Fact] void should_format_the_default_tenant_as_its_value() => _defaultTenant.ShouldEqual("00000000-0000-0000-0000-000000000000:20261015");
    [Fact] void should_format_an_unset_tenant_as_empty() => _notSetTenant.ShouldEqual(":20261015");

    // The Fundamentals binding carries the tenant as a plain string, so its body formats the same way.
    static string Format(GenerationContext context) => $"{context.Tenant}:{context.Now:yyyyMMdd}";

    static string Format(DateTimeOffset now, string tenant) => $"{tenant}:{now:yyyyMMdd}";
}
