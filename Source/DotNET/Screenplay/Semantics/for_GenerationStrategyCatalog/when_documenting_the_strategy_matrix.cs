// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Runtime.CompilerServices;

namespace Cratis.Screenplay.Semantics.for_GenerationStrategyCatalog;

public class when_documenting_the_strategy_matrix : Specification
{
    string _documentation;
    string[] _rows;

    void Establish() => _documentation = File.ReadAllText(DocumentationPath());

    void Because() => _rows = [.. GenerationStrategyCatalog.All.Select(strategy => $"| `{strategy.Name}` | {(strategy.Primitives.IsEmpty ? "-" : string.Join(", ", strategy.Primitives))} | {strategy.IntroducedIn?.ToString() ?? "reserved"} | {strategy.FixedBits?.ToString(CultureInfo.InvariantCulture) ?? "-"} | {(strategy.Options.IsEmpty ? "-" : string.Join(", ", strategy.Options.Select(option => option.Name)))} |")];

    [Fact] void should_hold_every_documented_strategy_to_the_public_contract() => _rows.ShouldEachConformTo(row => _documentation.Contains(row, StringComparison.Ordinal));
    [Fact] void should_check_all_ten_strategies() => _rows.Length.ShouldEqual(10);

    static string DocumentationPath([CallerFilePath] string path = "")
    {
        var root = Directory.GetParent(path);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "AGENTS.md"))) root = root.Parent;

        return Path.Combine(root!.FullName, "Documentation/screenplay/generated-values/strategies.md");
    }
}
