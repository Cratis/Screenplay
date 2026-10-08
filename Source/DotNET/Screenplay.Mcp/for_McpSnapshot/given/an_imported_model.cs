// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpSnapshot.given;

public class an_imported_model : Specification
{
    protected static readonly IReadOnlyDictionary<string, string> Sources = new Dictionary<string, string>
    {
        ["application.play"] = "import \"b.play\"\nimport \"a.play\"\n",
        ["a.play"] = "module A\n  feature F\n    slice StateView V\n      projection R\n        from E\n",
        ["b.play"] = "module B\n  feature G\n    slice StateChange W\n      event E\n"
    };
    private protected McpSnapshot _snapshot;
}
