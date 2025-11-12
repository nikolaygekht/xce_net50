using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Far.Colorer.RegularExpressions.Internal;
using Far.Colorer.RegularExpressions.Enums;

namespace Far.Colorer.Tests.RegularExpressions;

/// <summary>
/// Stress test for concurrent regex operations to ensure thread safety.
/// This test simulates background syntax highlighting where multiple threads
/// may be compiling and matching patterns simultaneously.
/// </summary>
public class ConcurrencyStressTest
{
    private readonly ITestOutputHelper _output;

    public ConcurrencyStressTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void ConcurrentRegexOperations_10Threads_5Seconds_ShouldSucceed()
    {
        // Arrange: Define 10 different patterns with their test strings
        var testCases = new[]
        {
            (Pattern: @"cat|dog", Input: "I have a cat and a dog", Expected: "cat"),
            (Pattern: @"\w+(?=\d)", Input: "test123", Expected: "test12"), // Greedy \w+ backtracks to where lookahead succeeds
            (Pattern: @"[a-z]+", Input: "hello123world", Expected: "hello"),
            (Pattern: @"\d{3}-\d{4}", Input: "Call 555-1234 now", Expected: "555-1234"),
            (Pattern: @"(foo|bar)+", Input: "foobarfoo", Expected: "foobarfoo"),
            (Pattern: @"\b\w{4}\b", Input: "the quick brown fox", Expected: "quick"),
            (Pattern: @"[A-Z][a-z]+", Input: "Hello World", Expected: "Hello"),
            (Pattern: @"\d+\.\d+", Input: "Price: 19.99", Expected: "19.99"),
            (Pattern: @"(?!test)\w+", Input: "hello test world", Expected: "hello"),
            (Pattern: @"a+b+c+", Input: "xaaabbbcccx", Expected: "aaabbbccc")
        };

        var duration = TimeSpan.FromSeconds(5);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var errors = new System.Collections.Concurrent.ConcurrentBag<Exception>();
        var successCounts = new System.Collections.Concurrent.ConcurrentDictionary<int, int>();
        var failureCounts = new System.Collections.Concurrent.ConcurrentDictionary<int, int>();

        // Pre-compile all patterns once to verify they work
        foreach (var testCase in testCases)
        {
            using var regex = new ColorerRegex(testCase.Pattern, RegexOptions.None);
            var match = regex.Match(testCase.Input);
            if (match == null || match.Value != testCase.Expected)
            {
                throw new InvalidOperationException($"Pre-validation failed for pattern '{testCase.Pattern}': expected '{testCase.Expected}', got '{match?.Value ?? "null"}'");
            }
        }

        // Act: Run 10 threads, each processing one pattern repeatedly for 5 seconds
        var tasks = Enumerable.Range(0, 10).Select(threadId =>
        {
            return Task.Run(() =>
            {
                var testCase = testCases[threadId];
                int successCount = 0;
                int failureCount = 0;

                try
                {
                    while (stopwatch.Elapsed < duration)
                    {
                        try
                        {
                            // Create a new regex instance each time to test compilation thread safety
                            using var regex = new ColorerRegex(testCase.Pattern, RegexOptions.None);
                            var match = regex.Match(testCase.Input);

                            if (match != null && match.Value == testCase.Expected)
                            {
                                successCount++;
                            }
                            else
                            {
                                failureCount++;
                                var actualValue = match?.Value ?? "null";
                                var error = new Exception(
                                    $"Thread {threadId}: Pattern '{testCase.Pattern}' on '{testCase.Input}' " +
                                    $"expected '{testCase.Expected}' but got '{actualValue}'");
                                errors.Add(error);

                                // Don't spam errors, just record first few
                                if (errors.Count > 20)
                                    break;
                            }

                            // Small delay to allow thread interleaving
                            Thread.Sleep(1);
                        }
                        catch (Exception ex)
                        {
                            failureCount++;
                            errors.Add(new Exception($"Thread {threadId}: {ex.Message}", ex));

                            if (errors.Count > 20)
                                break;
                        }
                    }
                }
                finally
                {
                    successCounts[threadId] = successCount;
                    failureCounts[threadId] = failureCount;
                }
            });
        }).ToArray();

        // Wait for all threads to complete
        Task.WaitAll(tasks);
        stopwatch.Stop();

        // Assert: Report results
        _output.WriteLine($"Stress test completed in {stopwatch.Elapsed.TotalSeconds:F2} seconds");
        _output.WriteLine("");

        int totalSuccess = 0;
        int totalFailure = 0;

        for (int i = 0; i < 10; i++)
        {
            int success = successCounts.GetValueOrDefault(i, 0);
            int failure = failureCounts.GetValueOrDefault(i, 0);
            totalSuccess += success;
            totalFailure += failure;

            _output.WriteLine($"Thread {i}: {success} successes, {failure} failures (Pattern: {testCases[i].Pattern})");
        }

        _output.WriteLine("");
        _output.WriteLine($"Total: {totalSuccess} successes, {totalFailure} failures");

        if (errors.Any())
        {
            _output.WriteLine("");
            _output.WriteLine("Errors encountered:");
            foreach (var error in errors.Take(10))
            {
                _output.WriteLine($"  - {error.Message}");
            }

            if (errors.Count > 10)
            {
                _output.WriteLine($"  ... and {errors.Count - 10} more errors");
            }
        }

        // Test passes if we have no failures
        Assert.Empty(errors);
        Assert.Equal(0, totalFailure);
        Assert.True(totalSuccess > 0, "Should have at least some successful matches");
    }

    [Fact]
    public void ConcurrentRegexReuse_SamePatternMultipleThreads_ShouldSucceed()
    {
        // This tests reusing the same compiled regex from multiple threads
        var pattern = @"cat|dog";
        var inputs = new[]
        {
            "I have a cat",
            "I have a dog",
            "I have a catdog",
            "I have cats",
            "I have dogs"
        };

        // Create one regex instance to be shared
        using var regex = new ColorerRegex(pattern, RegexOptions.None);
        var errors = new System.Collections.Concurrent.ConcurrentBag<Exception>();
        var matchCount = 0;

        // Run 100 matches across 10 threads
        var tasks = Enumerable.Range(0, 10).Select(threadId =>
        {
            return Task.Run(() =>
            {
                for (int i = 0; i < 10; i++)
                {
                    try
                    {
                        var input = inputs[i % inputs.Length];
                        var match = regex.Match(input);

                        if (match != null)
                        {
                            Interlocked.Increment(ref matchCount);

                            // Validate the match is actually "cat" or "dog"
                            if (match.Value != "cat" && match.Value != "dog")
                            {
                                errors.Add(new Exception(
                                    $"Thread {threadId}: Invalid match '{match.Value}' in '{input}'"));
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        errors.Add(new Exception($"Thread {threadId}, iteration {i}: {ex.Message}", ex));
                    }
                }
            });
        }).ToArray();

        Task.WaitAll(tasks);

        _output.WriteLine($"Completed {matchCount} matches");

        if (errors.Any())
        {
            _output.WriteLine("Errors:");
            foreach (var error in errors.Take(10))
            {
                _output.WriteLine($"  - {error.Message}");
            }
        }

        Assert.Empty(errors);
        Assert.True(matchCount > 0, "Should have found matches");
    }
}
