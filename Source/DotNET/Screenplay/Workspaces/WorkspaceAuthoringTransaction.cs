// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Files;
using Cratis.Screenplay.Parsing;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Workspaces;

sealed class WorkspaceAuthoringTransaction(
    ScreenplayWorkspace workspace,
    IReadOnlyDictionary<SemanticAddress, SemanticAddress>? referenceRenames = null,
    IReadOnlySet<DocumentId>? shapePreservingReplacements = null,
    IReadOnlyDictionary<(DocumentId Document, string Path), (DocumentId Document, string Path)>? movedOccurrences = null)
{
    readonly ImmutableArray<Diagnostic>.Builder _diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();

    internal static void RequireShape(WorkspaceSyntaxIndex before, WorkspaceSyntaxIndex after, DocumentId document)
    {
        var original = before.Entries.Where(entry => entry.Handle.Document == document).ToDictionary(entry => entry.Handle.Path, entry => entry.Kind, StringComparer.Ordinal);
        var candidate = after.Entries.Where(entry => entry.Handle.Document == document).ToDictionary(entry => entry.Handle.Path, entry => entry.Kind, StringComparer.Ordinal);
        if (original.Count != candidate.Count || original.Any(pair => candidate.GetValueOrDefault(pair.Key) != pair.Value))
        {
            throw new InvalidWorkspaceAuthoring("A generated rename rewrite changed the syntax shape of a document, so its absence-key correspondence cannot be proven.");
        }
    }

    internal WorkspaceAuthoringResult Propose(WorkspaceAuthoringRequest request)
    {
        if (request is null)
        {
            return Failure(WorkspaceConflictKind.InvalidOperation, "An authoring request is required.");
        }

        if (request.ExpectedRevision != workspace.Revision)
        {
            return Failure(WorkspaceConflictKind.StaleWorkspaceRevision, "The authoring request has a stale workspace revision.");
        }

        if (request.ExpectedCatalogRevision != workspace.IdentityCatalog.Revision)
        {
            return Failure(WorkspaceConflictKind.StaleCatalogRevision, "The authoring request has a stale identity catalog revision.");
        }

        if (!Enum.IsDefined(request.Validation) || !Enum.IsDefined(request.Formatting) || !Enum.IsDefined(request.ReferencePolicy) ||
            request.Operations.IsDefault || request.Documents.IsDefault || request.SemanticRenames.IsDefault ||
            request.EventRenames.IsDefault || request.RetiredSemanticAddresses.IsDefault || request.RetiredEventAddresses.IsDefault)
        {
            return Failure(WorkspaceConflictKind.InvalidOperation, "Authoring options must be known and request arrays must be non-default.");
        }

        if (request.SemanticRenames.Any(rename => rename is null || rename.PreviousAddress is null || rename.CurrentAddress is null) ||
            request.EventRenames.Any(rename => rename is null || rename.PreviousAddress is null || rename.CurrentAddress is null) ||
            request.RetiredSemanticAddresses.Any(address => address is null) || request.RetiredEventAddresses.Any(address => address is null))
        {
            return Failure(WorkspaceConflictKind.InvalidIdentityMigration, "Identity migration endpoints and retirement addresses must be present.");
        }

        try
        {
            return ProposeCore(request);
        }
        catch (InvalidSemanticContract exception)
        {
            return Failure(WorkspaceConflictKind.InvalidIdentityMigration, exception.Message, exception.IdentityMigrationIssues);
        }
        catch (Exception exception) when (exception is InvalidWorkspaceAuthoring or InvalidScreenplayWorkspace or InvalidWorkspaceDocument or InvalidPortablePlayPath or JsonException or ArgumentException or InvalidOperationException ||
            exception.GetType().Namespace == "Cratis.Screenplay.Syntax.Serialization")
        {
            return Failure(WorkspaceConflictKind.InvalidOperation, exception.Message);
        }
    }

    // A typed replacement of a keyed query's 'by' argument that changes only its name renames that one argument
    // occurrence. When the query keys a read model named by an absence assertion, this is the absence identifier
    // repair; supply its address migration so the argument's own references keep their correspondence.
    static IReadOnlyDictionary<SemanticAddress, SemanticAddress>? IdentifierMigrations(WorkspaceSyntaxIndex index, WorkspaceAstEdits edits, IReadOnlyDictionary<SemanticAddress, SemanticAddress>? referenceRenames)
    {
        var views = index.Entries.Select(entry => entry.Node).OfType<SpecificationAbsentReadModelSyntax>().Select(assertion => assertion.Name.Split('.')[^1]).ToHashSet(StringComparer.Ordinal);
        var migrations = new Dictionary<SemanticAddress, SemanticAddress>();
        foreach (var (target, value) in edits.Replacements)
        {
            if (string.Equals(target.Member, "by", StringComparison.Ordinal) && target is { Address: { } previous, Node: QueryParameterSyntax original } && value is QueryParameterSyntax replacement &&
                original.Name != replacement.Name && SyntaxJson.StructurallyEqual(original with { Name = replacement.Name }, replacement) &&
                target.Parent is { } parent && index.Find(parent) is { Node: QuerySyntax query, Address: { } owner } &&
                views.Contains(query.ReturnType.Name.Split('.')[^1]))
            {
                migrations[previous] = SemanticAddress.ForQueryArgument(owner, replacement.Name);
            }
        }

        if (migrations.Count == 0)
        {
            return referenceRenames;
        }

        foreach (var (previous, current) in referenceRenames ?? new Dictionary<SemanticAddress, SemanticAddress>())
        {
            migrations[previous] = current;
        }

        return migrations;
    }

    WorkspaceAuthoringResult ProposeCore(WorkspaceAuthoringRequest request)
    {
        var index = WorkspaceSyntaxIndex.Create(workspace);
        var edits = new WorkspaceAstEdits(index, request.RelocatesCompositionComments);
        if (request.Operations.OfType<MigrateOptionalTypeSpelling>().Any(operation => operation.Target is null || operation.Expected is null))
        {
            throw new InvalidWorkspaceAuthoring("Optionality migrations require a target and expected type.");
        }

        if (request.Operations.OfType<MigrateComplianceMarkerSpelling>().Any(operation => operation.Target is null || operation.Expected is null))
        {
            throw new InvalidWorkspaceAuthoring("Compliance migrations require a target and expected concept.");
        }

        var spellingMigrations = request.Operations.OfType<MigrateOptionalTypeSpelling>().GroupBy(operation => operation.Target.Document).ToArray();
        var complianceMigrations = request.Operations.OfType<MigrateComplianceMarkerSpelling>().GroupBy(operation => operation.Target.Document).ToArray();
        edits.Prepare([.. request.Operations.Where(operation => operation is not (MigrateOptionalTypeSpelling or MigrateComplianceMarkerSpelling))]);
        var candidates = workspace.Documents.ToDictionary(document => document.Id);
        var documentRenames = ImmutableArray.CreateBuilder<DocumentIdentityRename>();
        var retiredDocuments = ImmutableArray.CreateBuilder<string>();
        var targeted = new HashSet<DocumentId>();
        var creations = new List<CreateWorkspaceSyntaxDocument>();
        var replacements = new List<ReplaceWorkspaceSyntaxDocument>();
        foreach (var operation in request.Documents)
        {
            if (operation is CreateWorkspaceSyntaxDocument create)
            {
                var document = WorkspaceDocument.Create(create.StableKey, create.Path, []);
                if (!targeted.Add(document.Id) || !candidates.TryAdd(document.Id, document))
                {
                    throw new InvalidWorkspaceAuthoring("A typed document creation duplicates an existing or targeted document identity.");
                }

                creations.Add(create);
                continue;
            }

            if (operation is ReplaceWorkspaceSyntaxDocument replace)
            {
                if (!candidates.ContainsKey(replace.Document) || !targeted.Add(replace.Document) || (edits.Touched.Contains(replace.Document) && !edits.OnlyPendingRuleRemovals(replace.Document)))
                {
                    throw new InvalidWorkspaceAuthoring("A typed replacement requires an existing document not targeted by another document operation or node edit.");
                }

                replacements.Add(replace);
                continue;
            }

            if (operation is not (MoveWorkspaceDocument or RenameWorkspaceDocument or RemoveWorkspaceDocument))
            {
                throw new InvalidWorkspaceAuthoring("Authoring admits typed document creation/replacement, move, rename, or removal only. Raw source replacements and text patches belong to strict Propose.");
            }

            if (operation is RemoveWorkspaceDocument remove && edits.Touched.Contains(remove.Document))
            {
                throw new InvalidWorkspaceAuthoring("A removed document cannot also own an AST source or destination handle.");
            }

            var conflict = WorkspaceTransactionOperations.Apply(operation, workspace, candidates, targeted, [], documentRenames, retiredDocuments);
            if (conflict is not null)
            {
                return new() { Conflicts = [conflict] };
            }
        }

        foreach (var spellings in spellingMigrations)
        {
            if (edits.Touched.Contains(spellings.Key) || targeted.Contains(spellings.Key) || !candidates.TryGetValue(spellings.Key, out var document))
            {
                throw new InvalidWorkspaceAuthoring("A spelling migration requires an existing document not targeted by another edit.");
            }

            candidates[spellings.Key] = WorkspaceOptionalityRepairs.Print(index, document, [.. spellings], request.Formatting, _diagnostics);
        }

        foreach (var spellings in complianceMigrations)
        {
            if (edits.Touched.Contains(spellings.Key) || targeted.Contains(spellings.Key) || spellingMigrations.Any(group => group.Key == spellings.Key) || !candidates.TryGetValue(spellings.Key, out var document))
            {
                throw new InvalidWorkspaceAuthoring("A compliance migration requires an existing document not targeted by another edit.");
            }

            candidates[spellings.Key] = WorkspaceComplianceRepairs.Print(index, document, [.. spellings], request.Formatting, _diagnostics);
        }

        // The logical move planner supplies exact occurrence lineage and proves all fragments and
        // identities survive. Positional rename checks cannot interpret a reparented header.
        if (movedOccurrences is null)
        {
            edits.ValidateFragmentRenames(replacements);
        }

        // Printing uses the original placement to identify authored tokens. Structural validation waits
        // until all source edits and document moves have settled the final import placements.
        var intendedDocuments = new Dictionary<DocumentId, ApplicationSyntax>();

        // Every handle, expectation, typed slot, overlap and original anchor has now been validated.
        foreach (var (id, syntax) in edits.Apply())
        {
            if (replacements.Exists(replacement => replacement.Document == id))
            {
                continue;
            }

            var document = candidates[id];
            intendedDocuments[id] = syntax;
            candidates[id] = WorkspaceAuthoringPrinter.Print(id, document.StableKey, document.Path, document.Encoding, syntax, request.Formatting, _diagnostics, document, index.Placement(workspace.Documents.Single(original => original.Id == id)), validatePlacement: false, candidates: index.StreamCandidates);
        }

        foreach (var replacement in replacements)
        {
            var document = candidates[replacement.Document];
            intendedDocuments[document.Id] = replacement.Syntax;
            candidates[document.Id] = WorkspaceAuthoringPrinter.Print(document.Id, document.StableKey, document.Path, document.Encoding, replacement.Syntax, request.Formatting, _diagnostics, document, index.Placement(workspace.Documents.Single(original => original.Id == document.Id)), validatePlacement: false, candidates: index.StreamCandidates);
        }

        foreach (var creation in creations)
        {
            var document = WorkspaceDocument.Create(creation.StableKey, creation.Path, []);
            intendedDocuments[document.Id] = creation.Syntax;
            candidates[document.Id] = WorkspaceAuthoringPrinter.Print(document.Id, document.StableKey, document.Path, creation.Encoding, creation.Syntax, request.Formatting, _diagnostics, validatePlacement: false);
        }

        var ordered = candidates.Values.OrderBy(document => document.Id.ToString(), StringComparer.Ordinal).ToImmutableArray();
        var collision = WorkspaceTransactionOperations.PortablePathCollision(ordered);
        if (collision is not null)
        {
            return new() { Conflicts = [collision], AuthoringDiagnostics = _diagnostics.ToImmutable() };
        }

        if (ordered.Select(document => document.StableKey).Distinct(StringComparer.Ordinal).Count() != ordered.Length)
        {
            throw new InvalidWorkspaceAuthoring("The final documents have duplicate stable keys.");
        }

        var compiler = new ScreenplayCompiler();
        var draftAuthoring = request.Validation == WorkspaceAuthoringValidation.Authoring && request.ReferencePolicy == WorkspaceAuthoringReferencePolicy.Draft;
        var texts = ordered.OrderBy(document => document.Path.Value, StringComparer.Ordinal).ToDictionary(document => document.Path.Value, document => document.Text, StringComparer.Ordinal);
        var (placed, placementDiagnostics) = PlayImports.Resolve(texts.Keys, new InMemoryPlayDocumentSource(texts), compiler.Languages);
        if (placed.Any(document => !document.IsPlacementResolved))
        {
            _diagnostics.AddRange(placementDiagnostics);
            return Failure(WorkspaceConflictKind.CompilationFailed, "UnresolvedPlacement: repair conflicting or cyclic imports in the final document set before committing syntax edits.");
        }

        var placements = placed.ToDictionary(document => document.Path, document => document.Placement, StringComparer.Ordinal);
        var streamCandidates = ((ICommandStreamCandidateParser)compiler).CaptureCandidates(placed.Select(document => (SourceLineSplitter.Split(document.Source, path: document.Path), document.Placement)));
        foreach (var document in ordered.Where(document => intendedDocuments.ContainsKey(document.Id)))
        {
            WorkspaceAuthoringPrinter.Validate(document.Text, document.Path, intendedDocuments[document.Id], placements[document.Path.Value], _diagnostics, request.Formatting, streamCandidates);
        }

        var (_, merged) = PlayApplicationAssembly.Compile(compiler, texts.Keys, new InMemoryPlayDocumentSource(texts), draftAuthoring);
        _diagnostics.AddRange(merged.Diagnostics);
        if (!merged.Success || merged.Value is null)
        {
            var failed = Failure(WorkspaceConflictKind.CompilationFailed, "The final document set is not valid full-language Screenplay source.");
            var ownership = WorkspaceTransactionOperations.OwnershipConflict(merged.Diagnostics, ordered);
            return ownership is null ? failed : failed with { Conflicts = [ownership] };
        }

        var catalog = WorkspaceAuthoringIdentity.Migrate(workspace, request, ordered, merged.Value, documentRenames.ToImmutable(), retiredDocuments.ToImmutable());

        // Original-source loader warnings are not evidence for different candidate sources.
        var attachments = request.AttachmentLoader?.Invoke(ordered) ?? new AttachmentFileResult
        {
            Contents = workspace.AttachmentContents,
            Diagnostics = []
        };
        var compilation = ordered.IsEmpty
            ? ScreenplayWorkspace.EmptyCompilation()
            : new SemanticModelCompiler().Compile(workspace.ApplicationName, ScreenplayWorkspace.CreateDocumentSet(ordered, catalog, attachments.Contents));
        compilation = compilation with { Diagnostics = [.. compilation.Diagnostics, .. attachments.Diagnostics] };
        if (request.Validation == WorkspaceAuthoringValidation.Executable && !compilation.Success)
        {
            return Failure(WorkspaceConflictKind.CompilationFailed, "The final source is authorable but is not executable by the semantic backend.") with
            {
                ExecutableDiagnostics = [.. compilation.Diagnostics]
            };
        }

        var candidate = ScreenplayWorkspace.CreateValidated(workspace.ApplicationName, ordered, catalog, compilation, attachments.Contents, attachments.Diagnostics);
        var migrations = IdentifierMigrations(index, edits, referenceRenames);

        // A logical move uses planner-owned object lineage, not address/ordinal guesses. The planner
        // checks every original reference against its exact candidate occurrence before returning it.
        if (movedOccurrences is null)
        {
            WorkspaceAuthoringReferences.Validate(workspace, candidate, request, _diagnostics, migrations);
        }
        ValidateSourceTransitions(request, index, candidate, edits, replacements, migrations);
        return new()
        {
            Workspace = candidate,
            WritePlan = new()
            {
                BeforeRevision = workspace.Revision,
                AfterRevision = candidate.Revision,
                BeforeCatalogRevision = workspace.IdentityCatalog.Revision,
                AfterCatalogRevision = catalog.Revision,
                Entries = WorkspaceTransactionOperations.WriteEntries(workspace.Documents, candidate.Documents)
            },
            AuthoringDiagnostics = _diagnostics.ToImmutable(),
            ExecutableReady = compilation.Success,
            ExecutableDiagnostics = [.. compilation.Diagnostics]
        };
    }

    // Source transition obligations are validated beside, not through, the generic reference engine.
    // Correspondence comes only from this transaction's operations; see WorkspaceEditProvenance.
    void ValidateSourceTransitions(
        WorkspaceAuthoringRequest request,
        WorkspaceSyntaxIndex before,
        ScreenplayWorkspace candidate,
        WorkspaceAstEdits edits,
        List<ReplaceWorkspaceSyntaxDocument> replacements,
        IReadOnlyDictionary<SemanticAddress, SemanticAddress>? renames)
    {
        var after = WorkspaceSyntaxIndex.Create(candidate);
        var provenance = new WorkspaceEditProvenance();
        var replaced = replacements.Select(replacement => replacement.Document).ToHashSet();
        var survivors = candidate.Documents.Select(document => document.Id).ToHashSet();
        foreach (var document in workspace.Documents.Where(document => survivors.Contains(document.Id) && !replaced.Contains(document.Id) && !edits.Touched.Contains(document.Id)))
        {
            provenance.Subtree(before, (document.Id, string.Empty), (document.Id, string.Empty));
        }

        var generatedReplacements = replaced.Where(document => shapePreservingReplacements?.Contains(document) == true).ToArray();
        foreach (var document in generatedReplacements)
        {
            RequireShape(before, after, document);
            provenance.Subtree(before, (document, string.Empty), (document, string.Empty));
        }

        edits.Record(provenance, after);
        foreach (var document in replaced.Where(document => shapePreservingReplacements?.Contains(document) != true))
        {
            provenance.Region(before, (document, string.Empty), after, (document, string.Empty));
        }

        var migrations = renames?.ToDictionary(pair => pair.Key, pair => pair.Value) ?? [];
        foreach (var rename in request.SemanticRenames)
        {
            migrations[rename.PreviousAddress] = rename.CurrentAddress;
        }

        var ruleSources = new WorkspaceEditProvenance();
        edits.RecordPendingRuleSources(ruleSources, replacements);

        // Only the trusted generated rewrites that passed RequireShape above carry positional
        // lineage. Ordinary document replacements must still prove pending-rule correspondence.
        foreach (var document in generatedReplacements)
        {
            ruleSources.Subtree(before, (document, string.Empty), (document, string.Empty));
        }

        // Move lineage comes from retained JSON objects, never a positional guess after reparenting.
        var previousOccurrences = movedOccurrences is null ? null : before.Entries.ToDictionary(entry => (entry.Handle.Document, entry.Handle.Path));
        var candidateOccurrences = movedOccurrences is null ? null : after.Entries.ToDictionary(entry => (entry.Handle.Document, entry.Handle.Path));
        foreach (var (original, current) in movedOccurrences ?? new Dictionary<(DocumentId Document, string Path), (DocumentId Document, string Path)>())
        {
            var previous = previousOccurrences![original];
            var candidateEntry = candidateOccurrences!.GetValueOrDefault(current);
            if (candidateEntry?.Kind != previous.Kind)
            {
                throw new InvalidWorkspaceAuthoring("A generated move changed an occurrence kind or lost its source correspondence.");
            }
            provenance.Map(current, original);
            ruleSources.Map(current, original);
        }

        var removals = edits.PendingRuleRemovals.Select(entry => entry.Handle).ToHashSet();
        var ruleRegions = edits.Replacements.Select(replacement => replacement.Target.Handle)
            .Concat(replaced.Select(document => new WorkspaceNodeHandle(workspace.Revision, document, string.Empty))).ToHashSet();
        WorkspacePendingRuleTransitions.Validate(before, after, ruleSources, provenance, migrations, removals, ruleRegions);

        if (WorkspaceAbsenceKeyBindings.Present(before) || WorkspaceAbsenceKeyBindings.Present(after))
        {
            WorkspaceAbsenceKeyValidation.Validate(before, after, WorkspaceReferenceLayout.Equivalent(workspace, candidate), provenance, request.ReferencePolicy, migrations, _diagnostics);
        }
    }

    WorkspaceAuthoringResult Failure(WorkspaceConflictKind kind, string message, ImmutableArray<IdentityMigrationIssue> identityMigrationIssues = default) => new()
    {
        Conflicts = [new WorkspaceConflict { Kind = kind, Message = message, IdentityMigrationIssues = identityMigrationIssues.IsDefault ? [] : identityMigrationIssues }],
        AuthoringDiagnostics = _diagnostics.ToImmutable()
    };
}
