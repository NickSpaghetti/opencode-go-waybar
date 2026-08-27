using Xunit;

// Every test here manipulates the one process table the service reads, so they
// must not overlap: a test that starts an ACP agent would otherwise flip the
// result of a test asserting nothing is running.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
