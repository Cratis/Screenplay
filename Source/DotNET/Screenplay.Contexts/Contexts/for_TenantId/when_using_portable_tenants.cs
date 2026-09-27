// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Contexts.for_TenantId;

public class when_using_portable_tenants : Specification
{
    [Fact] void should_keep_the_published_default() => TenantId.Default.Value.ShouldEqual("00000000-0000-0000-0000-000000000000");
    [Fact] void should_keep_not_set_distinct_from_default() => (TenantId.NotSet == TenantId.Default).ShouldBeFalse();
    [Fact] void should_keep_named_tenants_distinct() => (new TenantId("Customer") == TenantId.Default).ShouldBeFalse();
}
