using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MiniShare.Models;

namespace MiniShare.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<int>, int>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Post> Posts => Set<Post>();
        public DbSet<PostImage> PostImages => Set<PostImage>();
        public DbSet<Comment> Comments => Set<Comment>();
        public DbSet<Product> Products => Set<Product>();
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<ProductRequest> ProductRequests => Set<ProductRequest>();
        public DbSet<ProductImage> ProductImages => Set<ProductImage>();
        public DbSet<ProductComment> ProductComments => Set<ProductComment>();
        public DbSet<PostLike> PostLikes => Set<PostLike>();
        public DbSet<PostFavorite> PostFavorites => Set<PostFavorite>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Post>()
                .HasOne(p => p.Author)
                .WithMany()
                .HasForeignKey(p => p.AuthorId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<PostImage>()
                .HasOne(i => i.Post)
                .WithMany(p => p.Images)
                .HasForeignKey(i => i.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Comment>()
                .HasOne(c => c.Post)
                .WithMany(p => p.Comments)
                .HasForeignKey(c => c.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ProductRequest>()
                .HasOne(r => r.RequestedBy)
                .WithMany()
                .HasForeignKey(r => r.RequestedById)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ProductRequest>()
                .HasOne(r => r.ReviewedBy)
                .WithMany()
                .HasForeignKey(r => r.ReviewedById)
                .OnDelete(DeleteBehavior.Restrict);

            // ProductImage relationships
            builder.Entity<ProductImage>()
                .HasOne(img => img.Product)
                .WithMany(p => p.Images)
                .HasForeignKey(img => img.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ProductImage>()
                .HasOne(img => img.ProductRequest)
                .WithMany()
                .HasForeignKey(img => img.ProductRequestId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ProductComment>()
                .HasOne(c => c.Product)
                .WithMany(p => p.Comments)
                .HasForeignKey(c => c.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ProductComment>()
                .HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // PostLike relationships
            builder.Entity<PostLike>()
                .HasOne(l => l.Post)
                .WithMany()
                .HasForeignKey(l => l.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<PostLike>()
                .HasOne(l => l.User)
                .WithMany()
                .HasForeignKey(l => l.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // 确保用户对同一帖子只能点赞一次
            builder.Entity<PostLike>()
                .HasIndex(l => new { l.PostId, l.UserId })
                .IsUnique();

            // PostFavorite relationships
            builder.Entity<PostFavorite>()
                .HasOne(f => f.Post)
                .WithMany()
                .HasForeignKey(f => f.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<PostFavorite>()
                .HasOne(f => f.User)
                .WithMany()
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // 确保用户对同一帖子只能收藏一次
            builder.Entity<PostFavorite>()
                .HasIndex(f => new { f.PostId, f.UserId })
                .IsUnique();

            // Comment User relationship
            builder.Entity<Comment>()
                .HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}