// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Screenplay.CanonicalCorpus;

/// <summary>
/// Provides the canonical RegisterProject conformance corpus.
/// </summary>
public static class RegisterProjectCorpus
{
    const string ResourcePrefix = "Cratis.Screenplay.CanonicalCorpus.Corpus.RegisterProject.v1_legacy";

    /// <summary>
    /// Gets the immutable legacy-v1 corpus that freezes the currently released semantic baseline before #167 migration.
    /// </summary>
    public static CanonicalCorpusVector LegacyV1 { get; } = LoadLegacyV1();

    /// <summary>
    /// Gets the RegisterProject corpus with typed state-change destination and an explicit specification event source.
    /// </summary>
    public static CanonicalCorpusVector V2 { get; } = LoadV2();

    /// <summary>
    /// Gets a source that parses but cannot be bound to portable ESM, with no publishable artifacts.
    /// </summary>
    public static CanonicalCorpusRejectionVector UnsupportedSequence { get; } = new()
    {
        Name = "register-project/unsupported-sequence",
        ApplicationName = "Projects",
        ApplicationIdentity = ApplicationIdentity.Parse("app1:20ccb167f2400bc55fae1597b1a0f4d19b40841f513bd013a7fa815e9e7f2994"),
        SourceForm = new CanonicalCorpusSourceForm
        {
            Name = "single",
            Documents = [Document("register-project-vector", "RegisterProject.play", "Cratis.Screenplay.CanonicalCorpus.Corpus.RegisterProject.unsupported_sequence.source.RegisterProject.play")],
            IdentityCatalogBytes = Resource($"{ResourcePrefix}.identity.catalog-v1.json")
        },
        Diagnostics =
        [
            new CanonicalCorpusDiagnosticExpectation
            {
                Code = "PLAY0268",
                Message = "Projection 'ProjectSummaryProjection' sequence is not portable ESM v1 behavior: which event sequence a projection observes is a realization concern."
            }
        ]
    };

    static CanonicalCorpusVector LoadLegacyV1()
    {
        var single = Document(
            "register-project-vector",
            "RegisterProject.play",
            $"{ResourcePrefix}.source.single.RegisterProject.play");
        var folder = new CanonicalCorpusSourceForm
        {
            Name = "folder",
            Documents =
            [
                Document("application", "application.play", $"{ResourcePrefix}.source.folder.application.play"),
                Document("projects-module", "Projects/Projects.play", $"{ResourcePrefix}.source.folder.Projects.Projects.play"),
                Document("projects-registration-feature", "Projects/Registration/Registration.play", $"{ResourcePrefix}.source.folder.Projects.Registration.Registration.play"),
                Document("register-project-slice", "Projects/Registration/RegisterProject/RegisterProject.play", $"{ResourcePrefix}.source.folder.Projects.Registration.RegisterProject.RegisterProject.play"),
                Document("project-lookup-slice", "Projects/Registration/ProjectLookup/ProjectLookup.play", $"{ResourcePrefix}.source.folder.Projects.Registration.ProjectLookup.ProjectLookup.play")
            ],
            IdentityCatalogBytes = Resource($"{ResourcePrefix}.identity.folder-catalog-v1.json")
        };
        var reordered = new CanonicalCorpusSourceForm
        {
            Name = "reordered",
            Documents = [.. folder.Documents.Reverse().Select(document => Document(
                document.StableKey,
                document.DisplayPath,
                $"{ResourcePrefix}.source.reordered.{document.DisplayPath.Replace('/', '.')}"))],
            IdentityCatalogBytes = folder.IdentityCatalogBytes
        };
        var relocated = new CanonicalCorpusSourceForm
        {
            Name = "relocated",
            Documents = [.. folder.Documents.Reverse().Select(document => Document(
                document.StableKey,
                $"Archive/{document.DisplayPath}",
                $"{ResourcePrefix}.source.relocated.Archive.{document.DisplayPath.Replace('/', '.')}"))],
            IdentityCatalogBytes = folder.IdentityCatalogBytes
        };
        var revision = System.Text.Encoding.UTF8.GetString(Resource($"{ResourcePrefix}.expected.semantic-revision.txt").AsSpan()).Trim();
        return new CanonicalCorpusVector
        {
            Name = "register-project/v1-legacy",
            ApplicationName = "Projects",
            ApplicationIdentity = ApplicationIdentity.Parse("app1:20ccb167f2400bc55fae1597b1a0f4d19b40841f513bd013a7fa815e9e7f2994"),
            RuntimeStreamId = "3fa85f64-5717-4562-b3fc-2c963f66afa6",
            SourceForms =
            [
                new CanonicalCorpusSourceForm
                {
                    Name = "single",
                    Documents = [single],
                    IdentityCatalogBytes = Resource($"{ResourcePrefix}.identity.catalog-v1.json")
                },
                folder,
                reordered,
                relocated
            ],
            SpecificationExpectations =
            [
                new CanonicalCorpusSpecificationExpectation
                {
                    Specification = SemanticId.Parse("sem1:951a1e8506741ec7552de6a3cd3ef5814c0b3c8d9600ab00fd1c3a3255664ae0"),
                    Name = "RejectingAnEmptyProjectName",
                    Outcome = SemanticExecutionOutcomeKind.Rejected,
                    RejectionMessage = "Project name is required"
                },
                new CanonicalCorpusSpecificationExpectation
                {
                    Specification = SemanticId.Parse("sem1:a65de3ac412b245dc9649944e9940214ee53f8e9b354941ea33a0f4bca62cf62"),
                    Name = "RegisteringAProject",
                    Outcome = SemanticExecutionOutcomeKind.Accepted
                }
            ],
            EsmBytes = Resource($"{ResourcePrefix}.expected.esm-v1.json"),
            SemanticRevision = SemanticRevision.Parse(revision)
        };
    }

