// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace Cratis.Screenplay.Semantics.for_SemanticMatchPattern;

public class when_compiling_an_invalid_pattern : Specification
{
    Exception _error;
    Exception _authoredError;

    void Establish() => _authoredError = Catch.Exception(() => _ = new Regex("$[", RegexOptions.ECMAScript | RegexOptions.CultureInvariant, SemanticMatchPattern.Timeout));

    void Because() => _error = Catch.Exception(() => SemanticMatchPattern.Create("$["));

    [Fact] void should_preserve_the_authored_pattern_in_the_error() => _error.Message.ShouldEqual(_authoredError.Message);
}
