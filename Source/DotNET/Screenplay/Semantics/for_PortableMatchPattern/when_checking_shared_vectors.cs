// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.for_SemanticMatchPattern;

namespace Cratis.Screenplay.Semantics.for_PortableMatchPattern;

public class when_checking_shared_vectors
{
    [Fact]
    void should_report_the_public_admission_contract_for_every_shared_case()
    {
        using var document = PortableMatchVectors.Read("portable-match-dialect.json");
        var cases = document.RootElement.GetProperty("cases").EnumerateArray().ToArray();
        cases.Length.ShouldBeGreaterThan(80);
        foreach (var vector in cases)
        {
            var result = PortableMatchPattern.Check(vector.GetProperty("pattern").GetString()!);
            var admitted = vector.GetProperty("accepted").GetBoolean();
            var invalid = !admitted && vector.GetProperty("invalid").GetBoolean();
            var (status, code) = (admitted, invalid) switch
            {
                (true, _) => (PortableMatchStatus.Admitted, (string?)null),
                (_, true) => (PortableMatchStatus.Invalid, "PLAY0367"),
                _ => (PortableMatchStatus.Refused, "PLAY0699")
            };
            result.Status.ShouldEqual(status);
            result.Code.ShouldEqual(code);
            result.Construct.ShouldEqual(admitted ? null : vector.GetProperty("construct").GetString());
            result.Offset.ShouldEqual(admitted ? 0 : vector.GetProperty("offset").GetInt32());
            result.Suggestion.ShouldEqual(admitted || invalid ? null : document.RootElement.GetProperty("suggestions").GetProperty(result.Construct!).GetString());
            result.Description.ShouldEqual(admitted ? string.Empty : $"{result.Construct} at offset {result.Offset}. {result.Suggestion}".TrimEnd());
            result.Replacement.ShouldBeNull();
        }
    }
}
