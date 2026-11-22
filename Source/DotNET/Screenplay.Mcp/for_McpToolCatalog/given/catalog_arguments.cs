// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpToolCatalog.given;

public class catalog_arguments : Specification
{
    protected string _tool;
    protected JsonDocument _arguments;
    protected bool _visual;
    protected Exception _error;

    protected McpFailure Failure => (McpFailure)_error;

    void Destroy() => _arguments.Dispose();
}
