// Tests for the poll based helper behind the async reader node.

using System.Diagnostics;
using NUnit.Framework;
using VL.Stride.glTF.Core;

namespace VL.Stride.glTF.Tests;

[TestFixture]
public class BackgroundComputationTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    private static bool PollUntilAdopted(BackgroundComputation<string> computation, int hash, out string? result)
    {
        var sw = Stopwatch.StartNew();
        while (sw.Elapsed < Timeout)
        {
            if (computation.Poll(hash, out result, out _, out _))
                return true;
            Thread.Sleep(5);
        }
        result = null;
        return false;
    }

    [Test]
    public void AdoptsResultOnceAndCaches()
    {
        var computation = new BackgroundComputation<string>();

        Assert.That(computation.Poll(1, out _, out bool needsStart, out bool inProgress), Is.False);
        Assert.That(needsStart, Is.True);
        Assert.That(inProgress, Is.False);

        computation.Start(1, () => "one");
        Assert.That(PollUntilAdopted(computation, 1, out var result), Is.True, "timed out");
        Assert.That(result, Is.EqualTo("one"));

        // Same input: the result stays cached. No restart. No new adoption.
        Assert.That(computation.Poll(1, out result, out needsStart, out inProgress), Is.False);
        Assert.That(result, Is.EqualTo("one"));
        Assert.That(needsStart, Is.False);
        Assert.That(inProgress, Is.False);
    }

    [Test]
    public void InputChangeTriggersRestartAndKeepsLastResult()
    {
        var computation = new BackgroundComputation<string>();
        computation.Poll(1, out _, out _, out _);
        computation.Start(1, () => "one");
        PollUntilAdopted(computation, 1, out _);

        // New input: the last result stays available while the new task runs.
        Assert.That(computation.Poll(2, out var result, out bool needsStart, out _), Is.False);
        Assert.That(result, Is.EqualTo("one"));
        Assert.That(needsStart, Is.True);

        computation.Start(2, () => "two");
        Assert.That(PollUntilAdopted(computation, 2, out result), Is.True, "timed out");
        Assert.That(result, Is.EqualTo("two"));
    }

    [Test]
    public void FaultSticksUntilInputChanges()
    {
        var computation = new BackgroundComputation<string>();
        computation.Poll(1, out _, out _, out _);
        computation.Start(1, () => throw new InvalidOperationException("boom"));

        // Wait for the fault to surface.
        var sw = Stopwatch.StartNew();
        Exception? thrown = null;
        while (sw.Elapsed < Timeout && thrown == null)
        {
            try
            {
                computation.Poll(1, out _, out _, out _);
                Thread.Sleep(5);
            }
            catch (Exception e)
            {
                thrown = e;
            }
        }
        Assert.That(thrown, Is.InstanceOf<InvalidOperationException>());

        // Same input: the fault keeps throwing. The task does not restart.
        Assert.That(() => computation.Poll(1, out _, out _, out _), Throws.InvalidOperationException);

        // Changed input: the fault clears and a restart is requested.
        Assert.That(() => computation.Poll(2, out _, out bool needsStart, out _), Throws.Nothing);
        computation.Poll(2, out _, out bool restart, out _);
        Assert.That(restart, Is.True);
    }
}
