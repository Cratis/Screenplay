// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics;

public sealed partial class SemanticIdentityCatalog
{
    sealed class ContinuityValidation(
        SemanticIdentityCatalog previous,
        ImmutableArray<string> currentKeys,
        ImmutableArray<SemanticAddress> currentSemantics,
        ImmutableArray<SemanticAddress> currentEvents,
        ImmutableArray<DocumentIdentityRename> documentRenames,
        ImmutableArray<SemanticIdentityRename> semanticRenames,
        ImmutableArray<EventContractIdentityRename> eventRenames,
        ImmutableArray<string> retiredDocumentKeys,
        ImmutableArray<SemanticAddress> retiredSemanticAddresses,
        ImmutableArray<SemanticAddress> retiredEventAddresses)
    {
        internal void Validate()
        {
            ValidateInputs();
            ValidateRenames();

            var renamedDocumentSources = documentRenames.Select(_ => _.PreviousKey).ToHashSet(StringComparer.Ordinal);
            var renamedSemanticSources = semanticRenames.Select(_ => _.PreviousAddress).ToHashSet();
            var renamedEventSources = eventRenames.Select(_ => _.PreviousAddress).ToHashSet();
            ValidateRetirements(renamedDocumentSources, renamedSemanticSources, renamedEventSources);
            ValidateRemainingAssignments(renamedDocumentSources, renamedSemanticSources, renamedEventSources);
        }

        void ValidateInputs()
        {
            ValidateSemanticRenameEndpoints(semanticRenames);
            ValidateEventRenameEndpoints(eventRenames);
            ValidateOneToOne(documentRenames, _ => _.PreviousKey, _ => _.CurrentKey, StringComparer.Ordinal, "document rename");
            RejectMigrationDuplicates(semanticRenames.Select(_ => _.PreviousAddress), nameof(semanticRenames));
            RejectMigrationDuplicates(semanticRenames.Select(_ => _.CurrentAddress), nameof(semanticRenames));
            RejectMigrationDuplicates(eventRenames.Select(_ => _.PreviousAddress), nameof(eventRenames));
            RejectMigrationDuplicates(eventRenames.Select(_ => _.CurrentAddress), nameof(eventRenames));
            RejectDuplicates(retiredDocumentKeys, StringComparer.Ordinal, "retired document key");
            RejectMigrationDuplicates(retiredSemanticAddresses, nameof(retiredSemanticAddresses));
            RejectMigrationDuplicates(retiredEventAddresses, nameof(retiredEventAddresses));
        }

        void ValidateRenames()
        {
            IdentityMigrationIssue.Reject(
                "An identity rename is stale, guessed, or does not map one removed assignment to one new address.",
                InvalidSemanticRenames().Concat(InvalidEventRenames()));
            if (documentRenames.Any(IsInvalidDocumentRename))
            {
                throw new InvalidSemanticContract("An identity rename is stale, guessed, or does not map one removed assignment to one new address.");
            }
        }

        bool IsInvalidDocumentRename(DocumentIdentityRename rename) =>
            rename.PreviousKey == rename.CurrentKey || !previous.Documents.Any(assignment => assignment.Key == rename.PreviousKey) ||
            previous.Documents.Any(assignment => assignment.Key == rename.CurrentKey) || !currentKeys.Contains(rename.CurrentKey) || currentKeys.Contains(rename.PreviousKey);

        IEnumerable<IdentityMigrationIssue> InvalidSemanticRenames() =>
            semanticRenames.Where(_ => _.PreviousAddress.Equals(_.CurrentAddress) || !previous.Semantics.Any(a => a.Address.Equals(_.PreviousAddress)) ||
                previous.Semantics.Any(a => a.Address.Equals(_.CurrentAddress)) || !currentSemantics.Contains(_.CurrentAddress) || currentSemantics.Contains(_.PreviousAddress))
                .SelectMany(_ => RenameIssues(nameof(semanticRenames), _.PreviousAddress, _.CurrentAddress));

        IEnumerable<IdentityMigrationIssue> InvalidEventRenames() =>
            eventRenames.Where(_ => _.PreviousAddress.Equals(_.CurrentAddress) || !previous.EventContracts.Any(a => a.Address.Equals(_.PreviousAddress)) ||
                previous.EventContracts.Any(a => a.Address.Equals(_.CurrentAddress)) || !currentEvents.Contains(_.CurrentAddress) || currentEvents.Contains(_.PreviousAddress))
                .SelectMany(_ => RenameIssues(nameof(eventRenames), _.PreviousAddress, _.CurrentAddress));

        void ValidateRetirements(
            HashSet<string> renamedDocumentSources,
            HashSet<SemanticAddress> renamedSemanticSources,
            HashSet<SemanticAddress> renamedEventSources)
        {
            IdentityMigrationIssue.Reject(
                "An identity retirement is stale, still current, duplicated by a rename, or was never assigned.",
                InvalidSemanticRetirements(renamedSemanticSources).Concat(InvalidEventRetirements(renamedEventSources)));
            if (retiredDocumentKeys.Any(key => IsInvalidDocumentRetirement(key, renamedDocumentSources)))
            {
                throw new InvalidSemanticContract("An identity retirement is stale, still current, duplicated by a rename, or was never assigned.");
            }
        }

        bool IsInvalidDocumentRetirement(string key, HashSet<string> renamedDocumentSources) =>
            !previous.Documents.Any(assignment => assignment.Key == key) || currentKeys.Contains(key) || renamedDocumentSources.Contains(key);

        IEnumerable<IdentityMigrationIssue> InvalidSemanticRetirements(HashSet<SemanticAddress> renamedSemanticSources) =>
            retiredSemanticAddresses.Where(address => !previous.Semantics.Any(_ => _.Address.Equals(address)) || currentSemantics.Contains(address) || renamedSemanticSources.Contains(address))
                .Select(address => new IdentityMigrationIssue([nameof(retiredSemanticAddresses)], address));

        IEnumerable<IdentityMigrationIssue> InvalidEventRetirements(HashSet<SemanticAddress> renamedEventSources) =>
            retiredEventAddresses.Where(address => !previous.EventContracts.Any(_ => _.Address.Equals(address)) || currentEvents.Contains(address) || renamedEventSources.Contains(address))
                .Select(address => new IdentityMigrationIssue([nameof(retiredEventAddresses)], address));

        void ValidateRemainingAssignments(
            HashSet<string> renamedDocumentSources,
            HashSet<SemanticAddress> renamedSemanticSources,
            HashSet<SemanticAddress> renamedEventSources)
        {
            var retiredDocuments = retiredDocumentKeys.ToHashSet(StringComparer.Ordinal);
            var retiredSemantics = retiredSemanticAddresses.ToHashSet();
            var retiredEvents = retiredEventAddresses.ToHashSet();
            IdentityMigrationIssue.Reject(
                "A base catalog assignment is stale and has no explicit one-to-one rename or retirement. Choose a rename to preserve identity or a retirement for a removed declaration.",
                MissingSemanticAssignments(renamedSemanticSources, retiredSemantics).Concat(MissingEventAssignments(renamedEventSources, retiredEvents)));
            if (previous.Documents.Any(assignment => IsMissingDocumentAssignment(assignment, renamedDocumentSources, retiredDocuments)))
            {
                throw new InvalidSemanticContract("A base catalog assignment is stale and has no explicit one-to-one rename or retirement.");
            }
        }

        bool IsMissingDocumentAssignment(DocumentIdentityAssignment assignment, HashSet<string> renamedDocumentSources, HashSet<string> retiredDocuments) =>
            !currentKeys.Contains(assignment.Key) && !renamedDocumentSources.Contains(assignment.Key) && !retiredDocuments.Contains(assignment.Key);

        IEnumerable<IdentityMigrationIssue> MissingSemanticAssignments(HashSet<SemanticAddress> renamedSemanticSources, HashSet<SemanticAddress> retiredSemantics) =>
            previous.Semantics.Where(_ => !currentSemantics.Contains(_.Address) && !renamedSemanticSources.Contains(_.Address) && !retiredSemantics.Contains(_.Address))
                .Select(_ => new IdentityMigrationIssue([nameof(semanticRenames), nameof(retiredSemanticAddresses)], _.Address));

        IEnumerable<IdentityMigrationIssue> MissingEventAssignments(HashSet<SemanticAddress> renamedEventSources, HashSet<SemanticAddress> retiredEvents) =>
            previous.EventContracts.Where(_ => !currentEvents.Contains(_.Address) && !renamedEventSources.Contains(_.Address) && !retiredEvents.Contains(_.Address))
                .Select(_ => new IdentityMigrationIssue([nameof(eventRenames), nameof(retiredEventAddresses)], _.Address));
    }
}
