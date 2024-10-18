using Microsoft.EntityFrameworkCore;

namespace VMETA_1.Entities
{
    public class SchoolContext : DbContext
    {

        public DbSet<Classroom> Classrooms { get; set; }
        public DbSet<Problem> Problems { get; set; }
        public DbSet<Person> Students { get; set; }
        public DbSet<Decision>  Decisions { get; set; }
        public DbSet<Pool> Pools { get; set; }
        public DbSet<Announcement> Announcements { get; set; }
        public DbSet<Letter> Letters { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(@"Server=(localdb)\MSSQLLocalDB;Database=MetaVanessaServer_1;Trusted_Connection=True;");
        }



    }
}
