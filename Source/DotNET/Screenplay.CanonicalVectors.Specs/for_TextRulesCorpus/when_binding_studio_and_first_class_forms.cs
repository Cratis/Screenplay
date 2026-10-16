// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_TextRulesCorpus;

public class when_binding_studio_and_first_class_forms : Specification
{
    SemanticCompilation[] _forms;

    void Because() => _forms = [.. TextRulesCorpus.SourceForms.Select(Compile)];

    [Fact] void should_examine_both_source_forms() => _forms.Length.ShouldEqual(2);
    [Fact] void should_select_the_same_text_rule_version() => _forms.ShouldEachConformTo(form => form.Model.SemanticVersion == SemanticVersion.V24);
    [Fact] void should_produce_identical_bytes() => SemanticModelSerializer.Serialize(_forms[0].Model).ShouldEqual(SemanticModelSerializer.Serialize(_forms[1].Model));
    [Fact]
    void should_execute_the_same_accepted_and_refused_specifications()
    {
        foreach (var form in _forms)
        {
            var plan = SemanticExecutionPlan.Compile(form.Model).Plan!;
            var specifications = form.Model.Application.Modules.Single().Features.Single().Slices.Single().Specifications;
            specifications.Length.ShouldEqual(2);
            foreach (var specification in specifications)
            {
                var run = new SemanticSpecificationRunner().Run(plan, specification.Id);
                Assert.True(run.Passed, string.Join('\n', run.Failures));
            }
        }
    }

    static SemanticCompilation Compile(CanonicalCorpusSourceForm form)
    {
        var catalog = SemanticIdentityCatalogSerializer.Deserialize(form.IdentityCatalogBytes.AsSpan());
        var documents = form.Documents.Select(document => SemanticSourceDocument.Create(catalog.ResolveDocument(document.StableKey), document.StableKey, document.DisplayPath, document.Text));
        var result = new SemanticModelCompiler().Compile("TextRules", SemanticDocumentSet.Create([.. documents], catalog));
        Assert.True(result.Success, string.Join('\n', result.Diagnostics.Select(diagnostic => diagnostic.Message)));
        result.Diagnostics.Count(diagnostic => diagnostic.Code == DiagnosticCodes.DeprecatedStudioTextRulePattern).ShouldEqual(form.Name == "studio" ? 2 : 0);

        return result.Value!;
    }
}
