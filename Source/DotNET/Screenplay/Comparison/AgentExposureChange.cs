// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Comparison;

/// <summary>An individual membership or metadata change on the explicit agent surface.</summary>
/// <param name="Change">The finding category; only Added means newly exposed.</param>
/// <param name="Before">The baseline entry, absent for additions.</param>
/// <param name="After">The candidate entry, absent for removals.</param>
/// <param name="ScopeChange">Scope widening or narrowing, not an authorization verdict.</param>
public sealed record AgentExposureChange(AgentExposureChangeKind Change, AgentExposureOperation? Before, AgentExposureOperation? After, AgentExposureScopeChange ScopeChange = AgentExposureScopeChange.None);
