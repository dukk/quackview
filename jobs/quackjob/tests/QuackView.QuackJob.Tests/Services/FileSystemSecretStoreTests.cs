using Castle.Core.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Testing.Platform.Services;
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
        var directory = Substitute.For<IDirectoryService>();
        var file = Substitute.For<IFileService>();
        var store = new FileSystemSecretStore(logger, directory, file);

        file.ExistsAsync(Path.Combine(AssemblyLifecycleSetup.QuackViewDir, "secrets", "MyKey.secret")).Returns(false);

        await store.SetSecretAsync("MyKey", "MyValue");

        file.ExistsAsync(Path.Combine(AssemblyLifecycleSetup.QuackViewDir, "secrets", "MyKey.secret")).Returns(true);
        file.ReadAllTextAsync(Path.Combine(AssemblyLifecycleSetup.QuackViewDir, "secrets", "MyKey.secret")).Returns("MyValue");

        // Act
        var value = await store.GetSecretAsync("MyKey");

        // Assert
        Assert.AreEqual("MyValue", value);
    }

    [TestMethod]
    public async Task GetSecret_NonExistingKey_ThrowsKeyNotFoundException()
    {
        // Arrange
        var logger = Substitute.For<ILogger<FileSystemSecretStore>>();
        var directory = Substitute.For<IDirectoryService>();
        var file = Substitute.For<IFileService>();
        var store = new FileSystemSecretStore(logger, directory, file);

        file.ExistsAsync(Path.Combine(AssemblyLifecycleSetup.QuackViewDir, "secrets", "NonExistingKey.secret")).Returns(false);

        // Act & Assert
        await Assert.ThrowsExceptionAsync<KeyNotFoundException>(async () => await store.GetSecretAsync("NonExistingKey"));
    }

    [TestMethod]
    public async Task SetSecret_AccidentalOverwrite_ThrowsException()
    {
        // Arrange
        var logger = Substitute.For<ILogger<FileSystemSecretStore>>();
        var directory = Substitute.For<IDirectoryService>();
        var file = Substitute.For<IFileService>();
        var store = new FileSystemSecretStore(logger, directory, file);

        file.ExistsAsync(Path.Combine(AssemblyLifecycleSetup.QuackViewDir, "secrets", "DoNotOverwrite.secret")).Returns(false);

        await store.SetSecretAsync("DoNotOverwrite", "bad");

        file.ExistsAsync(Path.Combine(AssemblyLifecycleSetup.QuackViewDir, "secrets", "DoNotOverwrite.secret")).Returns(true);

        await Assert.ThrowsExceptionAsync<ArgumentException>(async () => await store.SetSecretAsync("DoNotOverwrite", "bad"));
    }

    [TestMethod]
    public async Task SetSecret_Overwrite()
    {
        // Arrange
        var logger = Substitute.For<ILogger<FileSystemSecretStore>>();
        var directory = Substitute.For<IDirectoryService>();
        var file = Substitute.For<IFileService>();
        var store = new FileSystemSecretStore(logger, directory, file);

        file.ExistsAsync(Path.Combine(AssemblyLifecycleSetup.QuackViewDir, "secrets", "DoNotOverwrite.secret")).Returns(false);

        await store.SetSecretAsync("DoNotOverwrite", "bad");

        file.ExistsAsync(Path.Combine(AssemblyLifecycleSetup.QuackViewDir, "secrets", "DoNotOverwrite.secret")).Returns(true);

        await store.SetSecretAsync("DoNotOverwrite", "bad", overwrite: true);
    }

    [TestMethod]
    public async Task SetSecret_InvalidKey()
    {
        // Arrange
        var logger = Substitute.For<ILogger<FileSystemSecretStore>>();
        var directory = Substitute.For<IDirectoryService>();
        var file = Substitute.For<IFileService>();
        var store = new FileSystemSecretStore(logger, directory, file);

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
        var directory = Substitute.For<IDirectoryService>();
        var file = Substitute.For<IFileService>();
        var store = new FileSystemSecretStore(logger, directory, file);

        directory.EnumerateFilesAsync(Path.Combine(AssemblyLifecycleSetup.QuackViewDir, "secrets")).Returns(
            new[] {
                Path.Combine(AssemblyLifecycleSetup.QuackViewDir, "secrets", "MyKey1.secret"),
                Path.Combine(AssemblyLifecycleSetup.QuackViewDir, "secrets", "MyKey2.secret")
            });
        file.ExistsAsync(Path.Combine(AssemblyLifecycleSetup.QuackViewDir, "secrets", "MyKey1.secret")).Returns(true);
        file.ExistsAsync(Path.Combine(AssemblyLifecycleSetup.QuackViewDir, "secrets", "MyKey2.secret")).Returns(true);
        file.ReadAllTextAsync(Path.Combine(AssemblyLifecycleSetup.QuackViewDir, "secrets", "MyKey1.secret")).Returns("MyValue1");
        file.ReadAllTextAsync(Path.Combine(AssemblyLifecycleSetup.QuackViewDir, "secrets", "MyKey2.secret")).Returns("MyValue2");

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
        var directory = Substitute.For<IDirectoryService>();
        var file = Substitute.For<IFileService>();
        var store = new FileSystemSecretStore(logger, directory, file);
        var input = "This string has no secrets.";
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
        var directory = Substitute.For<IDirectoryService>();
        var file = Substitute.For<IFileService>();
        var store = new FileSystemSecretStore(logger, directory, file);
        var input = "This is $^{MissingKey}.";
        // Act & Assert
        await Assert.ThrowsExceptionAsync<KeyNotFoundException>(async () => await store.ExpandSecretsAsync(input));
    }
}