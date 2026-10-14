// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Validation;

/// <summary>
/// The exception that is thrown when a host passes an unparsed command-line selection to the resolver.
/// </summary>
/// <param name="selection">The unrecognized selection.</param>
public sealed class InvalidValidationGateSelection(string selection) : Exception($"Unrecognized validation gate selection '{selection}'.");
