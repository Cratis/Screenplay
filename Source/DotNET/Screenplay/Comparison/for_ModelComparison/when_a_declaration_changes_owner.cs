// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Comparison.for_ModelComparison;

public class when_a_declaration_changes_owner : given.two_models
{
    void Establish()
    {
        const string source = "module Projects\n  feature Registration\n    slice StateChange Left\n      command Run\n    slice StateChange Right\n      command Other\n";
        _before = Create(source);
        var catalog = SemanticIdentityCatalog.Create(
            _before.IdentityCatalog.Application,
            _before.IdentityCatalog.Documents,
            [.. _before.IdentityCatalog.Semantics.Select(assignment => assignment.Address.Kind == SemanticKind.Command && assignment.Address.Name == "Run" ? assignment with { Address = SemanticAddress.ForCommand(SemanticAddress.ForSlice(_before.IdentityCatalog.Application, "Projects", "Registration", "Right"), "Run"), Origin = SemanticIdentityOrigin.Persisted } : assignment)],
            _before.IdentityCatalog.EventContracts);
        ChangeSource(source.Replace("      command Run\n", string.Empty, StringComparison.Ordinal) + "      command Run\n", catalog);
    }

    void Because() => _result = ModelComparison.Compare(ComparedModel.WithIdentities(_before), ComparedModel.WithIdentities(_after));

    [Fact] void should_report_the_owner_move() => _result.Declarations.Any(change => change.Change == DeclarationChangeKind.Moved && change.Move == DeclarationMove.Owner && change.BeforeOwner == "Projects.Registration.Left" && change.AfterOwner == "Projects.Registration.Right").ShouldBeTrue();
}
