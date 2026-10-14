// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Contexts.for_GenerationContext;

public class when_inspecting_the_closed_context : Specification
{
    [Fact] void should_expose_only_the_generation_instant_and_portable_tenant() => typeof(GenerationContext).GetProperties().Select(property => property.Name).Order(StringComparer.Ordinal).ShouldEqual("Now", "Tenant");
    [Fact] void should_keep_the_now_contract_as_datetimeoffset() => typeof(GenerationContext).GetProperty("Now")!.PropertyType.ShouldEqual(typeof(DateTimeOffset));
    [Fact] void should_keep_the_tenant_contract_portable() => typeof(GenerationContext).GetProperty("Tenant")!.PropertyType.ShouldEqual(typeof(TenantId));
    [Fact] void should_keep_the_compiler_out_of_context_dependencies() => typeof(GenerationContext).Assembly.GetReferencedAssemblies().Any(assembly => assembly.Name == "Cratis.Screenplay").ShouldBeFalse();
}
