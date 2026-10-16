// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.CanonicalVectors.Specs.for_ProtectedCommandReadRaceVectors.given;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_ProtectedCommandReadRaceVectors;

public class when_loading_the_published_fixture : Specification
{
    byte[] _bytes;
    string _text;
    JsonNode _document;

    void Establish()
    {
        if (Environment.GetEnvironmentVariable(RaceVectorExecution.RegenerationVariable) == "1") RaceVectorExecution.Regenerate();
    }

    void Because()
    {
        _bytes = [.. ProtectedCommandReadRaceVectors.Bytes];
        _text = new UTF8Encoding(false, true).GetString(_bytes);
        _document = JsonNode.Parse(_bytes)!;
    }

    [Fact] void should_match_the_repository_file_byte_for_byte() => _bytes.ShouldEqual(File.ReadAllBytes(RaceVectorExecution.ResourcePath("races.v1.json")));
    [Fact] void should_match_the_deliberately_pinned_sha256() => Convert.ToHexString(SHA256.HashData(_bytes)).ToLowerInvariant().ShouldEqual(ProtectedCommandReadRaceVectors.Sha256);
    [Fact] void should_publish_the_fixture_version() => _document["version"]!.GetValue<int>().ShouldEqual(ProtectedCommandReadRaceVectors.Version);
    [Fact] void should_use_utf8_without_bom_lf_two_spaces_and_a_trailing_newline() => _text.ShouldEqual(RaceVectorExecution.Format(_document));
}
