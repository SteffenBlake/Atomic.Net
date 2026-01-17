using Xunit;

namespace Atomic.Net.MonoGame.Tests;

[CollectionDefinition("NonParallel", DisableParallelization = true)]
public class NonParallelCollectionDefinition
{
}
