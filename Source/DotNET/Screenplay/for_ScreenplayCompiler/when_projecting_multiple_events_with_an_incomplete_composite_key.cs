// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_projecting_multiple_events_with_an_incomplete_composite_key : given.a_compiler
{
    [Theory]
    [InlineData("")]
    [InlineData("          key\n            first = first\n")]
    void should_report_the_missing_key_parts_once_per_block(string key)
    {
        var result = _compiler.Compile("module M\n  feature F\n    slice StateView S\n      event A\n        first String\n      event B\n        first String\n      readmodel Row\n        first String key\n        second String key\n      projection Rows => Row\n        from A, B\n" + key);
        result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContainOnly(DiagnosticCodes.IncompleteReadModelKey);
        result.Diagnostics.Count(diagnostic => diagnostic.Code == DiagnosticCodes.IncompleteReadModelKey).ShouldEqual(1);
    }
}
