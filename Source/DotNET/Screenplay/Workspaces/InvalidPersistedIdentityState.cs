// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces;

/// <summary>
/// The exception that is thrown when persisted identity state is corrupt, noncanonical, unsupported or exceeds its bound.
/// </summary>
/// <param name="message">The reason the state was refused.</param>
/// <param name="innerException">The underlying failure, if any.</param>
public sealed class InvalidPersistedIdentityState(string message, Exception? innerException = null) : Exception(message, innerException);
