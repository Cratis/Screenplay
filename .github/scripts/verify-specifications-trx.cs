// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Xml;
using System.Xml.Linq;

try
{
    if (args.Length != 2) throw new InvalidDataException("Expected a TRX path and invocation start time.");
    var started = DateTimeOffset.FromUnixTimeSeconds(long.Parse(args[1], CultureInfo.InvariantCulture));
    if (!File.Exists(args[0])) throw new InvalidDataException("The native runner produced no TRX.");
    if (File.GetLastWriteTimeUtc(args[0]) < started.UtcDateTime) throw new InvalidDataException("The TRX predates this invocation.");
    using var reader = XmlReader.Create(args[0], new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null });
    var document = XDocument.Load(reader);
    var root = document.Root ?? throw new InvalidDataException("Missing TestRun.");
    XNamespace schema = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010";
    if (root.Name != schema + "TestRun") throw new InvalidDataException("Not a TRX TestRun.");
    var summary = Single(root, schema + "ResultSummary");
    if ((string?)summary.Attribute("outcome") is not ("Completed" or "Passed")) throw new InvalidDataException("The run did not complete successfully.");
    var counters = Single(summary, schema + "Counters");
    var results = Single(root, schema + "Results").Elements(schema + "UnitTestResult").ToArray();
    ValidateCounters(counters, results);
    Console.WriteLine($"Verified native specifications: {results.Length} result rows in {args[0]}");
    return 0;
}
catch (Exception exception) when (exception is IOException or InvalidDataException or XmlException or InvalidOperationException or FormatException or OverflowException or ArgumentException)
{
    Console.Error.WriteLine($"Invalid specification TRX: {exception.Message}");
    return 1;
}

static XElement Single(XElement parent, XName name) =>
    parent.Elements(name).SingleOrDefault() ?? throw new InvalidDataException($"Missing {name.LocalName}.");

static int Counter(XElement counters, string name)
{
    var value = (string?)counters.Attribute(name) ?? throw new InvalidDataException($"Missing counter {name}.");
    return int.Parse(value, NumberStyles.None, CultureInfo.InvariantCulture);
}

static void ValidateCounters(XElement counters, XElement[] results)
{
    var total = Counter(counters, "total");
    var passed = Counter(counters, "passed");
    Require(total > 0, "No specifications were reported.");
    Require(passed > 0, "No specifications actually passed.");
    Require(Counter(counters, "failed") == 0, "Specifications failed.");
    Require(Counter(counters, "executed") == passed, "Executed counter disagrees with successful results.");
    Require(results.Length == total, "Result row count disagrees with total.");
    Require(results.Count(result => (string?)result.Attribute("outcome") == "Passed") == passed, "Passed counter disagrees with results.");
    Require(results.All(result => (string?)result.Attribute("outcome") is "Passed" or "NotExecuted"), "Unexpected result outcomes.");
    ValidateIdentities(results);
    ValidateOtherCounters(counters, total - passed);
}

static void Require(bool condition, string message)
{
    if (!condition) throw new InvalidDataException(message);
}

static void ValidateIdentities(XElement[] results)
{
    var identities = results.Select(result => (string?)result.Attribute("testId")).ToArray();
    Require(identities.All(identity => !string.IsNullOrWhiteSpace(identity)), "Missing result identities.");
    Require(identities.Distinct().Count() == results.Length, "Duplicate result identities.");
}

static void ValidateOtherCounters(XElement counters, int skipped)
{
    foreach (var attribute in counters.Attributes().Where(attribute => attribute.Name.LocalName is not ("total" or "executed" or "passed")))
    {
        var value = int.Parse(attribute.Value, NumberStyles.None, CultureInfo.InvariantCulture);
        if (attribute.Name.LocalName == "notExecuted" ? value != skipped : value != 0)
            throw new InvalidDataException($"Unsuccessful or inconsistent counter {attribute.Name.LocalName}.");
    }
}
