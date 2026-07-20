// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Printing;

namespace Cratis.Screenplay.for_ScreenplayWriter;

public class when_inspecting_assembly_visibility : Specification
{
    IEnumerable<Type> _leaked;

    // A public type nested inside an internal type is enumerated by Cratis type discovery (IsNestedPublic)
    // but cannot be referenced by consumers because its container is internal, producing CS0122 in the
    // generated type-discovery provider of any consuming application.
    void Because() => _leaked = typeof(ScreenplayPrinter).Assembly.GetTypes()
        .Where(type => type.IsNestedPublic && type.DeclaringType is { IsPublic: false })
        .ToList();

    [Fact] void should_not_expose_public_types_nested_in_internal_types() => _leaked.ShouldBeEmpty();
}
