// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.for_Documentation.given;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_compliance_qualifiers : given.a_semantic_binder
{
    [Theory]
    [InlineData("pii", "pii special health\n  pii criminal", "{\"personal\":{\"special\":\"health\",\"criminal\":true}}")]
    [InlineData("personal", "personal criminal", "{\"personal\":{\"criminal\":true}}")]
    [InlineData("secret", "secret scope namespace", "{\"secret\":{\"scope\":\"namespace\"}}")]
    [InlineData("pii secret", "pii reason \"A note\"", "{\"personal\":{},\"secret\":{}}")]
    [InlineData("pii secret", "secret scope global", "{\"personal\":{},\"secret\":{}}")]
    void should_admit_markers_as_v13_without_reasons_or_personal_secret_scope(string markers, string settings, string compliance)
    {
        var result = Bind($"concept Protected : String {markers}\n  {settings}");
        Assert.True(result.Success, string.Join('\n', result.Diagnostics.Select(diagnostic => diagnostic.Message)));
        result.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V13);
        Encoding.UTF8.GetString(SemanticModelSerializer.Serialize(result.Value.Model)).ShouldContain($"\"compliance\":{compliance}");
    }

    [Fact]
    void should_report_reasons_only() => Bind("concept Protected : String pii\n  pii reason \"A note\"").Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.ReportOnlySemanticSyntax && diagnostic.Message.Contains("reason", StringComparison.Ordinal)).ShouldBeTrue();

    [Fact]
    void should_refuse_compliance_on_enums() => Refuse("concept Protected : Enum pii\n  one\n  two", "Enum concept");

    [Theory]
    [InlineData("concept Protected : Uuid pii\neventsource Record\n  identifier Protected")]
    [InlineData("concept Protected : Uuid secret\neventsource Record\n  stream Details\n    streamId Protected")]
    [InlineData("concept Protected : Uuid pii\nmodule M\n  feature F\n    slice StateChange S\n      command Change\n        id Protected identifier")]
    void should_refuse_compliance_on_identities(string source) => Refuse(source, "surrogate");

    [Theory]
    [InlineData("pii", "")]
    [InlineData("secret", "\n  secret scope subject")]
    void should_refuse_explicit_subject_on_an_event_with_protected_values(string marker, string qualifier) => Refuse($"concept Protected : String {marker}{qualifier}\nmodule M\n  feature F\n    slice StateChange S\n      event Changed\n        id Uuid subject\n        value Protected", "subject lineage");

    [Fact]
    void should_refuse_protected_values_nested_in_collection_elements() => Refuse("concept Protected : String pii\ntype Details\n  value Protected\nmodule M\n  feature F\n    slice StateChange S\n      event Changed\n        id Uuid subject\n        values Details[]", "subject lineage");

    [Fact]
    void should_allow_namespace_secret_with_explicit_subject() => Bind("concept Protected : String secret\n  secret scope namespace\nmodule M\n  feature F\n    slice StateChange S\n      event Changed\n        id Uuid subject\n        value Protected").Success.ShouldBeTrue();

    [Fact]
    void should_preserve_unmarked_v12_bytes_and_revision()
    {
        var root = Directory.GetParent(DocumentationExamples.Root())!.FullName;
        var corpus = Path.Combine(root, "Source/DotNET/Screenplay.CanonicalCorpus/Corpus/EnumMemberNumbers/v12");
        var source = File.ReadAllText(Path.Combine(corpus, "source/declarations.play")) + File.ReadAllText(Path.Combine(corpus, "source/module.play"));
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("EnumMemberNumbers"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("application"), "application", "application.play", source);
        var result = new SemanticModelCompiler().Compile("EnumMemberNumbers", SemanticDocumentSet.Create([document], catalog));
        Assert.True(result.Success, string.Join('\n', result.Diagnostics.Select(diagnostic => diagnostic.Message)));
        var model = result.Value!.Model;
        var baselineBytes = File.ReadAllBytes(Path.Combine(corpus, "expected/esm-v12.json"));
        var baselineRevision = SemanticRevision.Parse(File.ReadAllText(Path.Combine(corpus, "expected/semantic-revision.txt")).Trim());
        model.SemanticVersion.ShouldEqual(SemanticVersion.V12);
        SemanticModelSerializer.Serialize(model).SequenceEqual(baselineBytes).ShouldBeTrue();
        model.Revision.ShouldEqual(baselineRevision);
    }

    void Refuse(string source, string message)
    {
        var result = Bind(source);
        result.Success.ShouldBeFalse();
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == DiagnosticCodes.UnsupportedSemanticSyntax && diagnostic.Message.Contains(message, StringComparison.Ordinal));
    }
}
