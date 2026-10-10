// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Comparison.for_ModelComparison;

public class when_a_declaration_is_removed : given.two_models
{
    void Establish()
    {
        var catalog = SemanticIdentityCatalog.Create(
            _before.IdentityCatalog.Application,
            _before.IdentityCatalog.Documents,
            [.. _before.IdentityCatalog.Semantics.Where(assignment => !assignment.Address.Parts.Any(part => part.Kind == SemanticAddressPartKind.Slice && part.Key == "Obsolete"))],
            _before.IdentityCatalog.EventContracts);
        ChangeSource(Source[..Source.IndexOf("    slice StateView Obsolete", StringComparison.Ordinal)], catalog);
    }

    void Because() => _result = ModelComparison.Compare(ComparedModel.WithIdentities(_before), ComparedModel.WithIdentities(_after));

    [Fact] void should_report_the_removed_slice() => _result.Declarations.Any(change => change.Declaration.Kind == "Slice" && change.Change == DeclarationChangeKind.Removed).ShouldBeTrue();
}
