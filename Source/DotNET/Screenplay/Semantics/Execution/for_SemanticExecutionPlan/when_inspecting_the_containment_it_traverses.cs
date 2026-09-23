// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Reflection;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticExecutionPlan;

// The plan walks modules -> features (nested) -> slices and indexes seven slice members, constraints by name. A containment level or a
// slice member added to ESM without teaching the plan would be skipped silently and never executed or rejected,
// so the ESM shape is held against exactly what SemanticExecutionPlan.Compile traverses.
public class when_inspecting_the_containment_it_traverses : Specification
{
    static readonly Type[] _containers = [typeof(SemanticModule), typeof(SemanticFeature), typeof(SemanticSlice)];
    string[] _containment;
    string[] _sliceMembers;

    void Because()
    {
        var records = typeof(SemanticApplication).Assembly.GetTypes()
            .Where(type => type.IsPublic && type.Namespace == typeof(SemanticApplication).Namespace)
            .ToArray();
        _containment = [.. records
            .SelectMany(type => type.GetProperties(BindingFlags.Public | BindingFlags.Instance), (type, property) => (type, property))
            .Where(_ => _containers.Contains(ElementType(_.property.PropertyType)))
            .Select(_ => $"{_.type.Name}.{_.property.Name}")];
        _sliceMembers = [.. typeof(SemanticSlice).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.PropertyType.IsGenericType && property.PropertyType.GetGenericTypeDefinition() == typeof(ImmutableArray<>))
            .Select(property => property.Name)];
    }

    [Fact] void should_only_contain_the_levels_the_plan_walks() =>
        _containment.ShouldContainOnly(["SemanticApplication.Modules", "SemanticModule.Features", "SemanticFeature.Features", "SemanticFeature.Slices"]);

    [Fact] void should_only_carry_the_slice_members_the_plan_indexes() =>
        _sliceMembers.ShouldContainOnly(["Events", "Commands", "ReadModels", "Projections", "Queries", "Specifications", "Constraints"]);

    static Type ElementType(Type type) =>
        type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ImmutableArray<>) ? type.GetGenericArguments()[0] : type;
}
