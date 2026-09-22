// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Tool.Mcp;

sealed record McpDiskResult(bool Success, string Status, IReadOnlyList<string> Recovery, int PlannedChanges, int InstalledDocuments);
