// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Processing;

/// <summary>
/// A controller-declared third-country transfer and safeguard.
/// </summary>
/// <param name="Destination">The destination.</param>
/// <param name="Safeguard">The declared safeguard.</param>
public sealed record ProcessingTransfer(string Destination, string Safeguard);
