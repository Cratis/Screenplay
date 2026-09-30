// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAbsenceKeyValidation;

public class when_a_migration_would_resolve_debt : given.two_revisions_of_an_absence_key
{
    Exception? _error;

    void Because() => _error = Validate(
        Source.Replace("\"part\":\"old\"", "\"segment\":\"old\"", StringComparison.Ordinal),
        Source.Replace("\"part\":\"old\"", "\"segment\":\"old\"", StringComparison.Ordinal).Replace("  part String", "  segment String", StringComparison.Ordinal),
        WorkspaceAuthoringReferencePolicy.Draft,
        (before, after) => new()
        {
            [AddressOf(before, entry => entry.Node is PropertySyntax { Name: "part" })] = AddressOf(after, entry => entry.Node is PropertySyntax { Name: "segment" })
        });

    [Fact] void should_refuse_the_captured_debt() => _error.ShouldBeOfExactType<InvalidWorkspaceAuthoring>();
    [Fact] void should_explain_the_refusal() => _error!.Message.ShouldContain("Existing absence key debt 'segment' at");
}
