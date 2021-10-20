using NUnit.Framework;

namespace Forum.Services.Tests
{
    [TestFixture]
    public class ForumServiceTest
    {
        [SetUp]
        public void Set_Up_Before_Every_Test()
        {
            // LoginAsTestUser();
        }

        [TearDown]
        public void TearDown_After_Every_Test()
        {
            // CleanUpTestArtifacts();
        }

        [TestCase("truc", 2)]
        [TestCase("machin", 1)]
        [TestCase("bidule", 0)]
        public void Return_Results_Corresponding_To_Id(string query, int expected)
        {
            // Arrangement
            // Action
            // Assertion
        }
    }
}