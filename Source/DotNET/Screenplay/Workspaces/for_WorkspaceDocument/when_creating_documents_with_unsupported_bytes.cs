// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceDocument;

public class when_creating_documents_with_unsupported_bytes : Specification
{
    static readonly byte[][] _bytes =
    [
        [0xc3, 0x28],
        [0xef],
        [0xef, 0xbb],
        [0xff, 0xfe, 0x41, 0x00],
        [0xfe, 0xff, 0x00, 0x41],
        [0xff, 0xfe, 0x00, 0x00, 0x41, 0x00, 0x00, 0x00],
        [0x00, 0x00, 0xfe, 0xff, 0x00, 0x00, 0x00, 0x41],
        [0x2b, 0x2f, 0x76, 0x38, 0x2d]
    ];
    Exception[] _exceptions;

    void Because() => _exceptions =
    [
        .. _bytes.Select((bytes, index) => Catch.Exception(() => WorkspaceDocument.Create(
            $"unsupported-{index}",
            PortablePlayPath.Parse($"Unsupported-{index}.play"),
            bytes)))
    ];

    [Fact] void should_reject_every_unsupported_or_malformed_encoding() => _exceptions.All(exception => exception is InvalidWorkspaceDocument).ShouldBeTrue();
    [Fact] void should_preserve_the_decoder_failure_as_inner_evidence() => _exceptions[0].InnerException.ShouldBeOfExactType<DecoderFallbackException>();
}
