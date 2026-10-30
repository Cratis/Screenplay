// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Cratis.Screenplay.Semantics.Serialization.for_UnknownPreservingSemanticModelReader;

public class when_enforcing_the_document_byte_limit : Specification
{
    int _acceptedLength;
    Exception? _breach;

    void Because()
    {
        var root = given.documents.Minimal();
        root["application"]!["name"] = string.Empty;
        var overhead = given.documents.Seal(root).Length;
        root["application"]!["name"] = new string('x', 8_388_608 - overhead);
        var bytes = given.documents.Seal(root);
        _acceptedLength = UnknownPreservingSemanticModelReader.Read(bytes, given.documents.Range()).Length;
        _breach = Catch.Exception(() => UnknownPreservingSemanticModelReader.Read([.. bytes, 32], given.documents.Range()));
    }

    [Fact] void should_admit_exactly_eight_mib_and_refuse_one_more_byte_before_parsing() => (_acceptedLength == 8_388_608 && _breach is InvalidSemanticContract && _breach.Message.Contains("maximum document size", StringComparison.Ordinal)).ShouldBeTrue();
}
#endif