    static CanonicalCorpusVector LoadV2()
    {
        const string v2 = "Cratis.Screenplay.CanonicalCorpus.Corpus.RegisterProject.v2";
        var folder = new CanonicalCorpusSourceForm
        {
            Name = "folder",
            Documents =
            [
                Document("application", "application.play", $"{v2}.source.folder.application.play"),
                Document("projects-module", "Projects/Projects.play", $"{v2}.source.folder.Projects.Projects.play"),
                Document("projects-registration-feature", "Projects/Registration/Registration.play", $"{v2}.source.folder.Projects.Registration.Registration.play"),
                Document("register-project-slice", "Projects/Registration/RegisterProject/RegisterProject.play", $"{v2}.source.folder.Projects.Registration.RegisterProject.RegisterProject.play"),
                Document("project-lookup-slice", "Projects/Registration/ProjectLookup/ProjectLookup.play", $"{v2}.source.folder.Projects.Registration.ProjectLookup.ProjectLookup.play")
            ],
            IdentityCatalogBytes = Resource($"{v2}.identity.folder-catalog-v4.json")
        };
        return new()
    {
        Name = "register-project/v2",
        ApplicationName = "Projects",
        ApplicationIdentity = ApplicationIdentity.Parse("app1:20ccb167f2400bc55fae1597b1a0f4d19b40841f513bd013a7fa815e9e7f2994"),
        RuntimeStreamId = "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        SourceForms =
        [
            new CanonicalCorpusSourceForm
            {
                Name = "single",
                Documents = [Document("register-project-vector", "RegisterProject.play", "Cratis.Screenplay.CanonicalCorpus.Corpus.RegisterProject.v2.source.RegisterProject.play")],
                IdentityCatalogBytes = Resource($"{v2}.identity.catalog-v4.json")
            },
            folder,
            new CanonicalCorpusSourceForm
            {
                Name = "reordered",
                Documents = [.. folder.Documents.Reverse()],
                IdentityCatalogBytes = folder.IdentityCatalogBytes
            },
            new CanonicalCorpusSourceForm
            {
                Name = "relocated",
                Documents = [.. folder.Documents.Reverse().Select(document => new CanonicalCorpusDocument
                {
                    StableKey = document.StableKey,
                    DisplayPath = $"Archive/{document.DisplayPath}",
                    Bytes = document.Bytes
                })],
                IdentityCatalogBytes = folder.IdentityCatalogBytes
            }
        ],
        SpecificationExpectations =
        [
            new CanonicalCorpusSpecificationExpectation
            {
                Specification = SemanticId.Parse("sem1:951a1e8506741ec7552de6a3cd3ef5814c0b3c8d9600ab00fd1c3a3255664ae0"),
                Name = "RejectingAnEmptyProjectName",
                Outcome = SemanticExecutionOutcomeKind.Rejected,
                RejectionMessage = "Project name is required"
            },
            new CanonicalCorpusSpecificationExpectation
            {
                Specification = SemanticId.Parse("sem1:a65de3ac412b245dc9649944e9940214ee53f8e9b354941ea33a0f4bca62cf62"),
                Name = "RegisteringAProject",
                Outcome = SemanticExecutionOutcomeKind.Accepted
            }
        ],
        EsmBytes = Resource("Cratis.Screenplay.CanonicalCorpus.Corpus.RegisterProject.v2.expected.esm-v4.json"),
        SemanticRevision = SemanticRevision.Parse("rev1:c2bef8cf2ba598b564f67d9dc96a5555f4246ac33c6937cb83409b71acc2b3dd")
    };
    }

    static CanonicalCorpusDocument Document(string stableKey, string path, string resource) => new()
    {
        StableKey = stableKey,
        DisplayPath = path,
        Bytes = Resource(resource)
    };

    static ImmutableArray<byte> Resource(string name)
    {
        using var stream = typeof(RegisterProjectCorpus).Assembly.GetManifestResourceStream(name) ??
                           throw new InvalidOperationException($"Canonical corpus resource '{name}' is missing");
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return [.. memory.ToArray()];
    }
}
