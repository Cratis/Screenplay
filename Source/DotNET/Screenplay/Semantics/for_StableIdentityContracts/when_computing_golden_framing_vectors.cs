// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Screenplay.Semantics.for_StableIdentityContracts;

public class when_computing_golden_framing_vectors : Specification
{
    const string ExpectedApplicationIdentity = "app1:20ccb167f2400bc55fae1597b1a0f4d19b40841f513bd013a7fa815e9e7f2994";
    const string ExpectedSemanticRevision = "rev1:67cf273e3f1e1c59b0f490fc3a89118355c2aeddee97c06c3c736901f86076fc";
    const string ExpectedCatalogRevision = "catrev1:328c6e1ac4d7527fecef23e1912eebc742447705412a2aa11d7d85df18054258";
    string _applicationIdentity;
    string _semanticRevision;
    string _catalogRevision;

    void Because()
    {
        // These vectors freeze the first public v1 length-prefixed, domain-separated framing.
        var payload = Encoding.UTF8.GetBytes("{\"golden\":true}");
        _applicationIdentity = ApplicationIdentity.Create("Projects").ToString();
        _semanticRevision = SemanticRevision.Compute(payload).ToString();
        _catalogRevision = CatalogRevision.Compute(payload).ToString();
    }

    [Fact] void should_match_the_application_identity_vector() => _applicationIdentity.ShouldEqual(ExpectedApplicationIdentity);
    [Fact] void should_match_the_semantic_revision_vector() => _semanticRevision.ShouldEqual(ExpectedSemanticRevision);
    [Fact] void should_match_the_catalog_revision_vector() => _catalogRevision.ShouldEqual(ExpectedCatalogRevision);
}
