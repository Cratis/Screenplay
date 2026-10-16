// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_parsing_legacy_handler_forms : given.a_compiler
{
    const string Prefix = "module M\n  feature F\n    slice StateChange S\n      command C\n";

    [Theory]
    [InlineData("        handler\n          implementation")]
    [InlineData("        value String\n        returns value\n        handler\n          implementation")]
    [InlineData("        produces E\n        handler\n          implementation")]
    [InlineData("        handler\n          file First.cs\n        handler\n          file Last.cs")]
    void should_keep_valid_authoring_forms(string body) => _compiler.Parse(Prefix + body).Diagnostics.ShouldBeEmpty();

    [Fact]
    void should_keep_the_last_attached_handler() =>
        _compiler.Parse(Prefix + "        handler\n          file First.cs\n        handler\n          file Last.cs").Value!.Modules.Single().Features.Single().Slices.Single().Commands.Single().Handler!.File!.Path.ShouldEqual("Last.cs");

    [Theory]
    [InlineData("file C.cs")]
    [InlineData("implementation\n            file C.cs")]
    [InlineData("```csharp\n          return null;\n          ```")]
    [InlineData("implementation\n            ```csharp\n            return null;\n            ```")]
    void should_keep_refusing_attached_handlers_beside_productions(string body) =>
        _compiler.Parse(Prefix + "        produces E\n        handler\n          " + body).Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.CommandWithProducesAndHandler);
}
