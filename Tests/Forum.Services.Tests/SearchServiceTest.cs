using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Forum.Data;
using Forum.Data.Models;

namespace Forum.Services.Tests
{
    [TestFixture]
    public class SearchServiceTest
    {
        [SetUp]
        public void Set_Up_Before_Every_Test()
        {
            LoginAsTestUser();
        }

        [TearDown]
        public void TearDown_After_Every_Test()
        {
            CleanUpTestArtifacts();
        }

        [TestCase("truc", 2)]
        [TestCase("machin", 1)]
        [TestCase("bidule", 0)]
        public void Return_Results_Corresponding_To_Query(string query, int expected)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;

            // Arrangement
            using (var context = new ApplicationDbContext(options))
            {
                context.Forums.Add(new Forum { Id = 1 });
                context.Posts.Add(new Post
                {
                    Id = 1,
                    Title = "truc",
                    Content = "ouga",
                    Forum = context.Forums.Find(1)
                });
                context.Posts.Add(new Post
                {
                    Id = -1,
                    Title = "truc",
                    Content = "Péter pue",
                    Forum = context.Forums.Find(1)
                });
                context.Posts.Add(new Post
                {
                    Id = 2,
                    Title = "machin",
                    Content = "banane",
                    Forum = context.Forums.Find(1)
                });

                context.SaveChanges();
            }

            // Action
            using (var context = new ApplicationDbContext(options))
            {
                var postService = new PostService(context);
                var result = postService.GetFilteredPosts("", query, 1).Result;
                var postCount = result.Count();

                // Assertion
                Assert.AreEqual(expected, postCount);
            }
        }

        private void LoginAsTestUser()
        {
            throw new NotImplementedException();
        }

        private void CleanUpTestArtifacts()
        {
            throw new NotImplementedException();
        }
    }
}