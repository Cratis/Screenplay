// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Comparison;

internal static partial class StructuralComparison
{
    static bool? SemanticChange(Change[] changes, bool complete)
    {
        if (changes.Any(change => _structuralSections.Contains(change.Section, StringComparer.Ordinal) || (change.Section == "declarations" && (change.ChangeKind != "moved" || change.MoveKind == "owner")))) return true;
        return complete ? false : null;
    }

    static string Presence(object? before, object? after) => (before, after) switch
    {
        (null, _) => "added",
        (_, null) => "removed",
        _ => "changed"
    };

    static string IdentityChange(object? before, object? after) => (before, after) switch
    {
        (null, _) => "assigned",
        (_, null) => "retired",
        _ => "migrated"
    };

    static string Kind(SemanticAddress address) => address.Kind switch { SemanticKind.EventContract => "Event", SemanticKind.CompositeType => "Type", _ => address.Kind.ToString() };

    static string? Hash(string? value) => value is null ? null : Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    static bool AddressesEqual(SemanticAddress before, SemanticAddress after) => before.Kind == after.Kind && before.Parts.Where(part => part.Kind != SemanticAddressPartKind.Application).SequenceEqual(after.Parts.Where(part => part.Kind != SemanticAddressPartKind.Application));

    static bool SameDeclarationLocation(SemanticAddress before, SemanticAddress after) => before.Kind == after.Kind && before.Parts.Where(part => part.Kind is not (SemanticAddressPartKind.Application or SemanticAddressPartKind.Generation)).SequenceEqual(after.Parts.Where(part => part.Kind is not (SemanticAddressPartKind.Application or SemanticAddressPartKind.Generation)));

    static string AddressKey(SemanticAddress address) => $"{Kind(address)}:{JsonSerializer.Serialize(address.Parts.Where(part => part.Kind != SemanticAddressPartKind.Application))}";

    static string Owner(SemanticAddress address) => string.Join('.', address.Parts.SkipLast(1).Where(part => part.Kind is not (SemanticAddressPartKind.Application or SemanticAddressPartKind.OwnerKind or SemanticAddressPartKind.Generation)).Select(part => part.Key));

    static string Address(SemanticAddress address) => string.Join('.', address.Parts.Where(part => part.Kind is not (SemanticAddressPartKind.Application or SemanticAddressPartKind.OwnerKind or SemanticAddressPartKind.Generation)).Select(part => part.Key));

    static void IdentityContracts(Snapshot before, Snapshot after, List<Change> changes)
    {
        var left = before.Workspace.IdentityCatalog.EventContracts.ToDictionary(assignment => assignment.Id.ToString(), StringComparer.Ordinal);
        var right = after.Workspace.IdentityCatalog.EventContracts.ToDictionary(assignment => assignment.Id.ToString(), StringComparer.Ordinal);
        foreach (var id in left.Keys.Union(right.Keys).Order(StringComparer.Ordinal))
        {
            left.TryGetValue(id, out var old);
            right.TryGetValue(id, out var current);
            if (old is null || current is null || !old.Address.Equals(current.Address))
            {
                changes.Add(new("identities", IdentityChange(old, current), null, "Event", old is null ? null : Address(old.Address), current is null ? null : Address(current.Address), EventContractId: id));
            }
        }
    }
}
