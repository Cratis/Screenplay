// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Cratis.Screenplay.Semantics.Serialization.given;

/// <summary>
/// The exception that is thrown to fail a spec run that rewrote the checked-in golden vectors.
/// </summary>
/// <param name="message">The message that says what was regenerated and what to do next.</param>
public sealed class GoldenVectorsRegenerated(string message) : Exception(message);
#endif
