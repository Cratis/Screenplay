// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_parsing_legacy_handler_forms : given.a_compiler
{
    const string Prefix = "module M\n  feature F\n    slice StateChange S\n      command C\n";
    const string Inline = "```csharp\n          return null;\n          ```";
    const string WrappedInline = "implementation\n            ```csharp\n            return null;\n            ```";

    [Theory]
    [InlineData("        handler\n          implementation")]
    [InlineData("        value String\n        returns value\n        handler\n          implementation")]
    [InlineData("        produces E\n        handler\n          implementation")]
    void should_keep_valid_authoring_forms(string body) => _compiler.Parse(Prefix + body).Diagnostics.ShouldBeEmpty();

    [Theory]
    [InlineData(Inline, Inline)]
    [InlineData("file First.cs", Inline)]
    [InlineData(Inline, "file Last.cs")]
    [InlineData("file First.cs", "file Last.cs")]
    [InlineData(WrappedInline, "implementation\n            file Last.cs")]
    [InlineData("implementation\n            file First.cs", WrappedInline)]
    [InlineData("file First.cs", "")]
    [InlineData("", "file Last.cs")]
    void should_refuse_duplicate_attached_handlers(string first, string second)
    {
        var firstPart = Prefix + "        handler\n          " + first + "\n";
        var result = _compiler.Parse(firstPart + "        handler\n          " + second);
        result.Success.ShouldBeFalse();
        var diagnostic = result.Diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.DuplicateDeclaration);
        diagnostic.Location.Line.ShouldEqual(firstPart.Split('\n').Length);
    }

    [Fact]
    void should_keep_a_missing_handler_distinct_from_a_duplicate()
    {
        var result = _compiler.Parse(Prefix + "        handler");
        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == DiagnosticCodes.HandlerWithoutImplementation);
        result.Diagnostics.ShouldNotContain(diagnostic => diagnostic.Code == DiagnosticCodes.DuplicateDeclaration);
    }

    [Theory]
    [InlineData("file C.cs")]
    [InlineData("implementation\n            file C.cs")]
    [InlineData("```csharp\n          return null;\n          ```")]
    [InlineData("implementation\n            ```csharp\n            return null;\n            ```")]
    void should_admit_attached_handlers_beside_productions(string body) =>
        _compiler.Parse(Prefix + "        produces E\n        handler\n          " + body).Success.ShouldBeTrue();
}
