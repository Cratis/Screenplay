// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Comparison.for_ModelComparison.when_one_side_has_no_persisted_identities;

public class and_a_declaration_was_renamed : given.two_models
{
    void Establish()
    {
        RenameMember();
    }

    void Because() => _result = ModelComparison.Compare(ComparedModel.WithIdentities(_before), ComparedModel.WithoutIdentities(_after));

    [Fact] void should_report_removal_and_addition() => _result.Declarations.Where(change => change.Declaration.Kind == "Property").Select(change => change.Change).ShouldContainOnly(DeclarationChangeKind.Removed, DeclarationChangeKind.Added);
    [Fact] void should_not_infer_a_rename() => _result.Declarations.Any(change => change.Change == DeclarationChangeKind.Renamed).ShouldBeFalse();
    [Fact] void should_disclose_exact_matching() => _result.NotCompared.ShouldContain("Declarations are matched by exact kind and address because at least one side has no persisted identities; renames and owner moves appear as a removal and an addition.");
}
