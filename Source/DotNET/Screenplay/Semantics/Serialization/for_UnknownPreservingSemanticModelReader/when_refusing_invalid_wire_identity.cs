// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Cratis.Screenplay.Semantics.Serialization.for_UnknownPreservingSemanticModelReader;

public class when_refusing_invalid_wire_identity : Specification
{
    Exception?[] _errors;

    void Because()
    {
        var unsupported = given.documents.Minimal();
        unsupported["schemaVersion"] = 35;
        unsupported["languageVersion"] = $"35.{uint.MaxValue}";
        unsupported["semanticVersion"] = $"35.{uint.MaxValue}";
        var mismatched = given.documents.Minimal();
        mismatched["languageVersion"] = "1.0";
        var crossMajor = given.documents.Minimal();
        crossMajor["languageVersion"] = $"2.{uint.MaxValue}";
        var noncanonical = given.documents.Minimal();
        noncanonical["languageVersion"] = $"01.{uint.MaxValue}";
        _errors =
        [
            Catch.Exception(() => UnknownPreservingSemanticModelReader.Read(given.documents.Seal(unsupported), new(35, 0, uint.MaxValue))),
            Catch.Exception(() => UnknownPreservingSemanticModelReader.Read(given.documents.Seal(mismatched), given.documents.Range())),
            Catch.Exception(() => UnknownPreservingSemanticModelReader.Read(given.documents.Seal(crossMajor), given.documents.Range())),
            Catch.Exception(() => UnknownPreservingSemanticModelReader.Read(given.documents.Seal(noncanonical), given.documents.Range())),
            Catch.Exception(() => UnknownPreservingSemanticModelReader.Read([0xff], given.documents.Range()))
        ];
    }

    [Fact] void should_refuse_unshipped_majors_mismatched_pairs_noncanonical_versions_and_invalid_utf8() => _errors.All(error => error is InvalidSemanticContract).ShouldBeTrue();
}
#endif
