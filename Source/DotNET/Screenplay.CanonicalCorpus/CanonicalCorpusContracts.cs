// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Screenplay.CanonicalCorpus;

/// <summary>
/// Represents one immutable source document in a canonical corpus form.
/// </summary>
public sealed record CanonicalCorpusDocument
{
    static readonly UTF8Encoding _strictUtf8 = new(false, true);

    /// <summary>
    /// Gets the stable non-path document key.
    /// </summary>
    public required string StableKey { get; init; }

    /// <summary>
    /// Gets the portable display path.
    /// </summary>
    public required string DisplayPath { get; init; }

    /// <summary>
    /// Gets the exact UTF-8 source bytes.
    /// </summary>
    public required ImmutableArray<byte> Bytes { get; init; }

    /// <summary>
    /// Gets the strictly decoded source text.
    /// </summary>
    public string Text => _strictUtf8.GetString(Bytes.AsSpan());
}

/// <summary>
/// Represents one physical source form of a canonical corpus.
/// </summary>
public sealed record CanonicalCorpusSourceForm
{
    /// <summary>
    /// Gets the stable source-form name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets source documents in this form's declared input order.
    /// </summary>
    public ImmutableArray<CanonicalCorpusDocument> Documents { get; init; } = [];

    /// <summary>
    /// Gets the canonical identity catalog for this physical source form.
    /// </summary>
    public ImmutableArray<byte> IdentityCatalogBytes { get; init; } = [];
}

/// <summary>
/// Represents one expected normalized specification outcome in a canonical corpus.
/// </summary>
public sealed record CanonicalCorpusSpecificationExpectation
{
    /// <summary>
    /// Gets the exact specification semantic identity.
    /// </summary>
    public required SemanticId Specification { get; init; }

    /// <summary>
    /// Gets the specification display name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the expected normalized execution outcome.
    /// </summary>
    public required SemanticExecutionOutcomeKind Outcome { get; init; }

    /// <summary>
    /// Gets the expected rejection message, or <see langword="null"/> for an accepted scenario.
    /// </summary>
    public string? RejectionMessage { get; init; }

    /// <summary>
    /// Gets whether the authored assertions match. Unsupported execution can never pass.
    /// </summary>
    public bool Passed { get; init; } = true;

    /// <summary>
    /// Gets the expected typed rejection category, when specified.
    /// </summary>
    public SemanticRejectionCategory? RejectionCategory { get; init; }

    /// <summary>
    /// Gets the expected missing capability, when specified.
    /// </summary>
    public SemanticExecutionCapability? UnsupportedCapability { get; init; }

    /// <summary>
    /// Gets the expected resulting fact-world size, when specified.
    /// </summary>
    public int? WorldFactCount { get; init; }

    /// <summary>
    /// Gets the expected response as canonical JSON text, or <see langword="null"/> when no response exists.
    /// Record fields retain response contract order; values use the canonical semantic value encoding.
    /// </summary>
    public string? Response { get; init; }

    /// <summary>
    /// Gets route JSON text in produced-fact order.
    /// The JSON text <c>"null"</c> means unrouted; no defaults are materialized.
    /// </summary>
    public ImmutableArray<string> Routes { get; init; } = [];
}

/// <summary>
/// Represents a diagnostic expected from a source that cannot be bound to portable ESM.
/// </summary>
public sealed record CanonicalCorpusDiagnosticExpectation
{
    /// <summary>
    /// Gets the stable diagnostic code.
    /// </summary>
    public required string Code { get; init; }

    /// <summary>
    /// Gets the exact diagnostic message.
    /// </summary>
    public required string Message { get; init; }
}

/// <summary>
/// Represents a rejected corpus source with no publishable artifacts.
/// </summary>
public sealed record CanonicalCorpusRejectionVector
{
    /// <summary>
    /// Gets the corpus identity.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the application name.
    /// </summary>
    public required string ApplicationName { get; init; }

    /// <summary>
    /// Gets the fixed application identity.
    /// </summary>
    public required ApplicationIdentity ApplicationIdentity { get; init; }

    /// <summary>
    /// Gets the rejected physical source form.
    /// </summary>
    public required CanonicalCorpusSourceForm SourceForm { get; init; }

    /// <summary>
    /// Gets the expected binding diagnostics in emission order.
    /// </summary>
    public ImmutableArray<CanonicalCorpusDiagnosticExpectation> Diagnostics { get; init; } = [];

    /// <summary>
    /// Gets the expected publishable artifact paths, empty when compilation fails closed.
    /// </summary>
    public ImmutableArray<string> ArtifactPaths { get; init; } = [];
}

/// <summary>
/// Represents one versioned canonical semantic conformance corpus.
/// </summary>
public sealed record CanonicalScreenBehaviorProbe
{
    /// <summary>
    /// Gets the stable probe name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the user-visible behavior the probe covers.
    /// </summary>
    public required string Behavior { get; init; }

