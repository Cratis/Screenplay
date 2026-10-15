// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticMatchPattern;

public class when_compiling_an_invalid_pattern : Specification
{
    MatchPatternResult _result;

    void Because() => _result = SemanticMatchPattern.Parse("$[");

    [Fact] void should_classify_invalid_syntax() => _result.Invalid.ShouldBeTrue();
    [Fact] void should_locate_the_unterminated_class_in_the_authored_pattern() => _result.Offset.ShouldEqual(1);
    [Fact] void should_name_the_invalid_construct() => _result.Construct.ShouldEqual("unterminated character class");
}
