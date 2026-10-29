// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Cratis.Screenplay.Semantics.Serialization.for_UnknownPreservingSemanticModelReader;

public class when_preserving_unknowns : Specification
{
    byte[] _input;
    byte[] _original;
    byte[] _reemitted;
    UnknownPreservingSemanticDocument _document;
    UnknownEsmMember _propertyMember;
    SemanticId _propertyId;

    void Establish()
    {
        var root = given.documents.Future();
        root["application"]!["future"] = JsonNode.Parse("{\"a\":[null,{\"a\":false,\"z\":1.25}],\"z\":\"caf\u00e9\"}");
        var property = given.documents.Find(root, "type").Parent!.AsObject();
        _propertyId = SemanticId.Parse(property["id"]!.GetValue<string>());
        property["securityIdentityPolicyNotes"] = "ordinary prose";
        _input = given.documents.Seal(root);
        _original = [.. _input];
    }

    void Because()
    {
        _document = UnknownPreservingSemanticModelReader.Read(_input, given.documents.Range());
        _propertyMember = _document.Unknowns.Single(member => member.Name == "securityIdentityPolicyNotes");
        using var parsed = JsonDocument.Parse(_document.CopyBytes());
        _reemitted = new EsmAdmissionCanonicalJson(parsed.RootElement, 1, 0).Write(projection: false, revision: _document.Revision);
        _input[0] = 0;
        var copy = _document.CopyBytes();
        copy[0] = 0;
    }

    [Fact] void should_own_the_original_full_bytes() => _document.CopyBytes().ShouldEqual(_original);
    [Fact] void should_reemit_the_opaque_subtree_byte_identically() => _reemitted.ShouldEqual(_original);
    [Fact] void should_keep_the_wire_minor_distinct_from_the_projection() => (_document.SemanticVersion.Minor == uint.MaxValue && _document.Model.SemanticVersion == SemanticVersion.V1).ShouldBeTrue();
    [Fact] void should_retain_the_full_revision_not_the_projection_revision() => _document.Revision.ShouldNotEqual(_document.Model.Revision);
    [Fact] void should_attribute_the_property_extension_to_its_known_property() => _propertyMember.Owner.ShouldEqual(_propertyId);
    [Fact] void should_not_classify_from_security_words_or_an_identity() => _propertyMember.IsSecurityRelevant.ShouldBeFalse();
    [Fact] void should_not_discover_semantics_inside_the_opaque_object() => _document.Unknowns.Length.ShouldEqual(2);
    [Fact] void should_preserve_the_opaque_value_bytes() => Encoding.UTF8.GetString(_document.Unknowns.Single(member => member.Name == "future").Value.AsSpan()).ShouldEqual("{\"a\":[null,{\"a\":false,\"z\":1.25}],\"z\":\"caf\\u00E9\"}");
}
#endif
