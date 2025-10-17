using TypoDukk.QuackView.QuackJob.Services;

namespace TypoDukk.QuackView.QuackJob.Tests.Services;

[TestClass]
public sealed class SecretStoreTests
{
    [TestMethod]
    public void GetSecret_ExistingKey_ReturnsValue()
    {
        // Arrange
        var store = new SecretStore();
        store.SetSecret("MyKey", "MyValue");
        // Act
        var value = store.GetSecret("MyKey");
        // Assert
        Assert.AreEqual("MyValue", value);
    }

    [TestMethod]
    public void GetSecret_NonExistingKey_ThrowsKeyNotFoundException()
    {
        // Arrange
        var store = new SecretStore();
        // Act & Assert
        Assert.ThrowsException<KeyNotFoundException>(() => store.GetSecret("NonExistingKey"));
    }

    [TestMethod]
    public void ExpandSecrets_InputWithSecrets_ReplacesSecrets()
    {
        // Arrange
        var store = new SecretStore();
        store.SetSecret("Key1", "Value1");
        store.SetSecret("Key2", "Value2");
        var input = "This is ${Key1} and this is ${Key2}.";
        // Act
        var result = store.ExpandSecrets(input);
        // Assert
        Assert.AreEqual("This is Value1 and this is Value2.", result);
    }

    [TestMethod]
    public void ExpandSecrets_InputWithoutSecrets_ReturnsSameString()
    {
        // Arrange
        var store = new SecretStore();
        var input = "This string has no secrets.";
        // Act
        var result = store.ExpandSecrets(input);
        // Assert
        Assert.AreEqual(input, result);
    }

    [TestMethod]
    public void ExpandSecrets_SecretNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var store = new SecretStore();
        var input = "This is ${MissingKey}.";
        // Act & Assert
        Assert.ThrowsException<KeyNotFoundException>(() => store.ExpandSecrets(input));
    }
}