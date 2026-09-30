// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAbsenceKeyValidation;

public class when_a_rename_moves_a_resolved_key_member_along : given.two_revisions_of_an_absence_key
{
    Exception? _error;

    void Because() => _error = Validate(
        Source,
        Source.Replace("\"part\":\"old\"", "\"segment\":\"old\"", StringComparison.Ordinal).Replace("  part String", "  segment String", StringComparison.Ordinal),
        WorkspaceAuthoringReferencePolicy.Safe,
        (before, after) => new()
        {
            [AddressOf(before, entry => entry.Node is PropertySyntax { Name: "part" })] = AddressOf(after, entry => entry.Node is PropertySyntax { Name: "segment" })
        });

    [Fact] void should_accept_the_renamed_declaration_and_key() => _error.ShouldBeNull();
    [Fact] void should_leave_no_debt() => Debt.ShouldBeEmpty();
}
