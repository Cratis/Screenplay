// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution;

internal sealed record SemanticHandlerResultViolation(int? ItemIndex, string Reason)
{
    internal string Describe() => ItemIndex is { } index ? $"result item {index}: {Reason}" : Reason;
}
