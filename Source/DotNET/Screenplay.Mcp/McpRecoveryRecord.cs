// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Cratis.Screenplay.Mcp;

sealed record McpRecoveryRecord(int Version, string OperationId, string BeforeWorkspace, string AfterWorkspace, byte[]? BeforeState, byte[] AfterState, McpRecoveryAccess[] Access, McpRecoveryAccess? StateAccess, [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] McpNotesJournal? Notes = null, [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] McpReleaseBaselineJournal? ReleaseBaseline = null);
