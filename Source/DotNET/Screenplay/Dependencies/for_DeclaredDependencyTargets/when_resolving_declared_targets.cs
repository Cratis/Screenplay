// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Parsing;

namespace Cratis.Screenplay.Dependencies.for_DeclaredDependencyTargets;

public class when_resolving_declared_targets
{
    static readonly Declaration[] _containers =
    [
        new("Payroll", new([])), new("Runs", new([])), new("Timesheets", new([])),
        new("Handover", new(["Payroll"])), new("Runs", new(["Payroll"])),
        new("Approval", new(["Timesheets"])), new("Nested", new(["Payroll", "Handover"])),
        new("Cousin", new(["Payroll", "Runs"]))
    ];

    [Theory]
    [InlineData("Runs", "Payroll.Handover", "Payroll.Runs")]
    [InlineData("Runs", "Payroll.Handover.Nested", "Payroll.Runs")]
    [InlineData("Timesheets", "Payroll.Handover", "Timesheets")]
    [InlineData("Timesheets.Approval", "Payroll.Handover", "Timesheets.Approval")]
    [InlineData("Handover.Nested", "Timesheets.Approval", "Payroll.Handover.Nested")]
    [InlineData("Approval", "Payroll.Handover", null)]
    [InlineData("Cousin", "Payroll.Handover.Nested", null)]
    [InlineData("Nowhere", "Payroll.Handover", null)]
    public void should_search_only_siblings_ancestors_siblings_and_modules(string target, string owner, string? expected)
    {
        var result = DeclaredDependencyTargets.Resolve(target, new(owner.Split('.')), _containers);
        var address = result.Resolved is { } resolved ? string.Join('.', resolved.Scope.Segments.Append(resolved.Name)) : null;
        address.ShouldEqual(expected);
    }

    [Fact]
    public void should_not_choose_an_equally_near_dotted_match()
    {
        var result = DeclaredDependencyTargets.Resolve(
            "Group.Shared",
            new(["Payroll"]),
            [new("Shared", new(["A", "Group"])), new("Shared", new(["B", "Group"]))]);
        result.Resolved.ShouldBeNull();
        result.Ambiguous.Count.ShouldEqual(2);
    }
}
