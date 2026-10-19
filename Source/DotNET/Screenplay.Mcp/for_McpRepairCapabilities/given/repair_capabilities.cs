// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpRepairCapabilities.given;

public class repair_capabilities : for_McpAuthoringWorkflow.given.an_authoring_connection
{
    internal JsonElement Capabilities;

    void Establish()
    {
        Initialize();
        Capabilities = Result("repair-capabilities");
    }

    internal JsonElement Action(string code) => Capabilities.GetProperty("actions").EnumerateArray().Single(action => action.GetProperty("diagnosticCode").GetString() == code);

    internal JsonElement Refactoring(string kind) => Capabilities.GetProperty("refactorings").EnumerateArray().Single(refactoring => refactoring.GetProperty("kind").GetString() == kind);
}
