using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.Json;
using LocalErp.Infrastructure.Security;

const string BenchmarkPassword = "LocalErp-Benchmark-Password-2026!";

var options = BenchmarkOptions.Parse(args);
var hasher = new MicrosoftPasswordHashService(options.IterationCount);
var referenceHash = hasher.Hash(BenchmarkPassword);

for (var index = 0; index < options.WarmupCount; index++)
{
    _ = hasher.Hash(BenchmarkPassword);
    _ = hasher.Verify(referenceHash, BenchmarkPassword);
    _ = hasher.Verify(referenceHash, BenchmarkPassword + "x");
}

var hashSamples = Measure(options.SampleCount, () => hasher.Hash(BenchmarkPassword));
var verifySuccessSamples = Measure(options.SampleCount, () => hasher.Verify(referenceHash, BenchmarkPassword));
var verifyFailureSamples = Measure(options.SampleCount, () => hasher.Verify(referenceHash, BenchmarkPassword + "x"));

var report = new BenchmarkReport(
    DateTime.UtcNow,
    RuntimeInformation.OSDescription,
    RuntimeInformation.FrameworkDescription,
    RuntimeInformation.ProcessArchitecture.ToString(),
    Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER") ?? "unknown",
    Environment.ProcessorCount,
    System.Runtime.GCSettings.IsServerGC,
    options.IterationCount,
    options.WarmupCount,
    options.SampleCount,
    Summarize(hashSamples),
    Summarize(verifySuccessSamples),
    Summarize(verifyFailureSamples));

Console.WriteLine(JsonSerializer.Serialize(report, new JsonSerializerOptions
{
    WriteIndented = true,
}));

static double[] Measure(int sampleCount, Action operation)
{
    var samples = new double[sampleCount];

    for (var index = 0; index < sampleCount; index++)
    {
        var started = Stopwatch.GetTimestamp();
        operation();
        samples[index] = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
    }

    return samples;
}

static BenchmarkStatistics Summarize(double[] values)
{
    var ordered = values.Order().ToArray();
    return new BenchmarkStatistics(
        Math.Round(ordered.Min(), 3),
        Math.Round(Percentile(ordered, 0.50), 3),
        Math.Round(ordered.Average(), 3),
        Math.Round(Percentile(ordered, 0.95), 3),
        Math.Round(ordered.Max(), 3));
}

static double Percentile(double[] orderedValues, double percentile)
{
    if (orderedValues.Length == 0)
    {
        throw new ArgumentException("At least one benchmark sample is required.", nameof(orderedValues));
    }

    var rank = Math.Ceiling(percentile * orderedValues.Length) - 1;
    var index = Math.Clamp((int)rank, 0, orderedValues.Length - 1);
    return orderedValues[index];
}

internal sealed record BenchmarkReport(
    DateTime MeasuredAtUtc,
    string OperatingSystem,
    string Framework,
    string ProcessArchitecture,
    string ProcessorIdentifier,
    int ProcessorCount,
    bool ServerGarbageCollection,
    int IterationCount,
    int WarmupCount,
    int SampleCount,
    BenchmarkStatistics Hash,
    BenchmarkStatistics VerifySuccess,
    BenchmarkStatistics VerifyFailure);

internal sealed record BenchmarkStatistics(
    double MinimumMilliseconds,
    double MedianMilliseconds,
    double AverageMilliseconds,
    double P95Milliseconds,
    double MaximumMilliseconds);

internal sealed record BenchmarkOptions(int IterationCount, int WarmupCount, int SampleCount)
{
    public static BenchmarkOptions Parse(string[] arguments)
    {
        var iterations = MicrosoftPasswordHashService.MinimumIterationCount;
        var warmup = 2;
        var samples = 20;

        for (var index = 0; index < arguments.Length; index++)
        {
            switch (arguments[index])
            {
                case "--iterations":
                    iterations = ReadPositiveInt(arguments, ref index, "--iterations");
                    break;
                case "--warmup":
                    warmup = ReadNonNegativeInt(arguments, ref index, "--warmup");
                    break;
                case "--samples":
                    samples = ReadPositiveInt(arguments, ref index, "--samples");
                    break;
                default:
                    throw new ArgumentException(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "Unknown benchmark argument '{0}'.",
                            arguments[index]));
            }
        }

        return new BenchmarkOptions(iterations, warmup, samples);
    }

    private static int ReadPositiveInt(string[] arguments, ref int index, string optionName)
    {
        var value = ReadInt(arguments, ref index, optionName);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value, optionName);
        return value;
    }

    private static int ReadNonNegativeInt(string[] arguments, ref int index, string optionName)
    {
        var value = ReadInt(arguments, ref index, optionName);
        ArgumentOutOfRangeException.ThrowIfNegative(value, optionName);
        return value;
    }

    private static int ReadInt(string[] arguments, ref int index, string optionName)
    {
        if (++index >= arguments.Length ||
            !int.TryParse(arguments[index], NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
        {
            throw new ArgumentException($"Option '{optionName}' requires an integer value.", optionName);
        }

        return value;
    }
}
