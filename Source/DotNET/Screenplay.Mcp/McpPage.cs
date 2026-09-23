// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp;

sealed record McpPage<T>(string Revision, int TotalCount, int Offset, IReadOnlyList<T> Items, int? NextOffset);
