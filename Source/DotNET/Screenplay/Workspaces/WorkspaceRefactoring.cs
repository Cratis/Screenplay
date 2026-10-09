// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Dependencies;
using Cratis.Screenplay.Parsing;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Workspaces;

sealed class WorkspaceRefactoring(ScreenplayWorkspace workspace)
{
    // The complete candidate must bind every original absence obligation, at its unchanged position, to the migrated
    // original target, and only members bound to the renamed declaration may change their text.
    internal static void RequireAbsenceContinuity(
        WorkspaceAbsenceKeyBindings before,
        WorkspaceAbsenceKeyBindings after,
        Dictionary<SemanticAddress, SemanticAddress> migrations,
        SemanticAddress? renamed,
        string newName)
    {
        var remaining = after.Obligations.ToDictionary(obligation => obligation.Position);
        foreach (var previous in before.Obligations)
        {
            if (!remaining.Remove(previous.Position, out var current) || current.IsKey != previous.IsKey ||
                previous.Target?.Address is not { } original || current.Target?.Address is not { } address ||
                !(migrations.GetValueOrDefault(original) ?? original).Equals(address) ||
                (!previous.IsKey && current.Text != (renamed?.Equals(original) == true ? newName : previous.Text)))
            {
                throw new InvalidWorkspaceAuthoring($"Rename changes absence key binding at '{previous.Occurrence.Handle.Path}' ({previous.Text}). Capture, retargeting, and lost absence keys are not admitted.");
            }
        }

        if (remaining.Count > 0)
        {
            throw new InvalidWorkspaceAuthoring("Rename introduced unexpected absence key occurrences.");
        }
    }

    internal WorkspaceAuthoringResult Rename(WorkspaceRenameRequest request)
    {
        if (request is null)
        {
            return Failure(WorkspaceConflictKind.InvalidOperation, "A rename request is required.");
        }

        if (request.ExpectedRevision != workspace.Revision)
        {
            return Failure(WorkspaceConflictKind.StaleWorkspaceRevision, "The rename has a stale workspace revision.");
        }

        if (request.ExpectedCatalogRevision != workspace.IdentityCatalog.Revision)
        {
            return Failure(WorkspaceConflictKind.StaleCatalogRevision, "The rename has a stale catalog revision.");
        }

        try
        {
            return RenameCore(request);
        }
        catch (Exception exception) when (exception is InvalidWorkspaceAuthoring or InvalidSemanticContract)
        {
            return Failure(WorkspaceConflictKind.InvalidOperation, exception.Message);
        }
    }

    static IEnumerable<WorkspaceSyntaxEntry> Ancestors(WorkspaceSyntaxEntry entry, WorkspaceSyntaxIndex index)
    {
        var current = entry.Parent is { } parent ? index.Find(parent) : null;
        while (current is not null)
        {
            yield return current;
            current = current.Parent is { } ancestor ? index.Find(ancestor) : null;
        }
    }

    static void RejectOpaque(ImmutableArray<WorkspaceDocument> documents, WorkspaceSyntaxIndex index, WorkspaceSyntaxEntry target, WorkspaceRenameRequest request)
    {
        foreach (var entry in index.Entries)
        {
            var named = WorkspaceOpaqueText.Named(entry.Node, request.ExpectedName, request.NewName) ??
                PossibleReferencePaths(entry.Node).SelectMany(path => path.Split('.')).FirstOrDefault(segment => segment == request.ExpectedName || segment == request.NewName);
            if (named is not null)
            {
                throw new InvalidWorkspaceAuthoring($"Cannot prove reference safety through {entry.Kind} at '{Position(documents, entry)}' ({entry.Handle.Path}), which names '{named}'. Use explicit coordinated typed edits; canonical formatting does not make opaque references safe.");
            }

            if (target.Node is ReadModelSyntax && entry.Node is ProjectionVariantSyntax variant && variant.Name == request.ExpectedName)
            {
                throw new InvalidWorkspaceAuthoring("A projection variant names its output read model. Rename the variant and its read model with explicit coordinated typed edits; variant output continuity is not proven by this rename operation.");
            }

            if (target.Node is ReadModelSyntax && entry.Node is ProjectionSyntax { ReadModel: null } projection && projection.Name == request.ExpectedName)
            {
                throw new InvalidWorkspaceAuthoring("A projection's implicit output name is also its declaration identity. Make the output alias explicit before renaming this read model.");
            }
        }
    }

