// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Cratis.Screenplay.Semantics.Serialization.for_UnknownPreservingSemanticModelReader;

public class when_ordering_admission_refusals : Specification
{
    Exception? _outsideRange;
    Exception? _unknownMajor;
    Exception? _mismatchedPair;

    void Because()
    {
        var root = given.documents.Minimal();
        root["schemaVersion"] = 35;
        root["languageVersion"] = $"35.{uint.MaxValue}";
        root["semanticVersion"] = $"35.{uint.MaxValue}";
        var bytes = given.documents.Seal(root);
        _outsideRange = Catch.Exception(() => UnknownPreservingSemanticModelReader.Read(bytes, given.documents.Range()));
        _unknownMajor = Catch.Exception(() => UnknownPreservingSemanticModelReader.Read(bytes, new(35, 0, uint.MaxValue)));
        root["languageVersion"] = $"36.{uint.MaxValue}";
        _mismatchedPair = Catch.Exception(() => UnknownPreservingSemanticModelReader.Read(given.documents.Seal(root), given.documents.Range()));
    }

    [Fact] void should_check_the_caller_range_before_descriptor_availability() => (_outsideRange is InvalidSemanticContract && _outsideRange.Message.Contains("outside the caller", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_check_descriptor_availability_only_after_the_range_admits_the_major() => (_unknownMajor is InvalidSemanticContract && _unknownMajor.Message.Contains("no frozen baseline", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_check_wire_pair_consistency_before_the_caller_range() => (_mismatchedPair is InvalidSemanticContract && _mismatchedPair.Message.Contains("pair must match", StringComparison.Ordinal)).ShouldBeTrue();
}
#endif
