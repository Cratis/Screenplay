// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Parsing.for_SpecificationParser;

public class when_refusing_misplaced_tag_expectations : Specification
{
    string[][] _codes;

    void Because() => _codes = new[]
    {
        "specification S\n  given E\n    tag audit",
        "specification S\n  when append E\n    tag audit",
        "specification S\n  when C\n    no tags",
        "specification S\n  when redelivered E to R\n    tag audit",
        "specification S\n  then E\n    tag audit\n    no tags",
        "specification S\n  then E\n    no tags\n    tag audit",
        "specification S\n  then E\n    no tags\n    no tags",
        "specification S\n  then E\n    tag $context.occurred",
        "specification S\n  then E\n    tag 123",
        "specification S\n  then E\n    tag \"\"",
        "specification S\n  then E\n    tag \" \""
    }.Select(source => new ScreenplayCompiler().CompileSpecification(source).Diagnostics.Select(diagnostic => diagnostic.Code).ToArray()).ToArray();

    [Fact] void should_report_play1148_for_every_misuse() => _codes.All(codes => codes.Contains("PLAY1148")).ShouldBeTrue();
}
