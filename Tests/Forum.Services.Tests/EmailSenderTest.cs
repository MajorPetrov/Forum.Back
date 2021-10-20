using NUnit.Framework;

namespace ForumJV.Services.Tests
{
    [TestFixture]
    public class EmailSenderTest
    {
        [TestCase("truc", 2)]
        [TestCase("machin", 1)]
        [TestCase("bidule", 0)]
        public void Return_Results_Corresponding_To_SendEmail(string query, int expected)
        {
            // Arrangement
            // Action
            // Assertion
        }
    }
}