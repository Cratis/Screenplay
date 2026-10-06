// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Screenplay.CanonicalCorpus;

public static partial class RegisterProjectCorpus
{
    /// <summary>
    /// Gets a v7 source that supplies a generated property as specification input, with no publishable artifacts.
    /// </summary>
    public static CanonicalCorpusRejectionVector GeneratedPropertySuppliedAsInput { get; } = new()
    {
        Name = "register-project/generated-property-supplied-as-input",
        ApplicationName = "Projects",
        ApplicationIdentity = ApplicationIdentity.Parse("app1:20ccb167f2400bc55fae1597b1a0f4d19b40841f513bd013a7fa815e9e7f2994"),
        SourceForm = new CanonicalCorpusSourceForm
        {
            Name = "single",
            Documents = [Document("register-project-vector", "RegisterProject.play", "Cratis.Screenplay.CanonicalCorpus.Corpus.RegisterProject.v7.rejected.generated-input.play")],
            IdentityCatalogBytes = Resource("Cratis.Screenplay.CanonicalCorpus.Corpus.RegisterProject.v7.identity.single-catalog-v1.json")
        },
        Diagnostics =
        [
            new CanonicalCorpusDiagnosticExpectation
            {
                Code = "PLAY0485",
                Message = "Generated property 'projectId' cannot be supplied as request or form input."
            }
        ]
    };

    static CanonicalCorpusVector LoadV7()
    {
        const string prefix = "Cratis.Screenplay.CanonicalCorpus.Corpus.RegisterProject.v7";
        const string response = """
            {"kind":"record","fields":[{"name":"projectId","value":{"kind":"string","value":"3fa85f64-5717-4562-b3fc-2c963f66afa6"}},{"name":"receipt","value":{"kind":"string","value":"22222222-2222-2222-2222-2222222222aa"}},{"name":"name","value":{"kind":"string","value":"Screenplay"}}]}
            """;
        var folder = new CanonicalCorpusSourceForm
        {
            Name = "folder",
            Documents =
            [
                Document("application", "application.play", $"{prefix}.source.folder.application.play"),
                Document("projects-module", "Projects/Projects.play", $"{prefix}.source.folder.Projects.Projects.play"),
                Document("projects-registration-feature", "Projects/Registration/Registration.play", $"{prefix}.source.folder.Projects.Registration.Registration.play"),
                Document("register-project-slice", "Projects/Registration/RegisterProject/RegisterProject.play", $"{prefix}.source.folder.Projects.Registration.RegisterProject.RegisterProject.play")
            ],
            IdentityCatalogBytes = Resource($"{prefix}.identity.folder-catalog-v1.json")
        };

        return new()
        {
            Name = "register-project/v7",
            ApplicationName = "Projects",
            ApplicationIdentity = ApplicationIdentity.Parse("app1:20ccb167f2400bc55fae1597b1a0f4d19b40841f513bd013a7fa815e9e7f2994"),
            RuntimeStreamId = "3fa85f64-5717-4562-b3fc-2c963f66afa6",
            SourceForms =
            [
                new CanonicalCorpusSourceForm
                {
                    Name = "single",
                    Documents = [Document("register-project-vector", "RegisterProject.play", $"{prefix}.source.RegisterProject.play")],
                    IdentityCatalogBytes = Resource($"{prefix}.identity.single-catalog-v1.json")
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
                    Documents = [.. folder.Documents.Reverse().Select(document => document with { DisplayPath = $"Archive/{document.DisplayPath}" })],
                    IdentityCatalogBytes = folder.IdentityCatalogBytes
                }
            ],
            SpecificationExpectations =
            [
                new CanonicalCorpusSpecificationExpectation
                {
                    Specification = SemanticId.Parse("sem1:07378ab124bda3d7fdf905751b3ff8a7fff65f3b7dbb2fa278bd49091a571c66"),
                    Name = "MismatchedReturn",
                    Outcome = SemanticExecutionOutcomeKind.Accepted,
                    Passed = false,
                    WorldFactCount = 1,
                    Response = response
                },
                new CanonicalCorpusSpecificationExpectation
                {
                    Specification = SemanticId.Parse("sem1:9b88b321a5d6bc32200d7c077626ca34d1859f498042a8bbedfd5c14f5b62fa2"),
                    Name = "MissingAllocation",
                    Outcome = SemanticExecutionOutcomeKind.Unsupported,
                    Passed = false,
                    UnsupportedCapability = SemanticExecutionCapability.IdentityAllocation,
                    WorldFactCount = 0
                },
                new CanonicalCorpusSpecificationExpectation
                {
                    Specification = SemanticId.Parse("sem1:a65de3ac412b245dc9649944e9940214ee53f8e9b354941ea33a0f4bca62cf62"),
                    Name = "RegisteringAProject",
                    Outcome = SemanticExecutionOutcomeKind.Accepted,
                    WorldFactCount = 1,
                    Response = response
                }
            ],
            EsmBytes = Resource($"{prefix}.expected.esm-v7.json"),
            SemanticRevision = SemanticRevision.Parse(System.Text.Encoding.UTF8.GetString(Resource($"{prefix}.expected.semantic-revision.txt").AsSpan()).Trim())
        };
    }
}
