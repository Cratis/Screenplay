// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Extensions;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.for_Samples;

public class when_binding_the_invoicing_sample_against_its_lock : given.the_samples
{
    static readonly string[] ExtensionCodes =
    [
        DiagnosticCodes.ExtensionNotLocked,
        DiagnosticCodes.ExtensionNotRestored,
        DiagnosticCodes.ExtensionContractHashMismatch,
        DiagnosticCodes.ExtensionLockStale,
        DiagnosticCodes.InvalidExtensionLock,
        DiagnosticCodes.InvalidExtensionContract,
        DiagnosticCodes.UnknownSystemSetting,
        DiagnosticCodes.SecretSettingLiteral,
        DiagnosticCodes.SecretOnPublicSetting
    ];

    string _folder;
    string _source;

    void Establish()
    {
        _folder = _samples.Single(sample => Path.GetFileName(sample) == "Invoicing");
        _source = File.ReadAllText(Path.Combine(_folder, "invoicing.play"));
    }

    string[] Extension(ExtensionContractResolver resolver)
    {
        ExtensionContractResolver.Current = resolver;
        var syntax = new ScreenplayCompiler().Parse(_source).Value!;
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Invoicing"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("invoicing"), "invoicing", "invoicing.play", _source);
        var bound = new SemanticModelBinder().Bind("Invoicing", syntax, SemanticDocumentSet.Create([document], catalog));
        return [.. bound.Diagnostics.Select(diagnostic => diagnostic.Code).Where(ExtensionCodes.Contains)];
    }

    [Fact] void should_read_the_mail_contract_from_the_vendored_folder_without_extension_findings() => Extension(new ExtensionContractResolver(new(Path.Combine(Path.GetTempPath(), "empty-cache")), ExtensionResolutionMode.Strict, root: _folder)).ShouldBeEmpty();
    [Fact] void should_report_the_reference_as_not_locked_without_the_lock() => Extension(new ExtensionContractResolver(new(Path.Combine(Path.GetTempPath(), "empty-cache")), ExtensionResolutionMode.Strict, ExtensionLock.Empty)).ShouldEqual([DiagnosticCodes.ExtensionNotLocked]);
}
