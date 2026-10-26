// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text.Json.Nodes;

namespace Cratis.Screenplay.Semantics.Serialization.for_UnknownPreservingSemanticModelReader;

public class when_enforcing_admission : Specification
{
    Exception?[] _refusals;
    Exception? _strictFuture;
    Exception? _registeredUnknown;
    UnknownPreservingSemanticDocument _olderMajor;

    void Because()
    {
        var root = given.documents.Future();
        root["application"]!["future"] = true;
        var bytes = given.documents.Seal(root);
        _refusals = [.. new[] { new EsmAdmissionRange(1, 0, 0), new(2, 0, uint.MaxValue), new(1, 5, 4), default }
            .Select(range => Catch.Exception(() => UnknownPreservingSemanticModelReader.Read(bytes, range)))];
        _strictFuture = Catch.Exception(() => SemanticModelSerializer.Deserialize(bytes));
        root["languageVersion"] = "1.0";
        root["semanticVersion"] = "1.0";
        _registeredUnknown = Catch.Exception(() => UnknownPreservingSemanticModelReader.Read(given.documents.Seal(root), given.documents.Range()));
        _olderMajor = UnknownPreservingSemanticModelReader.Read(bytes, given.documents.Range());
    }

    [Fact] void should_refuse_out_of_range_cross_major_and_invalid_ranges() => _refusals.All(error => error is InvalidSemanticContract).ShouldBeTrue();
    [Fact] void should_leave_strict_reading_unchanged() => _strictFuture.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_refuse_unknowns_at_registered_pairs() => _registeredUnknown.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_not_confuse_latest_major_allocation_with_older_major_reading() => _olderMajor.LanguageVersion.Major.ShouldEqual(1u);
}
#endif
