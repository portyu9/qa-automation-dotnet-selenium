using UiTests.Framework.Testing;
using Xunit;

namespace UiTests.Tests.Fixtures;

[CollectionDefinition("local-ui")]
public sealed class LocalUiCollection : ICollectionFixture<LocalUiServer>
{
    public const string Name = "local-ui";
}
