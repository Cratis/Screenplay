// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Contexts.for_ContextPackage;

public class when_inspecting_dependencies : Specification
{
    [Fact] void should_not_reference_the_compiler() => typeof(TenantId).Assembly.GetReferencedAssemblies().Any(_ => _.Name == "Cratis.Screenplay").ShouldBeFalse();
    [Fact] void should_keep_the_context_namespace() => typeof(TenantId).Namespace.ShouldEqual("Cratis.Screenplay.Contexts");
}
