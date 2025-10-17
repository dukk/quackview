namespace TypoDukk.QuackView.QuackJob.Tests;

[TestClass]
public class ArgumentGuardExtensionsTests
{
    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public void NotNullOrWhiteSpace_Throws_On_Null()
    {
        string? test = null;
        test.NotNullOrWhiteSpace(nameof(test));
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public void NotNullOrWhiteSpace_Throws_On_Empty()
    {
        string test = string.Empty;
        test.NotNullOrWhiteSpace(nameof(test));
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public void NotNullOrWhiteSpace_Throws_On_Whitespace()
    {
        string test = "   ";
        test.NotNullOrWhiteSpace(nameof(test));
    }

    [TestMethod]
    public void NotNullOrWhiteSpace_Does_Not_Throw_On_Valid_String()
    {
        string test = "Valid String";
        test.NotNullOrWhiteSpace(nameof(test));
    }

    [TestMethod]
    public void NotNullOrWhiteSpace_Does_Not_Throw_On_Valid_String_With_Whitespace()
    {
        string test = "  Valid String  ";
        test.NotNullOrWhiteSpace(nameof(test));
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public void NotNullOrEmpty_Throws_On_Null_Array()
    {
        string[]? test = null;
        test.NotNullOrEmpty(nameof(test));
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public void NotNullOrEmpty_Throws_On_Empty_Array()
    {
        string[] test = Array.Empty<string>();
        test.NotNullOrEmpty(nameof(test));
    }

    // Not sure this one is really useful, we should be checking individual strings later on.
    // [TestMethod]
    // [ExpectedException(typeof(ArgumentNullException))]
    // public void NotNullOrEmpty_Throws_On_Whitespace_Array()
    // {
    //     string[] test = new string[] { "   " };
    //     test.NotNullOrEmpty(nameof(test));
    // }

    [TestMethod]
    public void NotNullOrEmpty_Does_Not_Throw_On_Valid_Array()
    {
        string[] test = new string[] { "Valid String" };
        test.NotNullOrEmpty(nameof(test));
    }

    [TestMethod]
    public void NotNullOrEmpty_Does_Not_Throw_On_Valid_Array_With_Whitespace()
    {
        string[] test = new string[] { "  Valid String  " };
        test.NotNullOrEmpty(nameof(test));
    }

    [TestMethod]
    public void EnsureBefore_Does_Not_Throw_When_Start_Is_Before_End()
    {
        DateTime start = new DateTime(2024, 1, 1);
        DateTime end = new DateTime(2024, 12, 31);
        start.EnsureBefore(end);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void EnsureBefore_Throws_When_Start_Is_After_End()
    {
        DateTime start = new DateTime(2024, 12, 31);
        DateTime end = new DateTime(2024, 1, 1);
        start.EnsureBefore(end);
    }
}