    static string Position(ImmutableArray<WorkspaceDocument> documents, WorkspaceSyntaxEntry entry) =>
        $"{documents.Single(document => document.Id == entry.Handle.Document).Path.Value}({entry.Location.Line},{entry.Location.Column})";

    static IEnumerable<string> PossibleReferencePaths(SyntaxNode node) => node switch
    {
        PathExpressionSyntax path => [path.Path],
        ComparisonConditionSyntax condition => [condition.Left],
        ValidationRuleSyntax rule => [rule.Property],
        ScreenTableSyntax table => [table.Target],
        ScreenSummarySyntax summary => [summary.Target],
        FormFieldSyntax { From: { } source } => [source],
        _ => []
    };

    static void RejectHierarchyCollision(WorkspaceSyntaxIndex index, WorkspaceSyntaxEntry target, string newName)
    {
        if (target.Node is not (ModuleSyntax or FeatureSyntax))
        {
            return;
        }

        var scope = WorkspaceReferenceBindings.Scope(target, index);
        if (index.Entries.Any(entry => entry.Node.GetType() == target.Node.GetType() &&
            entry.Address?.Equals(target.Address) == false && WorkspaceReferenceBindings.Name(entry.Node) == newName &&
            WorkspaceReferenceBindings.Scope(entry, index).Segments.SequenceEqual(scope.Segments)))
        {
            throw new InvalidWorkspaceAuthoring($"Cannot merge distinct logical {target.Address!.Kind} declarations by renaming '{target.Address.Name}' to '{newName}'.");
        }
    }

    static bool Identifier(string? value) => !string.IsNullOrEmpty(value) && (char.IsLetter(value[0]) || value[0] == '_') && value.All(character => char.IsLetterOrDigit(character) || character == '_');

    static string? PlannedEventId(EventSyntax declaration, WorkspaceRenameRequest request)
    {
        if (request.NewName == request.ExpectedName)
        {
            return declaration.Id;
        }

        var pin = declaration.Id ?? (request.EventNeverPersisted ? null : declaration.Name);
        if (request.EventNeverPersisted && pin == declaration.Name)
        {
            pin = null;
        }

        return pin == request.NewName ? null : pin;
    }

    static WorkspaceAuthoringResult Failure(WorkspaceConflictKind kind, string message) => new()
    {
        Conflicts = [new WorkspaceConflict { Kind = kind, Message = message }]
    };

