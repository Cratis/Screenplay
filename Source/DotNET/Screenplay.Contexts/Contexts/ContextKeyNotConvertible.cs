// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Contexts;

/// <summary>
/// The exception that is thrown when a context key cannot be converted to the requested type.
/// </summary>
/// <param name="requestedType">The type requested by the caller.</param>
/// <param name="reason">The value-free reason the conversion is not supported.</param>
public class ContextKeyNotConvertible(Type requestedType, string reason)
    : Exception($"Context key cannot be read as {requestedType.FullName}: {reason}");
