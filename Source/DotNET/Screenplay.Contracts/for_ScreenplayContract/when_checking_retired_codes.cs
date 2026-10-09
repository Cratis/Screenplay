// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

namespace Cratis.Screenplay.Contracts.for_ScreenplayContract;

public class when_checking_retired_codes : Specification
{
    string[] _documented;

    void Because()
    {
        var documentation = File.ReadAllText(Path.Combine(Root(), "Documentation/screenplay/diagnostics.md"));
        var retired = documentation.Split("## Retired codes", StringSplitOptions.None)[1];
        _documented = [.. ContractSources.Matches(retired, @"\| `(PLAY\d+)` \|").Order(StringComparer.Ordinal)];
    }

    [Fact] void should_keep_the_explicit_retirement_list_in_step_with_the_catalog_documentation() => ContractDiagnostics.Retired.Order(StringComparer.Ordinal).ShouldEqual(_documented);

    static string Root([CallerFilePath] string path = "")
    {
        var directory = Directory.GetParent(path);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "Documentation"))) directory = directory.Parent;

        return directory!.FullName;
    }
}
