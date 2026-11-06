using Microsoft.EntityFrameworkCore;

namespace CollegeApi
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Role> Roli { get; set; }
        public DbSet<Specialty> Specialnosti { get; set; }
        public DbSet<Group> Gruppy { get; set; }
        public DbSet<Discipline> Discipliny { get; set; }
        public DbSet<ControlType> VidyKontrolya { get; set; }
        public DbSet<User> Polzovateli { get; set; }
        public DbSet<Student> Studenty { get; set; }
        public DbSet<Teacher> Prepodavateli { get; set; }
        public DbSet<Plan> UchebniyPlan { get; set; }
        public DbSet<FinalGrade> Uspevaemost { get; set; }
        public DbSet<CurrentGrade> TekushchieOcenki { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Связи описываем явно: у сущностей нет навигационных свойств,
            // но EF должен знать порядок вставки и удаления строк.
            modelBuilder.Entity<Student>()
                .HasOne<User>().WithMany().HasForeignKey(s => s.id_polzovatelya)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Teacher>()
                .HasOne<User>().WithMany().HasForeignKey(t => t.id_polzovatelya)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Group>()
                .HasOne<Specialty>().WithMany().HasForeignKey(g => g.id_specialnosti)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Plan>()
                .HasOne<Specialty>().WithMany().HasForeignKey(p => p.id_specialnosti)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Plan>()
                .HasOne<Discipline>().WithMany().HasForeignKey(p => p.id_discipliny)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Plan>()
                .HasOne<Teacher>().WithMany().HasForeignKey(p => p.id_prepodavatelya)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Plan>()
                .HasOne<ControlType>().WithMany().HasForeignKey(p => p.id_vida_kontrolya)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<FinalGrade>()
                .HasOne<Student>().WithMany().HasForeignKey(g => g.id_studenta)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<FinalGrade>()
                .HasOne<Plan>().WithMany().HasForeignKey(g => g.id_plana)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CurrentGrade>()
                .HasOne<Student>().WithMany().HasForeignKey(g => g.id_studenta)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CurrentGrade>()
                .HasOne<Plan>().WithMany().HasForeignKey(g => g.id_plana)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