    WorkspaceAuthoringResult RenameCore(WorkspaceRenameRequest request)
    {
        var index = WorkspaceSyntaxIndex.Create(workspace);
        var target = request.Target is null ? null : index.Find(request.Target);
        var compositeProperty = target?.Node is PropertySyntax && target.Parent is { } parent && index.Find(parent)?.Node is TypeSyntax;
        if (target?.Node is ConstraintSyntax)
        {
            throw new InvalidWorkspaceAuthoring("Constraint names are executable identity: renaming starts an empty constraint index and changes default rejection messages. Safe rename cannot preserve this contract; use explicit coordinated typed edits.");
        }

        if (target is null || (target.Address is null && target.Node is not (SpecificationExampleSyntax or EventSourceSyntax or EventStreamSyntax or PersonaSyntax or SpecificationParameterSyntax or SpecificationCaseSyntax or SpecificationSyntax)) ||
            (!compositeProperty && target.Node is not (ConceptSyntax or TypeSyntax or CommandSyntax or EventSyntax or ReadModelSyntax or QuerySyntax or ModuleSyntax or FeatureSyntax or SliceSyntax or SpecificationExampleSyntax or SpecificationSyntax or ReactionSyntax or EventSourceSyntax or EventStreamSyntax or PersonaSyntax or SpecificationParameterSyntax or SpecificationCaseSyntax)))
        {
            throw new InvalidWorkspaceAuthoring("The target must be a current concept, type, composite-type property, command, event, read model, query, module, feature, slice, example, specification, reaction, event source, or stream declaration handle.");
        }

        if (WorkspaceReferenceBindings.Name(target.Node) != request.ExpectedName || !Identifier(request.NewName))
        {
            throw new InvalidWorkspaceAuthoring("The expected name must match exactly and the new name must be one unqualified identifier (letters, digits, underscore).");
        }

        if (index.Entries.Count(entry => entry.Parent is null) != workspace.Documents.Length)
        {
            throw new InvalidWorkspaceAuthoring("Every document must parse before references can be proven safe.");
        }

        if (compositeProperty && target.Parent is { } typeParent && index.Find(typeParent)?.Node is TypeSyntax owner &&
            owner.Properties.Any(property => property.Name == request.NewName && property.Name != request.ExpectedName))
        {
            throw new InvalidWorkspaceAuthoring($"Composite type '{owner.Name}' already declares property '{request.NewName}'.");
        }

        if (target.Node is ReactionSyntax && index.Entries.Any(entry =>
            entry.Handle != target.Handle && entry.Parent == target.Parent &&
            entry.Node is ReactionSyntax && WorkspaceReferenceBindings.Name(entry.Node) == request.NewName))
        {
            throw new InvalidWorkspaceAuthoring($"The owning slice already declares {target.Kind} '{request.NewName}'.");
        }

        if (target.Node is SpecificationParameterSyntax or SpecificationCaseSyntax && index.Entries.Any(entry => entry.Handle != target.Handle && entry.Parent == target.Parent && entry.Node.GetType() == target.Node.GetType() && WorkspaceReferenceBindings.Name(entry.Node) == request.NewName))
        {
            throw new InvalidWorkspaceAuthoring($"The table already declares {target.Kind} '{request.NewName}'.");
        }

        var eventSource = target.Node as EventSourceSyntax ?? (target.Node is EventStreamSyntax && target.Parent is { } sourceParent ? index.Find(sourceParent)?.Node as EventSourceSyntax : null);
        if (eventSource is not null && index.Entries.Count(entry => entry.Node is EventSourceSyntax source && source.Name == eventSource.Name) > 1)
        {
            throw new InvalidWorkspaceAuthoring($"Event source '{eventSource.Name}' has more than one physical declaration. Rename requires one physical source and stream.");
        }

        if (target.Node is EventSourceSyntax && index.Entries.Any(entry =>
            entry.Handle != target.Handle && entry.Node is EventSourceSyntax source && source.Name == request.NewName))
        {
            throw new InvalidWorkspaceAuthoring($"The application already declares event source '{request.NewName}'.");
        }

        if (target.Node is EventStreamSyntax && index.Entries.Any(entry =>
            entry.Handle != target.Handle && entry.Parent == target.Parent && entry.Node is EventStreamSyntax stream && stream.Name == request.NewName))
        {
            throw new InvalidWorkspaceAuthoring($"Event source '{eventSource!.Name}' already declares stream '{request.NewName}'.");
        }

        bool IsTarget(WorkspaceSyntaxEntry entry) => target.Address is { } address ? address.Equals(entry.Address) : target.Handle == entry.Handle;

        RejectHierarchyCollision(index, target, request.NewName);
        if (target.Node is not (SpecificationParameterSyntax or SpecificationCaseSyntax)) RejectOpaque(workspace.Documents, index, target, request);
        var bindings = new WorkspaceReferenceBindings(index);
        bindings.RequireNoCollisions();
        if (target.Node is ModuleSyntax or FeatureSyntax)
        {
            var containers = index.Entries.Where(entry => entry.Node is ModuleSyntax or FeatureSyntax)
                .DistinctBy(entry => entry.Address).ToArray();
            var declarations = containers.Select(entry => new Declaration(WorkspaceReferenceBindings.Name(entry.Node)!, WorkspaceReferenceBindings.Scope(entry, index))).ToArray();
            foreach (var binding in bindings.Bindings.Where(binding => binding.Reference.Domain == WorkspaceReferenceDomain.Container && binding.Target is null))
            {
                var resolution = DeclaredDependencyTargets.Resolve(binding.Reference.Text, WorkspaceReferenceBindings.Scope(binding.Reference.Entry, index), declarations);
                var couldName = binding.Reference.Text.Split('.').Contains(request.ExpectedName, StringComparer.Ordinal) ||
                    resolution.Ambiguous.Any(candidate => containers.Any(entry => WorkspaceReferenceBindings.Name(entry.Node) == candidate.Name &&
                        WorkspaceReferenceBindings.Scope(entry, index).Segments.SequenceEqual(candidate.Scope.Segments) &&
                        (IsTarget(entry) || Ancestors(entry, index).Any(IsTarget))));
                if (couldName)
                {
                    throw new InvalidWorkspaceAuthoring($"Cannot prove a rename while dependency target '{binding.Reference.Text}' is {binding.Outcome}. Repair the declaration with a typed edit first.");
                }
            }
        }

        var absence = new WorkspaceAbsenceKeyBindings(index, bindings);
        if (absence.Obligations.FirstOrDefault(obligation => obligation.Target is null) is { } debt)
        {
            throw new InvalidWorkspaceAuthoring($"Cannot prove a rename while absence key '{debt.Text}' at '{Position(workspace.Documents, debt.Occurrence)}' is unresolved ({debt.Reason}). Repair the absence key with a typed edit first.");
        }

        var generations = index.Entries.Where(IsTarget).Select(entry => entry.Node).OfType<EventSyntax>().ToArray();
        if (generations.Select(declaration => declaration.Id ?? declaration.Name).Distinct(StringComparer.Ordinal).Skip(1).Any())
        {
            throw new InvalidWorkspaceAuthoring("Event generations have contradictory effective identity pins. Resolve them before renaming.");
        }

        var plannedEventIds = generations.ToDictionary(declaration => declaration, declaration => PlannedEventId(declaration, request));
        if (plannedEventIds.Values.Select(id => id ?? request.NewName).Distinct(StringComparer.Ordinal).Skip(1).Any())
        {
            throw new InvalidWorkspaceAuthoring("Rename would give event generations contradictory effective identity pins. Resolve them before renaming.");
        }

        var roots = index.Entries.Where(entry => entry.Parent is null).ToDictionary(entry => entry.Handle.Document, entry => WorkspaceSyntaxMutation.Json(entry.Node));
        var touched = new HashSet<DocumentId>();
        var insertsEventPin = false;
        foreach (var entry in index.Entries.Where(IsTarget).ToArray())
        {
            WorkspaceSyntaxMutation.Set(roots[entry.Handle.Document], $"{entry.Handle.Path}/name", request.NewName);
            if (entry.Node is EventSyntax declaration && request.NewName != request.ExpectedName)
            {
                var id = plannedEventIds[declaration];
                insertsEventPin |= declaration.Id is null && id is not null;
                WorkspaceSyntaxMutation.Set(roots[entry.Handle.Document], $"{entry.Handle.Path}/id", id);
            }

            touched.Add(entry.Handle.Document);
        }

        foreach (var binding in bindings.Bindings.Where(binding => binding.Target?.Entry is not null))
        {
            var declaration = binding.Target!;
            var entry = declaration.Entry!;
            var scope = WorkspaceReferenceBindings.Scope(entry, index).Segments.ToArray();
            var ancestors = Ancestors(entry, index).Where(ancestor => ancestor.Node is ModuleSyntax or FeatureSyntax or SliceSyntax).Reverse().ToArray();
            for (var position = 0; position < ancestors.Length; position++)
            {
                if (IsTarget(ancestors[position]))
                {
                    scope[position] = request.NewName;
                }
            }

            var name = IsTarget(entry) ? request.NewName : declaration.Name;
            var segments = binding.Reference.Text.Split('.');
            var replacement = segments.Length == 1 ? name : string.Join('.', scope.TakeLast(segments.Length - 1).Append(name));
            if (replacement != binding.Reference.Text)
            {
                WorkspaceSyntaxMutation.Set(roots[binding.Reference.Entry.Handle.Document], binding.Reference.Path, replacement);
                touched.Add(binding.Reference.Entry.Handle.Document);
            }
        }

        // Only absence-key members bound to the renamed composite-type property are rewritten.
        foreach (var obligation in absence.Obligations.Where(obligation => !obligation.IsKey && target.Address?.Equals(obligation.Target!.Address) == true))
        {
            WorkspaceSyntaxMutation.Set(roots[obligation.Occurrence.Handle.Document], $"{obligation.Occurrence.Handle.Path}/name", request.NewName);
            touched.Add(obligation.Occurrence.Handle.Document);
        }

        var referenceRenames = new Dictionary<SemanticAddress, SemanticAddress>();
        var semanticRenames = new Dictionary<SemanticAddress, SemanticAddress>();
        var eventRenames = new Dictionary<SemanticAddress, SemanticAddress>();
        var documents = ImmutableArray.CreateBuilder<WorkspaceOperation>();
        foreach (var document in touched)
        {
            var intended = WorkspaceSyntaxMutation.Syntax(roots[document]);
            var after = WorkspaceSyntaxIndex.ForSyntax(intended, workspace.IdentityCatalog).ToDictionary(entry => entry.Handle.Path, StringComparer.Ordinal);
            foreach (var before in index.Entries.Where(entry => entry.Handle.Document == document && entry.Address is not null))
            {
                var current = after[before.Handle.Path].Address;
                if (current?.Equals(before.Address) == false)
                {
                    referenceRenames[before.Address!] = current;
                    if (before.SemanticId is not null)
                    {
                        semanticRenames[before.Address!] = current;
                    }

                    if (before.EventContractId is not null)
                    {
                        eventRenames[before.Address!] = current;
                    }
                }
            }

            documents.Add(new ReplaceWorkspaceSyntaxDocument(document, intended));
        }

        var result = new WorkspaceAuthoringTransaction(workspace, referenceRenames, touched).Propose(new()
        {
            ExpectedRevision = request.ExpectedRevision,
            ExpectedCatalogRevision = request.ExpectedCatalogRevision,
            Formatting = request.Formatting,
            Validation = request.Validation,
            Documents = documents.ToImmutable(),
            SemanticRenames = [.. semanticRenames.Select(pair => new SemanticIdentityRename(pair.Key, pair.Value))],
            EventRenames = [.. eventRenames.Select(pair => new EventContractIdentityRename(pair.Key, pair.Value))]
        });
        if (!result.Accepted)
        {
            return result;
        }

        if (insertsEventPin)
        {
            result = WorkspaceRepairVerification.RequireComments(result);
            if (!result.Accepted)
            {
                return result;
            }
        }

        var candidateIndex = WorkspaceSyntaxIndex.Create(result.Workspace!);
        var candidateBindings = new WorkspaceReferenceBindings(candidateIndex);
        candidateBindings.RequireNoCollisions();
        WorkspaceReferenceSafety.RequireRenameContinuity(bindings, candidateBindings);
        RequireAbsenceContinuity(absence, new WorkspaceAbsenceKeyBindings(candidateIndex, candidateBindings), referenceRenames, target.Address, request.NewName);

        return result;
    }
}
