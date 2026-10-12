// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow.given;

static class property_result_snapshots
{
    internal static void Verify(string name, JsonElement property)
    {
        var path = SnapshotPath(name);
        var actual = JsonSerializer.Serialize(property, new JsonSerializerOptions { WriteIndented = true }) + "\n";
        if (Environment.GetEnvironmentVariable("SCREENPLAY_REGENERATE_PROPERTY_RESULTS") == "1")
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, actual);
            Assert.Fail("Property result snapshot regenerated. Review and rerun without SCREENPLAY_REGENERATE_PROPERTY_RESULTS.");
        }
        actual.ShouldEqual(File.ReadAllText(path));
    }

    static string SnapshotPath(string name, [CallerFilePath] string source = "") => Path.Combine(Path.GetDirectoryName(source)!, "..", "PropertyResults", $"{name}.json");
}
