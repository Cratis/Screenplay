// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Comparison;

internal static partial class StructuralComparison
{
    internal sealed record DocumentLocation(string DocumentId, string Path);

    internal sealed record Change(string Section, string ChangeKind, string? SemanticId, string Kind, string? BeforeAddress, string? AfterAddress, string? Member = null,
        string? BeforeHash = null, string? AfterHash = null, string? BeforeType = null, string? AfterType = null, bool? ContractBreaking = null, bool? GenerationCovered = null,
        uint? BeforeGeneration = null, uint? AfterGeneration = null, DocumentLocation[]? BeforeDocuments = null, DocumentLocation[]? AfterDocuments = null,
        string? Snapshot = null, string? DependantAddress = null, string? Role = null, string? Resolution = null, string? EventContractId = null,
        string? MoveKind = null, string? BeforeOwner = null, string? AfterOwner = null);
}
