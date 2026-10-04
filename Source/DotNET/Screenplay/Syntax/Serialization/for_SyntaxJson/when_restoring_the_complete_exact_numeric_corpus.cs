// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Text.Json;
using Cratis.Screenplay.Printing;

namespace Cratis.Screenplay.Syntax.Serialization.for_SyntaxJson;

public class when_restoring_the_complete_exact_numeric_corpus
{
    [Fact]
    void should_compare_every_structural_member_in_both_directions_and_round_trip_source()
    {
        var folder = Path.Combine(Root(), "Source", "Screenplay", "Compiler", "Conformance");
        var compiler = new ScreenplayCompiler();
        var source = compiler.Parse(File.ReadAllText(Path.Combine(folder, "exact-numbers.play"))).Value!;
        using var wire = JsonDocument.Parse(File.ReadAllText(Path.Combine(folder, "exact-numbers.syntax.json")));
        var restored = (ApplicationSyntax)SyntaxJson.Deserialize(wire.RootElement);

        // Deliberately compare complete native trees, not a projection or a list of numeric values.
        SyntaxJson.Serialize(restored).GetRawText().ShouldEqual(SyntaxJson.Serialize(source).GetRawText());
        var printed = new ScreenplayPrinter().Print(restored);
        printed.StartsWith("numbers exact\n", StringComparison.Ordinal).ShouldBeTrue();
        printed.Split("numbers exact", StringSplitOptions.None).Length.ShouldEqual(3); // Preamble and the property named numbers.
        var reparsed = compiler.Parse(printed).Value!;
        SyntaxJson.Serialize(reparsed).GetRawText().ShouldEqual(SyntaxJson.Serialize(restored).GetRawText());
    }

    static string Root([CallerFilePath] string path = "")
    {
        var directory = Directory.GetParent(path);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "Documentation"))) directory = directory.Parent;

        return directory!.FullName;
    }
}
