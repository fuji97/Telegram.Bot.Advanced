using Xunit.v3;
using Xunit.Sdk;

// Lanes bind sockets and mutate a shared bot's webhook state, so no two integration tests may run concurrently.
[assembly: Parallelization(Mode = ParallelMode.None)]
