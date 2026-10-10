// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticMatchPattern;

public class when_matching_without_anchors : Specification
{
    bool _result;

    void Because() => _result = SemanticMatchPattern.Create("B").IsMatch("ABC");

    [Fact] void should_find_a_substring() => _result.ShouldBeTrue();
}
