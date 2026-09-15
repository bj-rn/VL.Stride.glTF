// Poll-based background computation helper for the async mesh nodes.
//
// The class runs at most one task at a time.
// When the task finishes, the caller adopts the result.
// If the inputs changed while the task ran, the caller starts a new task with the current
// inputs. This is the latest-wins rule.
// A faulted task keeps its fault. The fault is thrown again on every frame, until the
// inputs change.
//
// The two-call API (Poll, then Start only when needed) keeps the per-frame path free of
// closure allocations.

namespace VL.Stride.glTF.Core;

public sealed class BackgroundComputation<TResult> where TResult : class
{
    private Task<TResult>? task;
    private int taskHash;

    private TResult? result;
    private int resultHash;
    private bool hasResult;

    private Exception? fault;
    private int faultHash;

    /// <summary>
    /// Processes a completed task (if any) and reports the current state for the given
    /// input hash. Returns true when the caller adopted a fresh result this call. When
    /// <paramref name="needsStart"/> is true, the caller must call <see cref="Start"/>
    /// with work that matches <paramref name="inputHash"/>. Throws a pending fault again
    /// for the current inputs.
    /// </summary>
    public bool Poll(int inputHash, out TResult? latestResult, out bool needsStart, out bool inProgress)
    {
        bool adopted = false;

        if (task is { IsCompleted: true })
        {
            var finished = task;
            task = null;
            if (finished.IsFaulted)
            {
                fault = finished.Exception!.GetBaseException();
                faultHash = taskHash;
            }
            else
            {
                result = finished.Result;
                resultHash = taskHash;
                hasResult = true;
                adopted = true;
            }
        }

        if (fault != null && faultHash != inputHash)
            fault = null;

        bool upToDate = hasResult && resultHash == inputHash;
        needsStart = task == null && !upToDate && fault == null;
        inProgress = task != null;
        latestResult = result;

        if (fault != null)
            throw fault;

        return adopted;
    }

    /// <summary>Starts the background work for the given input hash.</summary>
    public void Start(int inputHash, Func<TResult> work)
    {
        taskHash = inputHash;
        task = Task.Run(work);
    }
}
