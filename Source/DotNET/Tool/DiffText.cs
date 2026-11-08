// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Comparison;

namespace Cratis.Screenplay.Tool;

// The human form of the diff report: one line per change, grouped as the library groups them.
static class DiffText
{
    internal static void Write(DiffReport report, TextWriter output)
    {
        var difference = report.Difference;
        output.WriteLine($"Comparing {report.Before.Path} -> {report.After.Path}, matched by {(difference.Matching == DeclarationMatching.Identity ? "persisted identity" : "address")}.");
        output.WriteLine($"Declarations: {difference.BeforeDeclarations} before, {difference.AfterDeclarations} after.");
        Section(output, "Declarations", difference.Declarations, Declaration);
        Section(output, "Members", difference.Members, change => $"{change.Change} {Name(change.Declaration)} {change.Member}{Breaking(change.ContractBreaking)}");
        Section(output, "Event contracts", difference.Events, Event);
        Section(output, "Enumerations", difference.Enumerations, change => $"{change.Change} {Name(change.Declaration)} {change.Member}{(change.AfterMember is null ? string.Empty : " -> " + change.AfterMember)}{Breaking(change.ContractBreaking)}");
        Section(output, "Specifications", difference.Specifications, change => $"{change.Change} {Name(change.Declaration)}{(change.Member is null ? string.Empty : " " + change.Member)}");
        Section(output, "Identities", difference.Identities, change => $"{change.Change} {Name(change.Declaration)}");
        Section(output, "Direct dependants", difference.Dependants, dependant => $"{dependant.DependantAddress} ({dependant.Role}) depends on {dependant.Changed.Kind} {Address(dependant.Changed)}");
        Section(output, "Slices", difference.Slices.Slices, Slice);
        Section(output, "Model-wide", difference.Slices.ModelWide, Name);
        Section(output, "Affected slices", difference.Slices.AffectedSlices, slice => slice);
        foreach (var section in difference.Sections.Where(section => !section.Complete))
        {
            foreach (var gap in section.Gaps) output.WriteLine($"Incomplete {section.Section}: {gap.Statement}");
        }

        foreach (var item in difference.NotCompared) output.WriteLine($"Not compared: {item}");
        output.WriteLine(difference.HasSemanticChange switch
        {
            true => "Semantic change: yes.",
            false => "Semantic change: none.",
            null => "Semantic change: unknown; the comparison is incomplete."
        });
    }

    static void Section<T>(TextWriter output, string title, IReadOnlyList<T> changes, Func<T, string> line)
    {
        if (changes.Count == 0) return;
        output.WriteLine($"{title} ({changes.Count}):");
        foreach (var change in changes) output.WriteLine($"  {line(change)}");
    }

    static string Declaration(DeclarationChange change) => change.Change switch
    {
        DeclarationChangeKind.Renamed => $"Renamed {change.Declaration.Kind} {change.Declaration.BeforeAddress} -> {change.Declaration.AfterAddress}",
        DeclarationChangeKind.Moved => $"Moved {change.Declaration.Kind} {Address(change.Declaration)} ({change.Move?.ToString().ToLowerInvariant() ?? "owner"}) {Location(change)}",
        _ => $"{change.Change} {Name(change.Declaration)}"
    };

    static string Slice(SliceChange slice)
    {
        var address = slice.BeforeAddress is not null && slice.AfterAddress is not null && slice.BeforeAddress != slice.AfterAddress
            ? $"{slice.BeforeAddress} -> {slice.AfterAddress}"
            : slice.Address;
        var dependents = slice.DependentSlices.Count == 0 ? string.Empty : $"; dependents: {string.Join(", ", slice.DependentSlices)}";
        return $"{string.Join(", ", slice.Changes)} {address} ({slice.Declarations.Count} declarations){dependents}";
    }

    static string Location(DeclarationChange change) => change.Move == DeclarationMove.Document
        ? $"{string.Join(", ", change.BeforeDocuments.Select(document => document.Path))} -> {string.Join(", ", change.AfterDocuments.Select(document => document.Path))}"
        : $"{change.BeforeOwner} -> {change.AfterOwner}";

    static string Event(EventContractChange change)
    {
        var types = change.BeforeType is null && change.AfterType is null ? string.Empty : $" {change.BeforeType ?? "-"} -> {change.AfterType ?? "-"}";
        return $"{change.Change} {Name(change.Declaration)}{(change.Property is null ? string.Empty : "." + change.Property)}{types}{Breaking(change.ContractBreaking)}";
    }

    static string Name(ComparedDeclaration declaration) => $"{declaration.Kind} {Address(declaration)}";

    static string Address(ComparedDeclaration declaration) => declaration.AfterAddress ?? declaration.BeforeAddress ?? "<unaddressed>";

    static string Breaking(bool contractBreaking) => contractBreaking ? " [contract-breaking]" : string.Empty;
}
