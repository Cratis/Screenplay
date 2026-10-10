// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Comparison.for_ModelComparison;

public class when_comparing_identical_models : given.two_models
{
    void Because() => _result = ModelComparison.Compare(ComparedModel.WithIdentities(_before), ComparedModel.WithIdentities(_after));

    [Fact] void should_match_by_identity() => _result.Matching.ShouldEqual(DeclarationMatching.Identity);
    [Fact] void should_be_complete() => _result.Complete.ShouldBeTrue();
    [Fact] void should_report_no_semantic_change() => _result.HasSemanticChange.ShouldEqual(false);
    [Fact] void should_report_declaration_counts() => _result.BeforeDeclarations.ShouldEqual(_before.IdentityCatalog.Semantics.Length);
    [Fact] void should_keep_equal_counts() => _result.AfterDeclarations.ShouldEqual(_result.BeforeDeclarations);
    [Fact] void should_have_no_changes() => (_result.Declarations.Count + _result.Members.Count + _result.Events.Count + _result.Specifications.Count + _result.Dependants.Count + _result.Identities.Count).ShouldEqual(0);
}
