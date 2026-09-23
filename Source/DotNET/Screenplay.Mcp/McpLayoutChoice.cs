// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp;

sealed record McpLayoutChoice(string Layout, int FileCount, int SourceBytes, bool Admissible, string? Reason);
