// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Tool.Mcp;

sealed record McpFixtureValue(
    McpReadOwner Specification,
    string Role,
    int Occurrence,
    string Target,
    IEnumerable<McpReadOwner> Candidates,
    string Property,
    TypeRefSyntax? DeclaredType,
    string ExpressionKind,
    object? Value,
    SourceLocation Location);
