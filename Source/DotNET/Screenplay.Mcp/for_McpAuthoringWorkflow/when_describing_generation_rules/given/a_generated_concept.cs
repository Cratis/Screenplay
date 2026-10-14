// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow.when_describing_generation_rules.given;

public class a_generated_concept : for_McpAuthoringWorkflow.given.an_authoring_connection
{
    protected JsonElement _generation;

    internal void Start(string source)
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), source);
        Initialize();
        Open();
    }
}
