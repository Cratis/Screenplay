// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_parsing_multiple_handler_occurrences : given.a_compiler
{
    [Theory]
    [InlineData("implementation", "file C.cs")]
    [InlineData("file C.cs", "implementation")]
    [InlineData("implementation", "implementation")]
    [InlineData("implementation\n            hint \"first\"", "implementation\n            hint \"second\"")]
    [InlineData("implementation", "```csharp\n          return null;\n          ```")]
    [InlineData("```csharp\n          return null;\n          ```", "implementation")]
    void should_report_the_second_occurrence(string first, string second)
    {
        var prefix = $"module M\n  feature F\n    slice StateChange S\n      command C\n        handler\n          {first}\n";
        var result = _compiler.Parse(prefix + $"        handler\n          {second}");
        var error = result.Diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.PendingIntentWithOtherHandler);
        error.Location.Line.ShouldEqual(prefix.Count(character => character == '\n') + 1);
        error.Severity.ShouldEqual(DiagnosticSeverity.Error);
    }

    [Fact]
    void should_not_drop_a_pending_intent_before_a_malformed_handler() =>
        _compiler.Parse("module M\n  feature F\n    slice StateChange S\n      command C\n        handler\n          implementation\n        handler").Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.PendingIntentWithOtherHandler).ShouldBeTrue();
}
