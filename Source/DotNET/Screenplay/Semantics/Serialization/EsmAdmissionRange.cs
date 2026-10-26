// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

namespace Cratis.Screenplay.Semantics.Serialization;

/// <summary>
/// Defines caller admission for one ESM major and inclusive minor bounds, not execution authority.
/// </summary>
/// <param name="Major">The single supported major.</param>
/// <param name="MinimumMinor">The inclusive lower minor bound.</param>
/// <param name="MaximumMinor">The inclusive upper minor bound.</param>
[StructLayout(LayoutKind.Auto)]
public readonly record struct EsmAdmissionRange(uint Major, uint MinimumMinor, uint MaximumMinor);
