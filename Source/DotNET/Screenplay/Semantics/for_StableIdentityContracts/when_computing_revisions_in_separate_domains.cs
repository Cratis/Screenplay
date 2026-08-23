// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.given;

namespace Cratis.Screenplay.Semantics.for_StableIdentityContracts;

public class when_computing_revisions_in_separate_domains : a_valid_semantic_model
{
    string _semanticRevision;
    string _catalogRevision;

    void Because()
    {
        _semanticRevision = _model.Revision.ToString();
        _catalogRevision = _catalog.Revision.ToString();
    }

    [Fact] void should_use_the_semantic_revision_prefix() => _semanticRevision.StartsWith("rev1:", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_use_the_catalog_revision_prefix() => _catalogRevision.StartsWith("catrev1:", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_domain_separate_catalog_and_semantic_revisions() => _catalogRevision.ShouldNotEqual(_semanticRevision);
    [Fact] void should_use_full_sha256_for_catalog_revisions() => _catalogRevision.Length.ShouldEqual(72);
}
