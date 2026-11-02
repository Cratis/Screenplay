// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpWorkspaces;

public class when_scoping_attachment_manifests_to_a_request : for_McpConnection.given.a_connection
{
    McpWorkspaces _workspaces = null!;
    McpAttachmentContents.Request? _first;
    McpAttachmentContents.Request? _second;

    void Establish()
    {
        _workspaces = new(Root);
        _workspaces.Open(McpJson.Empty);
    }

    void Because()
    {
        _workspaces.WithReleaseBaseline("propose-source", McpJson.Empty, () =>
        {
            _first = _workspaces.AttachmentRequest;
            return new object();
        });
        _workspaces.WithReleaseBaseline("propose-source", McpJson.Empty, () =>
        {
            _second = _workspaces.AttachmentRequest;
            return new object();
        });
    }

    [Fact] void should_create_a_manifest_scope_for_a_proposal() => _first.ShouldNotBeNull();
    [Fact] void should_not_share_manifests_between_requests() => ReferenceEquals(_first, _second).ShouldBeFalse();
    [Fact] void should_release_the_scope_after_the_call() => _workspaces.AttachmentRequest.ShouldBeNull();

    [Fact]
    void should_release_the_scope_when_the_call_throws()
    {
        Catch.Exception(() => _workspaces.WithReleaseBaseline("propose-source", McpJson.Empty, () => throw new InvalidOperationException())).ShouldNotBeNull();
        _workspaces.AttachmentRequest.ShouldBeNull();
    }

    [Fact]
    void should_release_the_scope_when_the_baseline_pin_is_refused()
    {
        var arguments = JsonSerializer.SerializeToElement(new { expectedReleaseBaselineRevision = "stale" });
        Catch.Exception(() => _workspaces.WithReleaseBaseline("propose-source", arguments, () => new object())).ShouldNotBeNull();
        _workspaces.AttachmentRequest.ShouldBeNull();
    }

    [Fact]
    void should_release_the_scope_when_opening_the_current_workspace_fails()
    {
        var unopened = new McpWorkspaces(Root);
        Catch.Exception(() => unopened.WithReleaseBaseline("propose-source", McpJson.Empty, () => new object())).ShouldNotBeNull();
        unopened.AttachmentRequest.ShouldBeNull();
    }

    [Fact]
    void should_not_enable_reuse_for_other_tools()
    {
        _workspaces.WithReleaseBaseline("read-workspace", McpJson.Empty, () =>
        {
            _workspaces.AttachmentRequest.ShouldBeNull();
            return new object();
        });
    }

    [Fact]
    async Task should_isolate_overlapping_requests_even_when_the_execution_context_is_inherited()
    {
        using var overlap = new Barrier(2);
        var enter = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        McpAttachmentContents.Request? outer = null;
        var pending = (Task<object[]>)_workspaces.WithReleaseBaseline("propose-source", McpJson.Empty, () =>
        {
            outer = _workspaces.AttachmentRequest;
            return Task.WhenAll(Enumerable.Range(0, 2).Select(_ => Task.Run(
                async () =>
                {
                    await enter.Task;
                    return _workspaces.WithReleaseBaseline("propose-source", McpJson.Empty, () =>
                    {
                        if (!overlap.SignalAndWait(TimeSpan.FromSeconds(30), TestContext.Current.CancellationToken)) throw new TimeoutException();
                        return _workspaces.AttachmentRequest!;
                    });
                },
                TestContext.Current.CancellationToken)));
        });
        enter.SetResult();
        var requests = await pending;
        ReferenceEquals(requests[0], requests[1]).ShouldBeFalse();
        ReferenceEquals(requests[0], outer).ShouldBeFalse();
        ReferenceEquals(requests[1], outer).ShouldBeFalse();
        _workspaces.AttachmentRequest.ShouldBeNull();
    }
}
