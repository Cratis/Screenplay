// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.CanonicalVectors.Specs.for_ClockScheduleVectors.given;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_ClockScheduleVectors;

public class when_loading_the_published_fixture : Specification
{
    byte[] _bytes;
    string _text;
    JsonNode _document;

    void Because()
    {
        _bytes = [.. ClockScheduleVectors.Bytes];
        _text = new UTF8Encoding(false, true).GetString(_bytes);
        _document = JsonNode.Parse(_bytes)!;
    }

    [Fact] void should_match_the_repository_file_byte_for_byte() => _bytes.ShouldEqual(File.ReadAllBytes(ClockVectorExecution.FixturePath()));
    [Fact] void should_match_the_deliberately_pinned_sha256() => Convert.ToHexString(SHA256.HashData(_bytes)).ToLowerInvariant().ShouldEqual(ClockScheduleVectors.Sha256);
    [Fact] void should_publish_the_fixture_version() => _document["version"]!.GetValue<int>().ShouldEqual(ClockScheduleVectors.Version);
    [Fact] void should_use_utf8_without_bom_lf_two_spaces_and_a_trailing_newline() => _text.ShouldEqual(_document.ToJsonString(new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping }).ReplaceLineEndings("\n") + "\n");
}
