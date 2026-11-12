// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Extensions.for_ExtensionContractResolver;

public class when_the_contract_is_not_restored : Specification
{
    static readonly SystemExtensionSyntax Reference = new("cratis/stripe", "2", SourceLocation.Start);

    ExtensionFixture _fixture;

    void Establish() => _fixture = ExtensionFixture.Locked("cratis/stripe", "2", ExtensionContractSample.Stripe, restored: false);

    void Destroy() => _fixture.Dispose();

    [Fact] void should_be_an_error_for_the_compiler() => _fixture.Resolver().Resolve([Reference], _fixture.Lock).Diagnostics.Select(d => (d.Code, d.Severity)).ShouldEqual([(DiagnosticCodes.ExtensionNotRestored, DiagnosticSeverity.Error)]);
    [Fact] void should_say_not_restored_in_an_editor() => _fixture.Resolver(ExtensionResolutionMode.Editor).Resolve([Reference], _fixture.Lock).Diagnostics.Select(d => (d.Code, d.Severity, d.Message)).ShouldEqual([(DiagnosticCodes.ExtensionNotRestored, DiagnosticSeverity.Warning, "Extension 'cratis/stripe' 2.3.1 is not restored.")]);

    [Fact]
    void should_restore_from_the_lock_when_the_source_has_the_package()
    {
        var vendored = Path.Combine(_fixture.Root, "vendored", "ghcr.io", "cratis", "extension-stripe", "2.3.1");
        Directory.CreateDirectory(vendored);
        File.WriteAllText(Path.Combine(vendored, "contract.play"), ExtensionContractSample.Stripe);
        var package = new DirectoryExtensionPackageSource(Path.Combine(_fixture.Root, "vendored")).Resolve("ghcr.io/cratis/extension-stripe", "2")!;
        var locked = new ExtensionLock([_fixture.Lock.Entries[0] with { Digest = package.Digest, ContractHash = ExtensionHashes.Of(package.Contract) }]);
        new ExtensionRestorer(_fixture.Cache, new DirectoryExtensionPackageSource(Path.Combine(_fixture.Root, "vendored"))).RestoreFromLock(locked).ShouldBeEmpty();
        _fixture.Resolver().Resolve([Reference], locked).Diagnostics.ShouldBeEmpty();
    }
}
