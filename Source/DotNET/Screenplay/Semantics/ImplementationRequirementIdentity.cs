// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;

namespace Cratis.Screenplay.Semantics;

internal static class ImplementationRequirementIdentity
{
    // This is the existing attachment identity encoding. Allocation/disambiguation belongs to callers.
    internal static string Create(SemanticId owner, SemanticImplementationRole role, string? member) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{owner}|{role}|{member?.Length ?? 0}:{member}"))).ToLowerInvariant();
}
