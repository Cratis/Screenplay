// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceDocument;

public class when_creating_a_utf8_bom_document : Specification
{
    readonly byte[] _source = [.. Encoding.UTF8.Preamble, .. Encoding.UTF8.GetBytes("domain Projects\r\n")];
    WorkspaceDocument _document;

    void Because()
    {
        _document = WorkspaceDocument.Create(
            "register-project-vector",
            PortablePlayPath.Parse("RegisterProject.play"),
            _source);
        _source[^1] = (byte)'x';
    }

    [Fact] void should_create_the_provisional_document_identity() => _document.Id.ShouldEqual(DocumentId.Create("register-project-vector"));
    [Fact] void should_retain_the_stable_key() => _document.StableKey.ShouldEqual("register-project-vector");
    [Fact] void should_retain_the_portable_path() => _document.Path.Value.ShouldEqual("RegisterProject.play");
    [Fact] void should_detect_the_utf8_bom() => _document.Encoding.ShouldEqual(WorkspaceTextEncoding.Utf8WithBom);
    [Fact] void should_decode_text_without_the_bom() => _document.Text.ShouldEqual("domain Projects\r\n");
    [Fact] void should_keep_an_immutable_exact_byte_snapshot() => _document.Bytes[^1].ShouldEqual((byte)'\n');
    [Fact] void should_keep_the_original_bom() => _document.Bytes.Take(Encoding.UTF8.Preamble.Length).ShouldEqual(Encoding.UTF8.Preamble.ToArray());
}
