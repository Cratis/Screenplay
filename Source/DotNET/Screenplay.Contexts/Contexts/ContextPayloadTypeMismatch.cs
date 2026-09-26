// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Contexts;

/// <summary>
/// The exception that is thrown when a context payload is not of the type requested by a typed accessor.
/// </summary>
/// <param name="member">The context member whose payload could not be cast.</param>
/// <param name="requestedType">The type requested by the caller.</param>
/// <param name="actualType">The actual payload type, or null when the payload is null.</param>
public class ContextPayloadTypeMismatch(string member, Type requestedType, Type? actualType)
    : Exception($"Context payload '{member}' is {actualType?.FullName ?? "null"}, not the requested type {requestedType.FullName}.");
