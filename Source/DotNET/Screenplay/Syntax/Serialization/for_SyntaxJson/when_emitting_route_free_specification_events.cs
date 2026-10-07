// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

namespace Cratis.Screenplay.Syntax.Serialization.for_SyntaxJson;

public class when_emitting_route_free_specification_events : Specification
{
    string _json;

    void Because() => _json = SyntaxJson.Serialize(new ScreenplayCompiler().CompileSpecification("specification X\n  then E").Value!.ThenEvents.Single()).GetRawText();

    [Fact]
    void should_hold_the_typescript_decoder_fixture_to_the_csharp_emitter() => _json.ShouldEqual(File.ReadAllText(Fixture()).TrimEnd());

    static string Fixture([CallerFilePath] string path = "")
    {
        var root = new DirectoryInfo(Path.GetDirectoryName(path)!);
        while (!Directory.Exists(Path.Combine(root.FullName, "Source", "Screenplay", "Compiler"))) root = root.Parent!;

        return Path.Combine(root.FullName, "Source", "Screenplay", "Compiler", "Conformance", "specification-event-without-route.json");
    }
}
