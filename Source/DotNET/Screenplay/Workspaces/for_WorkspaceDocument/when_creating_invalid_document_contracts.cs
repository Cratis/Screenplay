// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceDocument;

public class when_creating_invalid_document_contracts : Specification
{
    Exception _defaultIdentity;
    Exception _invalidPersistedKey;
    Exception _invalidProvisionalKey;
    Exception _nullPersistedPath;
    Exception _nullProvisionalPath;

    void Because()
    {
        var bytes = Encoding.UTF8.GetBytes("domain Projects\n");
        var path = PortablePlayPath.Parse("Projects.play");
        var identity = DocumentId.Create("projects");
        _defaultIdentity = Catch.Exception(() => WorkspaceDocument.Create(default, "projects", path, bytes));
        _invalidPersistedKey = Catch.Exception(() => WorkspaceDocument.Create(identity, "../projects", path, bytes));
        _invalidProvisionalKey = Catch.Exception(() => WorkspaceDocument.Create("../projects", path, bytes));
        _nullPersistedPath = Catch.Exception(() => WorkspaceDocument.Create(identity, "projects", null!, bytes));
        _nullProvisionalPath = Catch.Exception(() => WorkspaceDocument.Create("projects", null!, bytes));
    }

    [Fact] void should_reject_every_invalid_contract_consistently() => Exceptions().All(exception => exception is InvalidWorkspaceDocument).ShouldBeTrue();
    [Fact] void should_preserve_the_invalid_key_cause() => _invalidProvisionalKey.InnerException.ShouldBeOfExactType<InvalidSemanticContract>();

    IEnumerable<Exception> Exceptions() =>
    [
        _defaultIdentity,
        _invalidPersistedKey,
        _invalidProvisionalKey,
        _nullPersistedPath,
        _nullProvisionalPath
    ];
}
