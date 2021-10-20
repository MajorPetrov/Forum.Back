using System;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Forum.Data.Models;

namespace Forum.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public DbSet<ApplicationUser> ApplicationUsers { get; set; }
        public DbSet<Forum> Forums { get; set; }
        public DbSet<Post> Posts { get; set; }
        public DbSet<PostReply> PostReplies { get; set; }
        public DbSet<Badge> Badges { get; set; }
        public DbSet<UserBadge> UserBadges { get; set; }
        public DbSet<ArchivedPost> ArchivedPosts { get; set; }
        public DbSet<Poll> Polls { get; set; }
        public DbSet<PollOption> PollOptions { get; set; }
        public DbSet<PollVote> PollVotes { get; set; }
        public DbSet<IdentityUserRole<string>> AspNetUserRoles { get; set; }
        public DbSet<IdentityRole> AspNetRoles { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Remappage des clés primaires pour l'utilisateur pour les rendre compatibles entre l'EntityFrameworkCore et PostgreSQL
            builder.Entity<ApplicationUser>()
                .Property(u => u.Id)
                .HasMaxLength(450);

            builder.Entity<IdentityRole>()
                .Property(r => r.Id)
                .HasMaxLength(450);

            builder.Entity<IdentityUserLogin<string>>()
                .Property(l => l.LoginProvider)
                .HasMaxLength(450);

            builder.Entity<IdentityUserLogin<string>>()
                .Property(l => l.ProviderKey)
                .HasMaxLength(450);

            builder.Entity<IdentityUserToken<string>>()
                .Property(t => t.LoginProvider)
                .HasMaxLength(450);

            builder.Entity<IdentityUserToken<string>>()
                .Property(t => t.Name)
                .HasMaxLength(450);

            // Forçage de la suppression en cascade des réponses à la suppression de post
            builder.Entity<PostReply>()
                .HasOne(reply => reply.Post)
                .WithMany(post => post.Replies)
                .OnDelete(DeleteBehavior.Cascade);

            // Forçage de la suppression en cascade des options à la suppression de poll
            builder.Entity<PollOption>()
                .HasOne(option => option.Poll)
                .WithMany(poll => poll.Options)
                .OnDelete(DeleteBehavior.Cascade);

            // Utilisation de UserId et BadgeId comme coupe de clés primaires
            builder.Entity<UserBadge>()
                .HasKey(badge => new { badge.UserId, badge.BadgeId });

            // Utilisation de PostId comme clé primaire
            builder.Entity<ArchivedPost>()
                .HasKey(post => post.PostId);

            // Utilisation de PostId comme clé primaire
            builder.Entity<Poll>()
                .HasKey(poll => poll.PostId);

            // Utilisation de UserId comme clé primaire
            builder.Entity<PollVote>()
                .HasKey(vote => new { vote.UserId, vote.OptionId });
        }
    }
}