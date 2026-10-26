// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Conformance.for_SemanticModelEquivalence.when_comparing;

public class reordered_set_members : given.compiled_models
{
    void Because()
    {
        var billing = Source.IndexOf("module Billing", StringComparison.Ordinal);
        var reordered = $"{Source[billing..]}\n{Source[..billing].TrimEnd()}";
        _comparison = SemanticModelEquivalence.Compare(Compile(Source), Compile(reordered));
    }

    [Fact] void should_be_equivalent() => _comparison.IsEquivalent.ShouldBeTrue();
}
