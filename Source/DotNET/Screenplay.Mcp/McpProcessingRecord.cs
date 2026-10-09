// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Processing;

namespace Cratis.Screenplay.Mcp;

static class McpProcessingRecord
{
    internal static object Read(McpSnapshot snapshot, JsonElement arguments)
    {
        if (!snapshot.Compilation.Success) throw new McpFailure("Processing record requires an error-free source model; fix source errors before deriving coverage.");
        var report = ProcessingRecord.Create(snapshot.Compilation.Value!, McpJson.OptionalString(arguments, "controllerName"), McpJson.OptionalString(arguments, "controllerContact"));

        return new
        {
            success = true,
            snapshot.SourceRevision,
            report.Notice,
            report.Coverage,
            report.ControllerName,
            report.ControllerContact,
            rows = McpPaging.BoundedSourcePage(report.Rows.Cast<object>(), arguments, snapshot.SourceRevision)
        };
    }
}