    /// <summary>
    /// Gets the source form that contains the authoring surface.
    /// </summary>
    public required string SourceForm { get; init; }

    /// <summary>
    /// Gets the authored screen or UI declaration the probe is pinned to.
    /// </summary>
    public required string Declaration { get; init; }

    /// <summary>
    /// Gets the syntax node kinds that must remain present for the behavior to be renderable.
    /// </summary>
    public ImmutableArray<string> SyntaxKinds { get; init; } = [];
}

/// <summary>
/// Represents one positive typed-screen authoring source case.
/// </summary>
public sealed record CanonicalTypedScreenSourceCase
{
    /// <summary>
    /// Gets the stable case name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the positive source document.
    /// </summary>
    public required CanonicalCorpusDocument Document { get; init; }

    /// <summary>
    /// Gets the branch or feature set required before this source parses in this repository.
    /// </summary>
    public required string Requires { get; init; }

    /// <summary>
    /// Gets why the case is pending in the current released compiler, or <c>null</c> when it runs here.
    /// </summary>
    public string? PendingReason { get; init; }

    /// <summary>
    /// Gets the typed syntax node kinds expected once the required authoring syntax is available.
    /// </summary>
    public ImmutableArray<string> ExpectedSyntaxKinds { get; init; } = [];
}

/// <summary>
/// Represents an executable assertion made by a screen behavior harness.
/// </summary>
public sealed record CanonicalScreenAssertion
{
    /// <summary>
    /// Gets the subject path inside the harness output.
    /// </summary>
    public required string Path { get; init; }

    /// <summary>
    /// Gets the comparison operation.
    /// </summary>
    public required string Operation { get; init; }

    /// <summary>
    /// Gets the expected value.
    /// </summary>
    public required string Value { get; init; }
}

/// <summary>
/// Represents one expected behavior for a screen corpus harness.
/// </summary>
public sealed record CanonicalScreenBehaviorExpectation
{
    /// <summary>
    /// Gets the stable expectation name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the harness category.
    /// </summary>
    public required string Category { get; init; }

    /// <summary>
    /// Gets the positive source case the behavior belongs to.
    /// </summary>
    public required string SourceCase { get; init; }

    /// <summary>
    /// Gets the precondition under test.
    /// </summary>
    public required string Given { get; init; }

    /// <summary>
    /// Gets the user or tool action under test.
    /// </summary>
    public required string When { get; init; }

    /// <summary>
    /// Gets the expected observable outcome.
    /// </summary>
    public required string Then { get; init; }

    /// <summary>
    /// Gets machine-checkable assertions for the behavior.
    /// </summary>
    public ImmutableArray<CanonicalScreenAssertion> Assertions { get; init; } = [];
}

/// <summary>
/// Represents one MCP authoring edit invariant for the screen corpus.
/// </summary>
public sealed record CanonicalMcpEditExpectation
{
    /// <summary>
    /// Gets the stable edit expectation name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the positive source case the edit applies to.
    /// </summary>
    public required string SourceCase { get; init; }

    /// <summary>
    /// Gets the authored operation under test.
    /// </summary>
    public required string Operation { get; init; }

    /// <summary>
    /// Gets the declaration the edit targets.
    /// </summary>
    public required string TargetDeclaration { get; init; }

    /// <summary>
    /// Gets invariants that must survive the edit.
    /// </summary>
    public ImmutableArray<CanonicalScreenAssertion> Assertions { get; init; } = [];
}

/// <summary>
/// Represents one pending or executable harness entry point for released screen parity checks.
/// </summary>
public sealed record CanonicalScreenHarnessExpectation
{
    /// <summary>
    /// Gets the stable harness name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the host that executes the harness.
    /// </summary>
    public required string Host { get; init; }

    /// <summary>
    /// Gets the command, endpoint, or test entry point.
    /// </summary>
    public required string EntryPoint { get; init; }

    /// <summary>
    /// Gets the exact dependency vector required for a non-pending run.
    /// </summary>
    public required string RequiredVersionVector { get; init; }

    /// <summary>
    /// Gets why the harness is pending, or <c>null</c> when the current repository runs it.
    /// </summary>
    public string? PendingReason { get; init; }

    /// <summary>
    /// Gets machine-checkable assertions the harness must evaluate.
    /// </summary>
    public ImmutableArray<CanonicalScreenAssertion> Assertions { get; init; } = [];
}

/// <summary>
/// Represents one canonical Stage artifact expectation.
/// </summary>
public sealed record CanonicalStageArtifactExpectation
{
    /// <summary>
    /// Gets the portable artifact path.
    /// </summary>
    public required string Path { get; init; }

    /// <summary>
    /// Gets the expected SHA-256 hash encoded as lowercase hexadecimal.
    /// </summary>
    public required string Sha256 { get; init; }

    /// <summary>
    /// Gets the expected byte count.
    /// </summary>
    public required int ByteCount { get; init; }
}

