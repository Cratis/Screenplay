// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Workspaces.for_ScreenplayModelRoot.given;

public class a_persisted_model_root : Specification
{
    protected const string Source = """
        concept ProjectId : Uuid
        concept ProjectName : String
        module Projects
          feature Registration
            slice StateChange RegisterProject
              description "Registers a new project"
              command RegisterProject
                projectId ProjectId identifier
                name ProjectName
                produces ProjectRegistered
                  for projectId
                  projectId = projectId
                  name = name
              event ProjectRegistered
                projectId ProjectId
                name ProjectName
        """;

    protected string RootPath = null!;
    protected string IdentitiesPath = null!;
    protected string PendingPath = null!;
    protected string BaselinePath = null!;
    protected ScreenplayWorkspace Original = null!;
    protected byte[] OriginalState = [];
    protected byte[] PersistedCatalog = [];

    void Establish()
    {
        RootPath = SpecFixtureDirectory.Create("model-root");
        File.WriteAllText(Path.Combine(RootPath, "application.play"), Source, new UTF8Encoding(false));
        Original = ScreenplayWorkspace.Create("Projects", new ModelRootDirectory(RootPath).Read(), SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects")));
        Original.Compilation.Success.ShouldBeTrue();
        var metadata = Path.Combine(RootPath, ".screenplay");
        if (OperatingSystem.IsWindows())
        {
            Directory.CreateDirectory(metadata);
        }
        else
        {
            Directory.CreateDirectory(metadata, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }

        IdentitiesPath = Path.Combine(metadata, PersistedIdentityStateSerializer.FileName);
        PendingPath = Path.Combine(metadata, ModelRootMetadata.PendingOperationFileName);
        BaselinePath = Path.Combine(metadata, ReleaseBaselineStore.FileName);
        OriginalState = PersistedIdentityStateSerializer.Serialize(PersistedIdentityStateSerializer.From(Original));
        File.WriteAllBytes(IdentitiesPath, OriginalState);
        PersistedCatalog = SemanticIdentityCatalogSerializer.Serialize(Original.IdentityCatalog);
    }

    protected static byte[] CatalogOf(ScreenplayModelRootOutcome outcome) => SemanticIdentityCatalogSerializer.Serialize(outcome.Workspace!.IdentityCatalog);

    protected void AdmitBaseline()
    {
        var absent = ReleaseBaselineStore.ReadBytes(null, Original.IdentityCatalog.Application);
        var admitted = ReleaseBaseline.Admit(Original, absent, "v1", DateTimeOffset.UnixEpoch).Document!;
        File.WriteAllBytes(BaselinePath, ReleaseBaselineStore.Serialize(admitted));
    }

    protected SortedDictionary<string, string> Snapshot() => new(
        Directory.EnumerateFileSystemEntries(RootPath, "*", SearchOption.AllDirectories).ToDictionary(
            entry => Path.GetRelativePath(RootPath, entry),
            entry => Directory.Exists(entry) ? "directory" : Convert.ToHexString(File.ReadAllBytes(entry))),
        StringComparer.Ordinal);

    void Destroy() => SpecFixtureDirectory.Delete(RootPath);
}
