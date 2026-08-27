using Xunit;

// Every test in this suite manipulates the one process table the module reads,
// so they must not overlap: a test that starts an ACP agent would otherwise
// flip the result of a test asserting the module stays hidden.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