/// <summary>
/// Represents a Stage plan expectation for one profile and renderer target.
/// </summary>
public sealed record CanonicalStagePlanExpectation
{
    /// <summary>
    /// Gets the renderer target name.
    /// </summary>
    public required string Target { get; init; }

    /// <summary>
    /// Gets the UI profile name.
    /// </summary>
    public required string Profile { get; init; }

    /// <summary>
    /// Gets the exact plan digest, or a <c>pending:</c> marker until the released planner schema is available.
    /// </summary>
    public required string PlanDigest { get; init; }

    /// <summary>
    /// Gets the exact dependency vector required before this plan can assert artifact bytes.
    /// </summary>
    public string? RequiredVersionVector { get; init; }

    /// <summary>
    /// Gets why the plan is pending, or <c>null</c> when <see cref="PlanDigest"/> names exact released bytes.
    /// </summary>
    public string? PendingReason { get; init; }

    /// <summary>
    /// Gets the ordered artifact expectations for the plan.
    /// </summary>
    public ImmutableArray<CanonicalStageArtifactExpectation> Artifacts { get; init; } = [];

    /// <summary>
    /// Gets machine-checkable plan assertions that do not depend on final artifact bytes.
    /// </summary>
    public ImmutableArray<CanonicalScreenAssertion> Assertions { get; init; } = [];
}

/// <summary>
/// Represents a canonical screen application corpus with executable ESM and render probes.
/// </summary>
public sealed record CanonicalScreenCorpusVector
{
    /// <summary>
    /// Gets the corpus identity.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the application name.
    /// </summary>
    public required string ApplicationName { get; init; }

    /// <summary>
    /// Gets the fixed application identity.
    /// </summary>
    public required ApplicationIdentity ApplicationIdentity { get; init; }

    /// <summary>
    /// Gets every equivalent physical source form.
    /// </summary>
    public ImmutableArray<CanonicalCorpusSourceForm> SourceForms { get; init; } = [];

    /// <summary>
    /// Gets the expected executable-semantic-model refusal diagnostics while screens are authoring/rendering scope.
    /// </summary>
    public ImmutableArray<CanonicalCorpusDiagnosticExpectation> EsmDiagnostics { get; init; } = [];

    /// <summary>
    /// Gets the behavior probes expected from authoring and rendering hosts.
    /// </summary>
    public ImmutableArray<CanonicalScreenBehaviorProbe> BehaviorProbes { get; init; } = [];

    /// <summary>
    /// Gets positive typed-screen source cases that become executable when their required authoring syntax is available.
    /// </summary>
    public ImmutableArray<CanonicalTypedScreenSourceCase> TypedSourceCases { get; init; } = [];

    /// <summary>
    /// Gets behavior expectations that browser, CLI, Studio and Stage harnesses must assert.
    /// </summary>
    public ImmutableArray<CanonicalScreenBehaviorExpectation> BehaviorExpectations { get; init; } = [];

    /// <summary>
    /// Gets MCP edit invariants for authoring tools.
    /// </summary>
    public ImmutableArray<CanonicalMcpEditExpectation> McpEditExpectations { get; init; } = [];

    /// <summary>
    /// Gets released-version harness entry points.
    /// </summary>
    public ImmutableArray<CanonicalScreenHarnessExpectation> Harnesses { get; init; } = [];

    /// <summary>
    /// Gets Stage plan expectations when released renderer packages provide deterministic artifact bytes.
    /// </summary>
    public ImmutableArray<CanonicalStagePlanExpectation> StagePlans { get; init; } = [];
}

/// <summary>
/// Represents one versioned canonical semantic conformance corpus.
/// </summary>
public sealed record CanonicalCorpusVector
{
    /// <summary>
    /// Gets the corpus identity.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the application display name and stable key.
    /// </summary>
    public required string ApplicationName { get; init; }

    /// <summary>
    /// Gets the persisted application identity.
    /// </summary>
    public required ApplicationIdentity ApplicationIdentity { get; init; }

    /// <summary>
    /// Gets the fixed runtime stream identity used by specifications.
    /// </summary>
    public required string RuntimeStreamId { get; init; }

    /// <summary>
    /// Gets every equivalent physical source form.
    /// </summary>
    public ImmutableArray<CanonicalCorpusSourceForm> SourceForms { get; init; } = [];

    /// <summary>
    /// Gets expected normalized specification outcomes in semantic identity order.
    /// </summary>
    public ImmutableArray<CanonicalCorpusSpecificationExpectation> SpecificationExpectations { get; init; } = [];

    /// <summary>
    /// Gets canonical ESM bytes.
    /// </summary>
    public ImmutableArray<byte> EsmBytes { get; init; } = [];

    /// <summary>
    /// Gets the expected semantic revision.
    /// </summary>
    public SemanticRevision SemanticRevision { get; init; }
}
