// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Comparison;

/// <summary>
/// The exception that is thrown when persisted model identities belong to different applications.
/// </summary>
/// <param name="message">The reason identity continuity cannot be compared.</param>
public sealed class IncompatibleModelIdentities(string message) : Exception(message);
