using NUnit.Framework;

namespace ForumJV.Tests.Controllers
{
    [TestFixture]
    public class AccountControllerTest
    {
        [TestCase("truc", 2)]
        [TestCase("machin", 1)]
        [TestCase("bidue", 0)]
        public void Return_Results_Corresponding_To_(string query, int expected)
        {
            // Arrangement
            // Action
            // Assertion
        }
    }
}