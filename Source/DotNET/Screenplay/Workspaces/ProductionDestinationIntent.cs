// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces;

/// <summary>
/// Complete author-selected destinations for one command; not an executable preservation proof.
/// </summary>
/// <param name="Version">The intent contract version, currently 1.</param>
/// <param name="Productions">Exactly one selection per event-production occurrence.</param>
public sealed record ProductionDestinationIntent(int Version, ImmutableArray<ProductionDestinationSelection> Productions);

/// <summary>
/// Confirms an occurrence's identity destination and existing named routing separately.
/// </summary>
/// <param name="Subject">The original production occurrence.</param>
/// <param name="For">The explicitly selected identity expression.</param>
/// <param name="Route">Confirmation of existing effective routing.</param>
public sealed record ProductionDestinationSelection(WorkspaceNodeHandle Subject, ExpressionSyntax For, ProductionDestinationRoute Route);

/// <summary>
/// Confirms legacy routing or uniquely resolved source and stream declaration occurrences.
/// </summary>
/// <param name="Kind">Legacy or Named.</param>
/// <param name="Source">The source declaration for Named routing only.</param>
/// <param name="Stream">The source-owned stream declaration for Named routing only.</param>
public sealed record ProductionDestinationRoute(string Kind, WorkspaceNodeHandle? Source = null, WorkspaceNodeHandle? Stream = null);

/// <summary>
/// One discovery row. Page all rows to collect the complete command selection set.
/// </summary>
/// <param name="Command">The containing command occurrence.</param>
/// <param name="Diagnostics">The actual PLAY0470 occurrences on that command.</param>
/// <param name="Subject">The event-production occurrence, distinct even for repeated events.</param>
/// <param name="Event">The authored event reference.</param>
/// <param name="Condition">The unchanged production condition.</param>
/// <param name="ExistingFor">The existing explicit expression, if any.</param>
/// <param name="AuthoredRoute">The per-production route override, if any.</param>
/// <param name="EffectiveRoute">The inherited or explicit route syntax, if any.</param>
/// <param name="Route">The uniquely resolved confirmation, or null when ambiguous.</param>
public sealed record ProductionDestinationDescriptor(
    WorkspaceNodeHandle Command, ImmutableArray<Diagnostic> Diagnostics, WorkspaceNodeHandle Subject,
    string Event, ConditionSyntax? Condition, ExpressionSyntax? ExistingFor,
    CommandStreamSyntax? AuthoredRoute, CommandStreamSyntax? EffectiveRoute, ProductionDestinationRoute? Route);

/// <summary>
/// A retained routing row: syntax edits and effective routing established from invalid source are separate facts.
/// </summary>
/// <param name="Original">The original occurrence and authored evidence.</param>
/// <param name="Selection">The author's exact submitted selection.</param>
/// <param name="Omission">Syntax-only omission category; never a guessed before-destination.</param>
/// <param name="Classification">MadeExplicit or ExplicitUnchanged.</param>
/// <param name="EffectiveClassification">EstablishedFromInvalidModel.</param>
/// <param name="Property">The bound command property supplying the identity.</param>
/// <param name="Candidate">The candidate event production, including effective named route mappings.</param>
public sealed record ProductionDestinationRow(
    ProductionDestinationDescriptor Original, ProductionDestinationSelection Selection, string? Omission,
    string Classification, string EffectiveClassification, SemanticProperty Property, SemanticProducedEvent Candidate);

/// <summary>
/// Retained candidate admission evidence. It proves no baseline behavior preservation or target execution support.
/// </summary>
/// <param name="Intent">The complete submitted intent.</param>
/// <param name="LanguageVersion">The admitted candidate language version.</param>
/// <param name="SemanticVersion">The admitted candidate semantic version.</param>
/// <param name="Command">The candidate command with its default destination and route.</param>
/// <param name="Rows">Routing evidence in authored order.</param>
/// <param name="NewIdentities">New assignments enrolled by the authoring transaction.</param>
public sealed record ProductionDestinationReport(
    ProductionDestinationIntent Intent, LanguageVersion LanguageVersion, SemanticVersion SemanticVersion, SemanticCommand Command,
    ImmutableArray<ProductionDestinationRow> Rows, ImmutableArray<string> NewIdentities)
{
    /// <summary>
    /// Gets whether an executable baseline exists. This repair does not have one.
    /// </summary>
    public bool BaselineExecutableAvailable => false;

    /// <summary>
    /// Gets whether executable preservation was proved. It was not.
    /// </summary>
    public bool PreservationProven => false;

    /// <summary>
    /// Gets the whole-workspace binding result required for retention.
    /// </summary>
    public bool BindingSucceeded => true;

    /// <summary>
    /// Gets the scope of the non-subject routing claim: authored syntax only.
    /// </summary>
    public string OtherCommands => "Authored routing unchanged; executable preservation not proven";
}
