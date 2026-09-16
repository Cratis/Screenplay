// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Workspaces.for_ScreenplayWorkspaceSerializer;

public class when_roundtripping_unbindable_source : given.a_transport_workspace
{
    ScreenplayWorkspace _original = null!;
    ScreenplayWorkspace _result = null!;

    void Establish()
    {
        var invalid = WorkspaceDocument.Create(
            Registration.Id,
            Registration.StableKey,
            Registration.Path,
            Encoding.UTF8.GetBytes(RegistrationSource.Replace("ProjectName", "MissingConcept", StringComparison.Ordinal)));
        _original = ScreenplayWorkspace.Create(StableApplicationIdentity, Workspace.ApplicationName, [Concepts, invalid], Workspace.IdentityCatalog);
    }

    void Because() => _result = ScreenplayWorkspaceSerializer.Deserialize(ScreenplayWorkspaceSerializer.Serialize(_original));

    [Fact] void should_not_invent_successful_compilation() => _result.Compilation.Success.ShouldBeFalse();
    [Fact] void should_preserve_compilation_diagnostics() => _result.Compilation.Diagnostics.SequenceEqual(_original.Compilation.Diagnostics).ShouldBeTrue();
    [Fact] void should_preserve_the_revision() => _result.Revision.ShouldEqual(_original.Revision);
    [Fact] void should_preserve_the_exact_catalog() => SemanticIdentityCatalogSerializer.Serialize(_result.IdentityCatalog).SequenceEqual(SemanticIdentityCatalogSerializer.Serialize(_original.IdentityCatalog)).ShouldBeTrue();
    [Fact] void should_preserve_the_invalid_source_bytes() => _result.Documents.Zip(_original.Documents).All(pair => pair.First.Bytes.AsSpan().SequenceEqual(pair.Second.Bytes.AsSpan())).ShouldBeTrue();
}
