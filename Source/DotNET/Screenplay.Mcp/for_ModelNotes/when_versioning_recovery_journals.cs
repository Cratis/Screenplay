// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_ModelNotes;

public class when_versioning_recovery_journals : given.a_notes_workspace
{
    McpRecoveryRecord _record = null!;

    void Establish()
    {
        var proposal = new McpNotesProposal(Original, new(NotesBytes, NotesBytes, ModelNotesStore.Revision(NotesBytes), ModelNotesStore.Revision(NotesBytes)));
        _record = McpRecoveryJournal.Prepare(Root, proposal, new(OriginalState, OriginalState)).Record;
    }

    [Fact] void should_write_version_two_for_notes() => _record.Version.ShouldEqual(2);
    [Fact] void should_be_refused_by_a_version_one_only_reader() => OlderReaderAccepts(Files.Read(McpRecoveryJournal.FileName)!).ShouldBeFalse();

    [Fact]
    void should_write_version_one_for_source_only_operations()
    {
        new McpRecovery(Root, McpRecoveryJournal.Load(Root)!).Rollback();
        Prepare().Record.Version.ShouldEqual(1);
    }

    [Fact]
    void should_recover_version_one_without_notes()
    {
        ReplaceMarker(_record with { Version = 1, Notes = null });
        new McpRecovery(Root, McpRecoveryJournal.Load(Root)!).Rollback();
        Files.Read(McpRecoveryJournal.FileName).ShouldBeNull();
        McpManagedFiles.Equal(Files.Read(ModelNotesStore.FileName), NotesBytes).ShouldBeTrue();
    }

    [Fact]
    void should_refuse_version_one_carrying_notes()
    {
        ReplaceMarker(_record with { Version = 1 });
        Catch.Exception(() => McpRecoveryJournal.Load(Root)).ShouldBeOfExactType<McpFailure>();
        Files.Read(McpRecoveryJournal.FileName).ShouldNotBeNull();
    }

    [Fact]
    void should_refuse_version_two_without_notes()
    {
        ReplaceMarker(_record with { Notes = null });
        Catch.Exception(() => McpRecoveryJournal.Load(Root)).ShouldBeOfExactType<McpFailure>();
    }

    [Fact]
    void should_refuse_inconsistent_version_two_notes()
    {
        ReplaceMarker(_record with { Notes = _record.Notes! with { After = [123] } });
        Catch.Exception(() => McpRecoveryJournal.Load(Root)).ShouldBeOfExactType<McpFailure>();
    }

    static bool OlderReaderAccepts(byte[] bytes) => JsonSerializer.Deserialize<OlderEnvelope>(bytes)!.Version == 1;

    void ReplaceMarker(McpRecoveryRecord record) => File.WriteAllBytes(Files.PathFor(McpRecoveryJournal.FileName), JsonSerializer.SerializeToUtf8Bytes(record));

    sealed record OlderEnvelope(int Version);
}
