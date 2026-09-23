// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Reflection;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticExecutionPlan;

// The plan walks modules -> features (nested) -> slices and indexes six slice members. A containment level or a
// slice member added to ESM without teaching the plan would be skipped silently and never executed or rejected,
// so the ESM shape is held against exactly what SemanticExecutionPlan.Compile traverses. A scoped projection nests further
// levels - children and nested objects, to any depth - which SemanticScopedProjection and SemanticScopedProjectionIssues walk,
// so those levels and the members of a level are held here too.
public class when_inspecting_the_containment_it_traverses : Specification
{
    static readonly Type[] _containers = [typeof(SemanticModule), typeof(SemanticFeature), typeof(SemanticSlice)];
    static readonly Type[] _projectionLevels = [typeof(SemanticProjectionScope), typeof(SemanticProjectionChildren), typeof(SemanticProjectionNested)];
    string[] _containment;
    string[] _sliceMembers;
    string[] _projectionContainment;
    string[] _scopeMembers;

    void Because()
    {
        var records = typeof(SemanticApplication).Assembly.GetTypes()
            .Where(type => type.IsPublic && type.Namespace == typeof(SemanticApplication).Namespace)
            .ToArray();
        _containment = [.. records
            .SelectMany(type => type.GetProperties(BindingFlags.Public | BindingFlags.Instance), (type, property) => (type, property))
            .Where(_ => _containers.Contains(ElementType(_.property.PropertyType)))
            .Select(_ => $"{_.type.Name}.{_.property.Name}")];
        _projectionContainment = [.. records
            .SelectMany(type => type.GetProperties(BindingFlags.Public | BindingFlags.Instance), (type, property) => (type, property))
            .Where(_ => _projectionLevels.Contains(ElementType(_.property.PropertyType)))
            .Select(_ => $"{_.type.Name}.{_.property.Name}")];
        _scopeMembers = [.. typeof(SemanticProjectionScope).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.GetMethod?.IsStatic == false)
            .Select(property => property.Name)];
        _sliceMembers = [.. typeof(SemanticSlice).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.PropertyType.IsGenericType && property.PropertyType.GetGenericTypeDefinition() == typeof(ImmutableArray<>))
            .Select(property => property.Name)];
    }

    [Fact] void should_only_contain_the_levels_the_plan_walks() =>
        _containment.ShouldContainOnly(["SemanticApplication.Modules", "SemanticModule.Features", "SemanticFeature.Features", "SemanticFeature.Slices"]);

    [Fact] void should_only_carry_the_slice_members_the_plan_indexes() =>
        _sliceMembers.ShouldContainOnly(["Events", "Commands", "ReadModels", "Projections", "Queries", "Specifications"]);

    [Fact] void should_only_nest_the_projection_levels_the_executor_walks() =>
        _projectionContainment.ShouldContainOnly(["SemanticProjection.Scope", "SemanticProjectionScope.Children", "SemanticProjectionScope.Nested", "SemanticProjectionChildren.Scope", "SemanticProjectionNested.Scope"]);

    [Fact] void should_only_carry_the_scope_members_the_executor_handles() =>
        _scopeMembers.ShouldContainOnly(["From", "Joins", "Children", "Nested", "Every", "Removals", "JoinRemovals"]);

    static Type ElementType(Type type) =>
        type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ImmutableArray<>) ? type.GetGenericArguments()[0] : type;
}
