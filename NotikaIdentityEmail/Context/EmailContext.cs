using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NotikaIdentityEmail.Entities;

namespace NotikaIdentityEmail.Context
{
    public class EmailContext:IdentityDbContext<AppUser>
    {
        public DbSet<Category> Categories { get; set; }
        public DbSet <Message> Messages  { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Comment> Comments { get; set; }

    }
}
