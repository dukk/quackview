using Microsoft.Extensions.Logging;
using NSubstitute;
using TypoDukk.QuackView.QuackJob.Services;

namespace TypoDukk.QuackView.QuackJob.Tests.Services;

[TestClass]
public sealed class FileSystemSecretStoreTests
{
    [TestMethod]
    public async Task GetSecret_ExistingKey_ReturnsValue()
    {
        // Arrange
        var logger = Substitute.For<ILogger<FileSystemSecretStore>>();
        var disk = Substitute.For<IDiskIOService>();
        var SpecialPaths = Substitute.For<ISpecialPaths>();
        var store = new FileSystemSecretStore(logger, disk, SpecialPaths);

        SpecialPaths.GetSecretsDirectoryPathAsync().Returns(Path.Combine("quackview-test", "secrets"));

        disk.FileExistsAsync(Path.Combine("quackview-test", "secrets", "MyKey.secret")).Returns(false);

        await store.SetSecretAsync("MyKey", "MyValue");

        disk.FileExistsAsync(Path.Combine("quackview-test", "secrets", "MyKey.secret")).Returns(true);
        disk.ReadAllTextAsync(Path.Combine("quackview-test", "secrets", "MyKey.secret")).Returns("MyValue");

        // Act
        var value = await store.GetSecretAsync("MyKey");

        // Assert
        Assert.AreEqual("MyValue", value);
    }

    [TestMethod]
    public async Task GetSecret_ExistingKeyWithNewLines_ReturnsTrimmedValue()
    {
        // Arrange
        var logger = Substitute.For<ILogger<FileSystemSecretStore>>();
        var disk = Substitute.For<IDiskIOService>();
        var SpecialPaths = Substitute.For<ISpecialPaths>();
        var store = new FileSystemSecretStore(logger, disk, SpecialPaths);

        SpecialPaths.GetSecretsDirectoryPathAsync().Returns(Path.Combine("quackview-test", "secrets"));

        disk.FileExistsAsync(Path.Combine("quackview-test", "secrets", "MyKey.secret")).Returns(true);
        disk.ReadAllTextAsync(Path.Combine("quackview-test", "secrets", "MyKey.secret")).Returns("MyValue\r\n");

        disk.FileExistsAsync(Path.Combine("quackview-test", "secrets", "MyKey2.secret")).Returns(true);
        disk.ReadAllTextAsync(Path.Combine("quackview-test", "secrets", "MyKey2.secret")).Returns("MyValue\n");

        disk.FileExistsAsync(Path.Combine("quackview-test", "secrets", "MyKey3.secret")).Returns(true);
        disk.ReadAllTextAsync(Path.Combine("quackview-test", "secrets", "MyKey3.secret")).Returns("MyValue\t\n");

        // Act
        var value = await store.GetSecretAsync("MyKey");
        var value2 = await store.GetSecretAsync("MyKey2");
        var value3 = await store.GetSecretAsync("MyKey3");

        // Assert
        Assert.AreEqual("MyValue", value);
        Assert.AreEqual("MyValue", value2);
        Assert.AreEqual("MyValue", value3);
    }

    [TestMethod]
    public async Task GetSecret_NonExistingKey_ThrowsKeyNotFoundException()
    {
        // Arrange
        var logger = Substitute.For<ILogger<FileSystemSecretStore>>();
        var disk = Substitute.For<IDiskIOService>();
        var SpecialPaths = Substitute.For<ISpecialPaths>();
        var store = new FileSystemSecretStore(logger, disk, SpecialPaths);

        SpecialPaths.GetSecretsDirectoryPathAsync().Returns(Path.Combine("quackview-test", "secrets"));

        disk.FileExistsAsync(Path.Combine("quackview-test", "secrets", "NonExistingKey.secret")).Returns(false);

        // Act & Assert
        await Assert.ThrowsExceptionAsync<KeyNotFoundException>(async () => await store.GetSecretAsync("NonExistingKey"));
    }

    [TestMethod]
    public async Task SetSecret_AccidentalOverwrite_ThrowsException()
    {
        // Arrange
        var logger = Substitute.For<ILogger<FileSystemSecretStore>>();
        var disk = Substitute.For<IDiskIOService>();
        var SpecialPaths = Substitute.For<ISpecialPaths>();
        var store = new FileSystemSecretStore(logger, disk, SpecialPaths);

        SpecialPaths.GetSecretsDirectoryPathAsync().Returns(Path.Combine("quackview-test", "secrets"));

        disk.FileExistsAsync(Path.Combine("quackview-test", "secrets", "DoNotOverwrite.secret")).Returns(false);

        await store.SetSecretAsync("DoNotOverwrite", "bad");

        disk.FileExistsAsync(Path.Combine("quackview-test", "secrets", "DoNotOverwrite.secret")).Returns(true);

        await Assert.ThrowsExceptionAsync<ArgumentException>(async () => await store.SetSecretAsync("DoNotOverwrite", "bad"));
    }

