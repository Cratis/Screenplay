// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace Cratis.Screenplay.Syntax.Serialization.for_SyntaxJson;

public class when_freezing_legacy_source_syntax_bytes
{
    [Fact]
    void should_preserve_the_complete_legacy_native_bytes_without_normalizing_values()
    {
        var root = Root();
        var folder = Path.Combine(root, "Source", "Screenplay", "Compiler", "Conformance");
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(folder, "manifest.json")));
        var compiler = new ScreenplayCompiler();
        var initializing = Environment.GetEnvironmentVariable("SCREENPLAY_INITIALIZE_LEGACY_SYNTAX_BYTES") == "1";
        var count = 0;
        foreach (var document in manifest.RootElement.GetProperty("documents").EnumerateArray())
        {
            var parsed = compiler.Parse(File.ReadAllText(Path.Combine(root, document.GetProperty("path").GetString()!))).Value!;
            if (parsed.SourceOptions != SourceOptions.Legacy) continue;
            var actual = Encoding.UTF8.GetBytes(SyntaxJson.Serialize(parsed).GetRawText());
            var path = Path.Combine(folder, "LegacySyntax", document.GetProperty("name").GetString() + ".json");
            if (initializing && !File.Exists(path))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllBytes(path, actual);
            }

            File.ReadAllBytes(path).SequenceEqual(actual).ShouldBeTrue();
            count++;
        }

        count.ShouldEqual(16);
        Assert.False(initializing, "Review the new protected bytes and rerun without SCREENPLAY_INITIALIZE_LEGACY_SYNTAX_BYTES. Existing baselines are never overwritten.");
    }

    static string Root([CallerFilePath] string path = "")
    {
        var directory = Directory.GetParent(path);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "Documentation"))) directory = directory.Parent;

        return directory!.FullName;
    }
}
