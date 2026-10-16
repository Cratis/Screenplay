// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticDateComparisons;

public class when_comparing_invalid_date_values : Specification
{
    Exception[] _errors;

    void Because() => _errors = new (SemanticValue Left, SemanticValue Right, SemanticPrimitiveType Primitive)[]
    {
        (SemanticValue.Number(1), SemanticValue.Text("2026-01-01"), SemanticPrimitiveType.Date),
        (SemanticValue.Text("2026-01-01"), SemanticValue.Number(1), SemanticPrimitiveType.Date),
        (SemanticValue.Text("not a date"), SemanticValue.Text("2026-01-01"), SemanticPrimitiveType.Date),
        (SemanticValue.Text("2026-01-01"), SemanticValue.Text("2026-02-30"), SemanticPrimitiveType.Date),
        (SemanticValue.Text("2026-01-01T00:00"), SemanticValue.Text("2026-01-01T00:00Z"), SemanticPrimitiveType.DateTime),
        (SemanticValue.Text("2026-01-01T00:00Z"), SemanticValue.Text("invalid"), SemanticPrimitiveType.DateTime),
        (SemanticValue.Text("2026-01-01"), SemanticValue.Text("2026-01-01"), SemanticPrimitiveType.Text)
    }.Select(example => Catch.Exception(() => SemanticDateComparisons.Compare(example.Left, example.Right, example.Primitive))).ToArray();

    [Fact] void should_refuse_every_invalid_pair() => _errors.ShouldEachConformTo(error => error is InvalidSemanticContract);
    [Fact] void should_preserve_the_non_text_refusal() => _errors[0].Message.ShouldEqual("A date comparison requires text values of its declared date primitive.");
    [Fact] void should_preserve_the_unparseable_refusal() => _errors[2].Message.ShouldEqual("A date comparison requires valid dates or explicitly offset instants.");
}
