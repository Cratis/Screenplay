// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_CompositeReadModelKeysCorpus;

public class when_refusing_invalid_query_part_arguments : given.a_flat_key_model
{
    [Theory]
    [InlineData("when query Find\n  first = \"a\"\nthen no result")]
    [InlineData("when query Find\n  first = \"a\"\n  second = \"b\"\n  unknown = \"x\"\nthen no result")]
    [InlineData("when query Find\n  first = \"a\"\n  second = \"b\"\n  first = \"a\"\nthen no result")]
    [InlineData("then query Find\n  arguments\n    first = \"a\"")]
    [InlineData("then query Find\n  arguments\n    first = \"a\"\n    second = \"b\"\n    unknown = \"x\"")]
    [InlineData("then query Find\n  arguments\n    first = \"a\"\n    second = \"b\"\n    second = \"b\"")]
    public void should_report_the_missing_unknown_or_repeated_argument(string steps)
    {
        var result = Compile(Specification(steps));
        result.Success.ShouldBeFalse();
        result.Value.ShouldBeNull();
        result.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).Select(diagnostic => diagnostic.Code).ShouldContainOnly(DiagnosticCodes.UnknownSpecificationQueryArgument);
    }
}