    [TestMethod]
    public async Task SetSecret_Overwrite()
    {
        // Arrange
        var logger = Substitute.For<ILogger<FileSystemSecretStore>>();
        var disk = Substitute.For<IDiskIOService>();
        var SpecialPaths = Substitute.For<ISpecialPaths>();
        var store = new FileSystemSecretStore(logger, disk, SpecialPaths);

        SpecialPaths.GetSecretsDirectoryPathAsync().Returns(Path.Combine("quackview-test", "secrets"));

        disk.FileExistsAsync(Path.Combine("quackview-test", "secrets", "DoNotOverwrite.secret")).Returns(false);

        await store.SetSecretAsync("DoNotOverwrite", "bad");

        disk.FileExistsAsync(Path.Combine("quackview-test", "secrets", "DoNotOverwrite.secret")).Returns(true);

        await store.SetSecretAsync("DoNotOverwrite", "bad", overwrite: true);
    }

    [TestMethod]
    public async Task SetSecret_InvalidKey()
    {
        // Arrange
        var logger = Substitute.For<ILogger<FileSystemSecretStore>>();
        var disk = Substitute.For<IDiskIOService>();
        var SpecialPaths = Substitute.For<ISpecialPaths>();
        var store = new FileSystemSecretStore(logger, disk, SpecialPaths);

        SpecialPaths.GetSecretsDirectoryPathAsync().Returns(Path.Combine("quackview-test", "secrets"));

        await Assert.ThrowsExceptionAsync<ArgumentException>(async () => await store.SetSecretAsync("no.no.no.no", "bad"));
        await Assert.ThrowsExceptionAsync<ArgumentException>(async () => await store.SetSecretAsync("..\\..\\..\\", "bad"));
        await Assert.ThrowsExceptionAsync<ArgumentException>(async () => await store.SetSecretAsync("!@#$%^&*()", "bad"));
        await Assert.ThrowsExceptionAsync<ArgumentException>(async () => await store.SetSecretAsync("\\/", "bad"));
    }

    [TestMethod]
    public async Task ExpandSecrets_InputWithSecrets_ReplacesSecrets()
    {
        // Arrange
        var logger = Substitute.For<ILogger<FileSystemSecretStore>>();
        var disk = Substitute.For<IDiskIOService>();
        var SpecialPaths = Substitute.For<ISpecialPaths>();
        var store = new FileSystemSecretStore(logger, disk, SpecialPaths);

        SpecialPaths.GetSecretsDirectoryPathAsync().Returns(Path.Combine("quackview-test", "secrets"));

        disk.EnumerateFilesAsync(Path.Combine("quackview-test", "secrets")).Returns(
            new[] {
                Path.Combine("quackview-test", "secrets", "MyKey1.secret"),
                Path.Combine("quackview-test", "secrets", "MyKey2.secret")
            });
        disk.FileExistsAsync(Path.Combine("quackview-test", "secrets", "MyKey1.secret")).Returns(true);
        disk.FileExistsAsync(Path.Combine("quackview-test", "secrets", "MyKey2.secret")).Returns(true);
        disk.ReadAllTextAsync(Path.Combine("quackview-test", "secrets", "MyKey1.secret")).Returns("MyValue1");
        disk.ReadAllTextAsync(Path.Combine("quackview-test", "secrets", "MyKey2.secret")).Returns("MyValue2");

        var input = "This is $^{MyKey1} and this is $^{MyKey2}.";

        // Act
        var result = await store.ExpandSecretsAsync(input);

        // Assert
        Assert.AreEqual("This is MyValue1 and this is MyValue2.", result);
    }

    [TestMethod]
    public async Task ExpandSecrets_InputWithoutSecrets_ReturnsSameString()
    {
        // Arrange
        var logger = Substitute.For<ILogger<FileSystemSecretStore>>();
        var disk = Substitute.For<IDiskIOService>();
        var SpecialPaths = Substitute.For<ISpecialPaths>();
        var store = new FileSystemSecretStore(logger, disk, SpecialPaths);
        var input = "This string has no secrets.";

        SpecialPaths.GetSecretsDirectoryPathAsync().Returns(Path.Combine("quackview-test", "secrets"));

        // Act
        var result = await store.ExpandSecretsAsync(input);
        // Assert
        Assert.AreEqual(input, result);
    }

    [TestMethod]
    public async Task ExpandSecrets_SecretNotFound_ThrowsArgumentException()
    {
        // Arrange
        var logger = Substitute.For<ILogger<FileSystemSecretStore>>();
        var disk = Substitute.For<IDiskIOService>();
        var SpecialPaths = Substitute.For<ISpecialPaths>();
        var store = new FileSystemSecretStore(logger, disk, SpecialPaths);
        var input = "This is $^{MissingKey}.";

        SpecialPaths.GetSecretsDirectoryPathAsync().Returns(Path.Combine("quackview-test", "secrets"));

        // Act & Assert
        await Assert.ThrowsExceptionAsync<KeyNotFoundException>(async () => await store.ExpandSecretsAsync(input));
    }
}