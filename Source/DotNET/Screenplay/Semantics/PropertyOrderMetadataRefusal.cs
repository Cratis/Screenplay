// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

/// <summary>Refuses an explicit property-order request without silently returning a lower-version model.</summary>
/// <param name="BaseVersion">The version the unchanged binder selected.</param>
public sealed record PropertyOrderMetadataRefusal(SemanticVersion BaseVersion)
{
    /// <summary>Gets the reason the requested metadata cannot be supplied.</summary>
    public string Reason => $"Property-order metadata currently requires a valid ESM 36.0 base containing a state machine or workflow; this model selects {BaseVersion}";
}
