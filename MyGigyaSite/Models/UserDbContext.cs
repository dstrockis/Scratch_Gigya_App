using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyGigyaSite.Models
{
    class UserDbContext : DbContext
    {
        public UserDbContext() : base("UserDbContext") { }
        public DbSet<AppUser> Users { get; set; }
    }
}
