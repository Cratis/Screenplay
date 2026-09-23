// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Mcp;

sealed record McpFixtureOccurrence(McpReadOwner Specification, string Role, McpReference Reference, int Ordinal, IEnumerable<PropertyMappingSyntax> Values, ExpressionSyntax? For = null);
