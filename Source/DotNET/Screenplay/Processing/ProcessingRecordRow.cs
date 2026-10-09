// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Processing;

/// <summary>
/// Declared processing facts and bounded derivations for one purpose.
/// </summary>
/// <param name="Purpose">The declared purpose name.</param>
/// <param name="Description">The processing description.</param>
/// <param name="Basis">The Art. 6(1) basis.</param>
/// <param name="BasisReference">The accompanying reference.</param>
/// <param name="Interest">The legitimate-interest statement.</param>
/// <param name="Condition">The Art. 9(2) condition.</param>
/// <param name="ConditionReference">The condition reference.</param>
/// <param name="Authorization">The Art. 10 authorization.</param>
/// <param name="Subjects">The declared categories of data subjects.</param>
/// <param name="Recipients">The recipient categories.</param>
/// <param name="Transfers">Transfers and their safeguards.</param>
/// <param name="Retention">The declared retention period or criteria.</param>
/// <param name="ErasureException">The Art. 17(3) exception.</param>
/// <param name="Categories">The reachable personal-data concept names.</param>
/// <param name="SpecialCategories">The reachable Art. 9(1) categories.</param>
/// <param name="CriminalData">Whether criminal-offence data is reachable.</param>
/// <param name="SecurityMeasures">Protection mappings declared by value markers, not verified runtime controls.</param>
/// <param name="DpiaPrompt">The assessment prompt, without inferring scale or a legal verdict.</param>
/// <param name="Findings">Declared retention/erasure conflicts.</param>
/// <param name="CoveredSlices">Fully qualified covered slice addresses.</param>
public sealed record ProcessingRecordRow(
    string Purpose,
    string? Description,
    string? Basis,
    string? BasisReference,
    string? Interest,
    string? Condition,
    string? ConditionReference,
    string? Authorization,
    IReadOnlyList<string> Subjects,
    IReadOnlyList<string> Recipients,
    IReadOnlyList<ProcessingTransfer> Transfers,
    string? Retention,
    string? ErasureException,
    IReadOnlyList<string> Categories,
    IReadOnlyList<string> SpecialCategories,
    bool CriminalData,
    IReadOnlyList<string> SecurityMeasures,
    string? DpiaPrompt,
    IReadOnlyList<string> Findings,
    IReadOnlyList<string> CoveredSlices);
