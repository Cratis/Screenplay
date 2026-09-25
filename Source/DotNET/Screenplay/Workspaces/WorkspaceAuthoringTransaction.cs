// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Files;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces;

sealed class WorkspaceAuthoringTransaction(ScreenplayWorkspace workspace, IReadOnlyDictionary<SemanticAddress, SemanticAddress>? referenceRenames = null)
{
    readonly ImmutableArray<Diagnostic>.Builder _diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();

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
            return Failure(WorkspaceConflictKind.InvalidIdentityMigration, exception.Message);
        }
        catch (Exception exception) when (exception is InvalidWorkspaceAuthoring or InvalidScreenplayWorkspace or InvalidWorkspaceDocument or InvalidPortablePlayPath or JsonException or ArgumentException or InvalidOperationException ||
            exception.GetType().Namespace == "Cratis.Screenplay.Syntax.Serialization")
        {
            return Failure(WorkspaceConflictKind.InvalidOperation, exception.Message);
        }
    }

    WorkspaceAuthoringResult ProposeCore(WorkspaceAuthoringRequest request)
    {
        var index = WorkspaceSyntaxIndex.Create(workspace);
        var edits = new WorkspaceAstEdits(index);
        edits.Prepare(request.Operations);
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
                if (!candidates.ContainsKey(replace.Document) || !targeted.Add(replace.Document) || edits.Touched.Contains(replace.Document))
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

        edits.ValidateFragmentRenames(replacements);

        // Every handle, expectation, typed slot, overlap and original anchor has now been validated.
        foreach (var (id, syntax) in edits.Apply())
        {
            var document = candidates[id];
            candidates[id] = WorkspaceAuthoringPrinter.Print(id, document.StableKey, document.Path, document.Encoding, syntax, request.Formatting, _diagnostics, document);
        }

        foreach (var replacement in replacements)
        {
            var document = candidates[replacement.Document];
            candidates[document.Id] = WorkspaceAuthoringPrinter.Print(document.Id, document.StableKey, document.Path, document.Encoding, replacement.Syntax, request.Formatting, _diagnostics, document);
        }

        foreach (var creation in creations)
        {
            var document = WorkspaceDocument.Create(creation.StableKey, creation.Path, []);
            candidates[document.Id] = WorkspaceAuthoringPrinter.Print(document.Id, document.StableKey, document.Path, creation.Encoding, creation.Syntax, request.Formatting, _diagnostics);
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
        var merged = PlayFolderMerge.Merge(
            [.. ordered.OrderBy(document => document.Path.Value, StringComparer.Ordinal).Select(document => compiler.Parse(document.Text, document.Path.Value))],
            allowUnresolvedPersonaPolicies: draftAuthoring);
        _diagnostics.AddRange(merged.Diagnostics);
        if (!merged.Success || merged.Value is null)
        {
            var failed = Failure(WorkspaceConflictKind.CompilationFailed, "The final document set is not valid full-language Screenplay source.");
            var ownership = WorkspaceTransactionOperations.OwnershipConflict(merged.Diagnostics, ordered);
            return ownership is null ? failed : failed with { Conflicts = [ownership] };
        }

        var catalog = WorkspaceAuthoringIdentity.Migrate(workspace, request, ordered, merged.Value, documentRenames.ToImmutable(), retiredDocuments.ToImmutable());
        var compilation = ordered.IsEmpty
            ? ScreenplayWorkspace.EmptyCompilation()
            : new SemanticModelCompiler().Compile(workspace.ApplicationName, ScreenplayWorkspace.CreateDocumentSet(ordered, catalog, workspace.AttachmentContents));
        if (request.Validation == WorkspaceAuthoringValidation.Executable && !compilation.Success)
        {
            return Failure(WorkspaceConflictKind.CompilationFailed, "The final source is authorable but is not executable by the semantic backend.") with
            {
                ExecutableDiagnostics = [.. compilation.Diagnostics]
            };
        }

        var candidate = ScreenplayWorkspace.CreateValidated(workspace.ApplicationName, ordered, catalog, compilation, workspace.AttachmentContents);
        WorkspaceAuthoringReferences.Validate(workspace, candidate, request, _diagnostics, referenceRenames);
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

    WorkspaceAuthoringResult Failure(WorkspaceConflictKind kind, string message) => new()
    {
        Conflicts = [new WorkspaceConflict { Kind = kind, Message = message }],
        AuthoringDiagnostics = _diagnostics.ToImmutable()
    };
}
