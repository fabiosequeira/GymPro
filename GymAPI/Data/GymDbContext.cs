using Microsoft.EntityFrameworkCore;
using GymAPI.Models;

namespace GymAPI.Data;

public class GymDbContext : DbContext
{
    public GymDbContext(DbContextOptions<GymDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Member> Members => Set<Member>();
    public DbSet<Trainer> Trainers => Set<Trainer>();
    public DbSet<Plan> Plans => Set<Plan>();
    public DbSet<GymClass> GymClasses => Set<GymClass>();
    public DbSet<ClassEnrollment> ClassEnrollments => Set<ClassEnrollment>();
    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Member>()
            .HasOne(m => m.User).WithOne().HasForeignKey<Member>(m => m.UserId);

        modelBuilder.Entity<Trainer>()
            .HasOne(t => t.User).WithOne().HasForeignKey<Trainer>(t => t.UserId);

        modelBuilder.Entity<ClassEnrollment>()
            .HasOne(e => e.Member).WithMany(m => m.Enrollments).HasForeignKey(e => e.MemberId);

        modelBuilder.Entity<ClassEnrollment>()
            .HasOne(e => e.GymClass).WithMany(c => c.Enrollments).HasForeignKey(e => e.GymClassId);

        modelBuilder.Entity<Plan>()
            .Property(p => p.Price).HasColumnType("decimal(10,2)");

        modelBuilder.Entity<Payment>()
            .Property(p => p.Amount).HasColumnType("decimal(10,2)");
    }
}

public static class DbSeeder
{
    public static void Seed(GymDbContext db)
    {
        if (db.Users.Any()) return;

        // Plans
        var plans = new List<Plan>
        {
            new() { Name = "Básico", Description = "Acesso ao ginásio + 4 aulas/mês", Price = 25, DurationMonths = 1, MaxClassesPerMonth = 4, IncludesPersonalTrainer = false },
            new() { Name = "Standard", Description = "Acesso ao ginásio + 10 aulas/mês", Price = 45, DurationMonths = 1, MaxClassesPerMonth = 10, IncludesPersonalTrainer = false },
            new() { Name = "Premium", Description = "Acesso ilimitado + Personal Trainer", Price = 80, DurationMonths = 1, MaxClassesPerMonth = 999, IncludesPersonalTrainer = true },
        };
        db.Plans.AddRange(plans);
        db.SaveChanges();

        // Admin user
        var adminUser = new User { Name = "Admin", Email = "admin@gym.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"), Role = "Admin" };
        db.Users.Add(adminUser);
        db.SaveChanges();

        // Trainers
        var trainerUser1 = new User { Name = "Carlos Silva", Email = "carlos@gym.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("trainer123"), Role = "Trainer" };
        var trainerUser2 = new User { Name = "Ana Costa", Email = "ana@gym.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("trainer123"), Role = "Trainer" };
        db.Users.AddRange(trainerUser1, trainerUser2);
        db.SaveChanges();

        var trainer1 = new Trainer { UserId = trainerUser1.Id, Specialization = "Yoga & Pilates", Bio = "10 anos de experiência em yoga terapêutico." };
        var trainer2 = new Trainer { UserId = trainerUser2.Id, Specialization = "Spinning & Cardio", Bio = "Especialista em treino cardiovascular de alta intensidade." };
        db.Trainers.AddRange(trainer1, trainer2);
        db.SaveChanges();

        // Members
        var memberUser1 = new User { Name = "João Ferreira", Email = "joao@email.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("member123"), Role = "Member" };
        var memberUser2 = new User { Name = "Maria Santos", Email = "maria@email.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("member123"), Role = "Member" };
        db.Users.AddRange(memberUser1, memberUser2);
        db.SaveChanges();

        db.Members.AddRange(
            new Member { UserId = memberUser1.Id, Phone = "910000001", BirthDate = new DateTime(1990, 5, 10), PlanId = plans[1].Id, PlanStartDate = DateTime.UtcNow, PlanEndDate = DateTime.UtcNow.AddMonths(1) },
            new Member { UserId = memberUser2.Id, Phone = "910000002", BirthDate = new DateTime(1995, 8, 22), PlanId = plans[2].Id, PlanStartDate = DateTime.UtcNow, PlanEndDate = DateTime.UtcNow.AddMonths(1) }
        );
        db.SaveChanges();

        // Classes
        var now = DateTime.UtcNow;
        db.GymClasses.AddRange(
            new GymClass { Name = "Yoga Matinal", Description = "Yoga para iniciantes", TrainerId = trainer1.Id, StartTime = now.AddDays(1).Date.AddHours(8), EndTime = now.AddDays(1).Date.AddHours(9), MaxCapacity = 15, Room = "Sala A" },
            new GymClass { Name = "Spinning Intensivo", Description = "Cardio de alta intensidade", TrainerId = trainer2.Id, StartTime = now.AddDays(1).Date.AddHours(18), EndTime = now.AddDays(1).Date.AddHours(19), MaxCapacity = 20, Room = "Sala B" },
            new GymClass { Name = "Pilates", Description = "Fortalecimento e flexibilidade", TrainerId = trainer1.Id, StartTime = now.AddDays(2).Date.AddHours(10), EndTime = now.AddDays(2).Date.AddHours(11), MaxCapacity = 12, Room = "Sala A" }
        );
        db.SaveChanges();
    }
}
