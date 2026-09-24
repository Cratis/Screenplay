// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Files.for_PlayFileWriter;

public class when_round_tripping_a_commented_workspace : Specification
{
    const string Source = """
        // application annotation
        concept InvoiceId : Uuid // concept annotation
        // module annotation
        module Invoicing
          description "Manages invoices"
          // feature annotation
          feature Invoices
            description "Invoice registration"
            // slice annotation
            slice StateChange Register
              // command annotation
              command Register
                invoiceId InvoiceId identifier
                produces Registered
                  for invoiceId
                  invoiceId = invoiceId // mapping annotation
              // event annotation
              event Registered
                invoiceId InvoiceId
        """;

    DirectoryInfo _root;
    ApplicationCompilation<ApplicationSyntax> _merged;
    string _canonical = string.Empty;
    string _reexpanded = string.Empty;
    string[] _comments = [];
    bool _sameRevision;
    bool _sameIdentities;

    void Establish() => _root = Directory.CreateTempSubdirectory("playworkspace");

    void Because()
    {
        var parser = new ScreenplayCompiler();
        var syntax = parser.Parse(Source).Value!;
        var printer = new ScreenplayPrinter();
        var writer = new PlayFileWriter();
        var single = ScreenplayWorkspace.Create(
            "Invoicing",
            [WorkspaceDocument.Create("single", PortablePlayPath.Parse("application.play"), Encoding.UTF8.GetBytes(Source))],
            SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Invoicing")));
        _canonical = printer.Print(syntax);
        _comments = [.. Source.Split('\n').Where(line => line.Contains("//", StringComparison.Ordinal)).Select(line => line[line.IndexOf("//", StringComparison.Ordinal)..].Trim())];
        var expanded = writer.Expand(syntax).ToArray();
        writer.WriteTo(syntax, _root.FullName);
        _merged = new PlayFileCompiler().CompileFolder(_root.FullName);
        var migrated = SemanticIdentityCatalog.PlanMigration(
            single.IdentityCatalog,
            single.IdentityCatalog.Revision,
            ["single", .. expanded.Skip(1).Select((_, index) => $"document-{index}")],
            [.. single.IdentityCatalog.Semantics.Select(item => item.Address)],
            [.. single.IdentityCatalog.EventContracts.Select(item => item.Address)],
            [],
            [],
            []).Catalog;
        var documents = expanded.Select((file, index) => index == 0
            ? WorkspaceDocument.Create(single.Documents.Single().Id, "single", PortablePlayPath.Parse(file.RelativePath), Encoding.UTF8.GetBytes(file.Content))
            : WorkspaceDocument.Create($"document-{index - 1}", PortablePlayPath.Parse(file.RelativePath), Encoding.UTF8.GetBytes(file.Content)));
        var folder = ScreenplayWorkspace.Create("Invoicing", [.. documents], migrated);
        _sameRevision = single.Compilation.Success && folder.Compilation.Success && single.Compilation.Value!.Model.Revision == folder.Compilation.Value!.Model.Revision;
        _sameIdentities = single.IdentityCatalog.Semantics.Length == folder.IdentityCatalog.Semantics.Length &&
            single.IdentityCatalog.Semantics.All(item => folder.IdentityCatalog.Semantics.Any(other => other.Address.Equals(item.Address) && other.Id == item.Id));
        _reexpanded = string.Join('\n', writer.Expand(_merged.Result.Value!).Select(file => file.Content));
    }

    [Fact] void should_compile_the_folder() => _merged.Result.Success.ShouldBeTrue();
    [Fact] void should_preserve_the_semantic_revision() => _sameRevision.ShouldBeTrue();
    [Fact] void should_preserve_semantic_identities() => _sameIdentities.ShouldBeTrue();
    [Fact] void should_preserve_the_canonical_print() => new ScreenplayPrinter().Print(_merged.Result.Value!).ShouldEqual(_canonical);
    [Fact] void should_keep_every_comment_once() => _comments.All(comment => _reexpanded.Split(comment, StringSplitOptions.None).Length == 2).ShouldBeTrue();

    void Destroy() => _root.Delete(true);
}
