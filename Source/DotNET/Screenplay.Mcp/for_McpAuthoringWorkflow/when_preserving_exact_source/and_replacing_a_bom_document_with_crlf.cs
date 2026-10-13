// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow.when_preserving_exact_source;

public class and_replacing_a_bom_document_with_crlf : given.an_exact_source_connection
{
    static readonly string Supplied = WithImport.Replace("\n", "\r\n", StringComparison.Ordinal);

    void Establish()
    {
        File.WriteAllText(ApplicationPath, Original.Replace("\n", "\r\n", StringComparison.Ordinal), new UTF8Encoding(true));
        Opened = Open();
    }

    void Because() => Apply(Opened, Result("propose-source", Arguments(ReplaceSource(Supplied), new
    {
        operation = "create-document", path = "Notifications/Notifications.play", stableKey = "notifications", encoding = "Utf8WithBom", source = Notifications.Replace("\n", "\r\n", StringComparison.Ordinal)
    })));

    [Fact] void should_keep_the_original_bom_and_supplied_crlf_without_a_final_newline() => File.ReadAllBytes(ApplicationPath).SequenceEqual([.. Encoding.UTF8.Preamble, .. Encoding.UTF8.GetBytes(Supplied)]).ShouldBeTrue();
    [Fact] void should_use_the_requested_creation_bom_without_changing_the_text() => File.ReadAllBytes(Path.Combine(RootPath, "Notifications/Notifications.play")).SequenceEqual([.. Encoding.UTF8.Preamble, .. Encoding.UTF8.GetBytes(Notifications.Replace("\n", "\r\n", StringComparison.Ordinal))]).ShouldBeTrue();
}
