using Autoparts.Models;
using Microsoft.EntityFrameworkCore;

namespace Autoparts
{
    public class Context : DbContext
    {
        public Context(DbContextOptions<Context> options) : base(options)
        {
        }

        public DbSet<Brand> Brands { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Issue> Issues { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<Part> Parts { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Wish> Wishes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configure the one-to-many relationship
            modelBuilder.Entity<Issue>()
                .HasOne(i => i.User)
                .WithMany(u => u.Issues)
                .HasForeignKey(i => i.UserId)
                .OnDelete(DeleteBehavior.Restrict); // Modify cascade behavior here

            base.OnModelCreating(modelBuilder);
        }


        public static void FillDatabase(Context context)
        {
            if (context.Users.Any())
            {
                return;
            }

            var Users = new User[]
            {
                new()
                {
                    Role = Role.Admin,
                    Login = "Dima",
                    Password = "auto",
                }
            };

            context.Users.AddRange(Users);
            context.SaveChanges();
        }
    }
}
