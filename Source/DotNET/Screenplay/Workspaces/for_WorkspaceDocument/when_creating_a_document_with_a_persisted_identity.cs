// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceDocument;

public class when_creating_a_document_with_a_persisted_identity : Specification
{
    DocumentId _identity;
    WorkspaceDocument _document;

    void Establish() => _identity = DocumentId.Parse("doc1:0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef");

    void Because() => _document = WorkspaceDocument.Create(
        _identity,
        "renamed-document-key",
        PortablePlayPath.Parse("Renamed.play"),
        Encoding.UTF8.GetBytes("domain Projects\n"));

    [Fact] void should_keep_the_persisted_identity() => _document.Id.ShouldEqual(_identity);
    [Fact] void should_not_rederive_identity_from_the_current_key() => _document.Id.ShouldNotEqual(DocumentId.Create(_document.StableKey));
    [Fact] void should_detect_utf8_without_a_bom() => _document.Encoding.ShouldEqual(WorkspaceTextEncoding.Utf8);
}
