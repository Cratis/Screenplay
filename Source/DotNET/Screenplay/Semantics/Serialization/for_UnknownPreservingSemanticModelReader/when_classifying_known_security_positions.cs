// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Cratis.Screenplay.Semantics.Serialization.for_UnknownPreservingSemanticModelReader;

public class when_classifying_known_security_positions : Specification
{
    UnknownPreservingSemanticDocument _document;

    void Because()
    {
        var root = given.documents.Future(13);
        foreach (var position in new[] { "authorization", "givenCaller", "runsAs", "compliance", "route", "affectedInstance" })
        {
            given.documents.Find(root, position)["future"] = true;
        }

        root["application"]!["policies"]![0]!["condition"]!["future"] = true;
        _document = UnknownPreservingSemanticModelReader.Read(given.documents.Seal(root), given.documents.Range(13));
    }

    [Fact] void should_flag_the_known_security_positions() => _document.Unknowns.All(member => member.IsSecurityRelevant).ShouldBeTrue();
    [Fact] void should_include_authorization_identity_compliance_and_routing_categories() => _document.Unknowns.Select(member => member.Category).Distinct().Order().ShouldEqual([UnknownEsmCategory.Authorization, UnknownEsmCategory.IdentityMapping, UnknownEsmCategory.Compliance, UnknownEsmCategory.EventStreamRouting]);
    [Fact] void should_provide_a_path_and_known_owner_for_every_boundary() => _document.Unknowns.All(member => member.Path.StartsWith("/application/", StringComparison.Ordinal) && member.Owner.IsSet).ShouldBeTrue();
}
#endif
