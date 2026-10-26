// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

namespace Cratis.Screenplay.Conformance;

internal sealed record SemanticNodeLocation(string Path, string Owner, string Property, string OwnerKey)
{
    public SemanticStringKind StringKind { get; init; }
}
