// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_paging_typed_contexts_with_specification_refusals : given.a_connection
{
    JsonElement _workspace;
    JsonElement _proposal;
    bool _hasOptInContexts;

    void Because()
    {
        var source = Source.Replace("        produces ProjectRegistered", "        validate csharp\n          ```\n          return true;\n          ```\n        produces ProjectRegistered", StringComparison.Ordinal) + """

                  specification Refused
                    when RegisterProject
                      projectId = "d2def216-9717-4672-9fc4-a2112455c9fc"
                    then ProjectRegistered
                      projectId = "d2def216-9717-4672-9fc4-a2112455c9fc"
                      name = "Project"
            """;
        File.WriteAllText(Path.Combine(RootPath, "application.play"), source);
        var compilation = Workspace().Compilation;
        _hasOptInContexts = !compilation.Success && compilation.SpecificationRefusedValue is not null && compilation.SpecificationRefusedTypedContextDescriptors.Any(descriptor => descriptor.IsWrapperReady);
        Initialize();
        var opened = Call("open-workspace", new { applicationName = "Projects" }).GetProperty("result").GetProperty("structuredContent");
        var revision = opened.GetProperty("revision").GetString();
        _workspace = Call("read-workspace", new { expectedRevision = revision, view = "typed-contexts" }).GetProperty("result").GetProperty("structuredContent");
        var documentId = Call("read-workspace", new { expectedRevision = revision }).GetProperty("result").GetProperty("structuredContent").GetProperty("page").GetProperty("items")[0].GetProperty("documentId").GetString();
        var proposal = Call("propose-ast", new
        {
            expectedRevision = revision,
            expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(),
            formatting = "CanonicalizeTouchedDocuments",
            validation = "Authoring",
            documents = new[] { new { operation = "replace-document", documentId, node = SyntaxJson.Serialize(new ScreenplayCompiler().Parse(source + "\n").Value!) } }
        }).GetProperty("result").GetProperty("structuredContent");
        _proposal = Call("read-proposal", new { proposalId = proposal.GetProperty("proposalId").GetString(), view = "typed-contexts" }).GetProperty("result").GetProperty("structuredContent").GetProperty("result");
    }

    [Fact] void should_have_wrapper_ready_contexts_only_on_the_opt_in_path() => _hasOptInContexts.ShouldBeTrue();
    [Fact] void should_keep_workspace_contexts_unavailable() => _workspace.GetProperty("available").GetBoolean().ShouldBeFalse();
    [Fact] void should_keep_workspace_failure_descriptors() => _workspace.GetProperty("page").GetProperty("totalCount").GetInt32().ShouldEqual(0);
    [Fact] void should_keep_proposal_contexts_unavailable() => _proposal.GetProperty("available").GetBoolean().ShouldBeFalse();
    [Fact] void should_keep_proposal_failure_descriptors() => _proposal.GetProperty("page").GetProperty("totalCount").GetInt32().ShouldEqual(0);
}
