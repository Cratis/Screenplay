// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;
using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Syntax.Serialization;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

public class when_reviewing_exact_unicode_proposal_pages : given.an_authoring_connection
{
    byte[] _original = [];
    byte[] _expected = [];
    byte[] _before = [];
    byte[] _after = [];
    JsonElement _proposal;
    JsonElement _changes;
    ScreenplayWorkspace _exported = null!;
    bool _unchangedBeforeApply;
    int _pageCount;

    void Establish()
    {
        _original = [.. Encoding.UTF8.GetPreamble(), .. Encoding.UTF8.GetBytes(FullSource + "\n// Preserve exact review bytes: café 🚀\n")];
        File.WriteAllBytes(Path.Combine(RootPath, "application.play"), _original);
        Initialize();
    }

    void Because()
    {
        var opened = Open();
        var root = Node("ApplicationSyntax", opened.GetProperty("revision").GetString());
        var replacement = new ScreenplayCompiler().Parse(FullSource.Replace("Registers café projects 🚀", "Registers naïve projects 🌍", StringComparison.Ordinal)).Value;

        // The authoring path restores source positions from the original document after typed JSON admission.
        _expected = [.. Encoding.UTF8.GetPreamble(), .. Encoding.UTF8.GetBytes(new ScreenplayPrinter().Print(replacement))];
        _proposal = Result("propose-ast", new
        {
            expectedRevision = opened.GetProperty("revision").GetString(),
            expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(),
            formatting = "CanonicalizeTouchedDocuments",
            operations = new[] { new { operation = "replace", target = root.GetProperty("handle"), expected = root.GetProperty("node"), node = SyntaxJson.Serialize(replacement) } }
        });
        var proposalId = _proposal.GetProperty("proposalId").GetString();
        var documentId = root.GetProperty("handle").GetProperty("documentId").GetString();
        _changes = Result("read-proposal", new { proposalId, limit = 1 }).GetProperty("result");
        _before = ReadBytes(proposalId, documentId, "before", opened.GetProperty("revision").GetString());
        _after = ReadBytes(proposalId, documentId, "after", _proposal.GetProperty("after").GetProperty("revision").GetString());
        _exported = Candidate(_proposal);
        _unchangedBeforeApply = File.ReadAllBytes(Path.Combine(RootPath, "application.play")).SequenceEqual(_original);
        Apply(opened, _proposal);
    }

    [Fact] void should_return_compact_proposal_metadata() => _proposal.GetProperty("changes").ValueKind.ShouldEqual(JsonValueKind.Null);
    [Fact] void should_page_exactly_one_changed_document() => _changes.GetProperty("totalCount").GetInt32().ShouldEqual(1);
    [Fact] void should_review_more_than_one_byte_page() => _pageCount.ShouldBeGreaterThan(2);
    [Fact] void should_reassemble_exact_before_bytes_including_bom_comments_and_unicode() => _before.SequenceEqual(_original).ShouldBeTrue();
    [Fact] void should_reassemble_exact_canonical_after_bytes_including_bom_and_unicode() => _after.SequenceEqual(_expected).ShouldBeTrue();
    [Fact] void should_export_the_same_candidate_that_was_reviewed() => _exported.Documents.Single().Bytes.SequenceEqual(_after).ShouldBeTrue();
    [Fact] void should_not_write_during_preview_or_export() => _unchangedBeforeApply.ShouldBeTrue();
    [Fact] void should_apply_exactly_the_reviewed_after_bytes() => File.ReadAllBytes(Path.Combine(RootPath, "application.play")).SequenceEqual(_after).ShouldBeTrue();
    [Fact] void should_compile_the_applied_unicode_source() => new McpSnapshot(Root.Read()).Compilation.Success.ShouldBeTrue();

    byte[] ReadBytes(string proposalId, string documentId, string view, string revision)
    {
        var bytes = new List<byte>();
        var offset = 0;
        while (true)
        {
            var content = Result("read-proposal", new { proposalId, documentId, view, offset, limit = 17 }).GetProperty("result").GetProperty("content");
            content.GetProperty("revision").GetString().ShouldEqual(revision);
            content.GetProperty("offset").GetInt32().ShouldEqual(offset);
            var chunk = content.GetProperty("bytesBase64").GetBytesFromBase64();
            chunk.Length.ShouldEqual(content.GetProperty("byteCount").GetInt32());
            bytes.AddRange(chunk);
            _pageCount++;
            if (content.GetProperty("nextOffset").ValueKind == JsonValueKind.Null)
            {
                bytes.Count.ShouldEqual(content.GetProperty("totalBytes").GetInt32());
                break;
            }

            offset = content.GetProperty("nextOffset").GetInt32();
        }

        return [.. bytes];
    }
}
