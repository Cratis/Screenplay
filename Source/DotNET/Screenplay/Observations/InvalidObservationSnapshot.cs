// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Observations;

/// <summary>
/// The exception that is thrown when observation evidence is inconsistent.
/// </summary>
/// <param name="message">The violated snapshot invariant.</param>
public sealed class InvalidObservationSnapshot(string message) : Exception(message);
