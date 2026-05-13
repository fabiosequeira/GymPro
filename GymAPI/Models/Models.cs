namespace GymAPI.Models;

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = "Member"; // Admin, Trainer, Member
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}

public class Member
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public string Phone { get; set; } = string.Empty;
    public DateTime BirthDate { get; set; }
    public int PlanId { get; set; }
    public Plan Plan { get; set; } = null!;
    public DateTime PlanStartDate { get; set; } = DateTime.UtcNow;
    public DateTime PlanEndDate { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<ClassEnrollment> Enrollments { get; set; } = new List<ClassEnrollment>();
}

public class Trainer
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public string Specialization { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public ICollection<GymClass> Classes { get; set; } = new List<GymClass>();
}

public class Plan
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;       // Básico, Standard, Premium
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int DurationMonths { get; set; }
    public int MaxClassesPerMonth { get; set; }
    public bool IncludesPersonalTrainer { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<Member> Members { get; set; } = new List<Member>();
}

public class GymClass
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;       // Yoga, Spinning, Pilates…
    public string Description { get; set; } = string.Empty;
    public int TrainerId { get; set; }
    public Trainer Trainer { get; set; } = null!;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int MaxCapacity { get; set; }
    public string Room { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public ICollection<ClassEnrollment> Enrollments { get; set; } = new List<ClassEnrollment>();
}

public class ClassEnrollment
{
    public int Id { get; set; }
    public int MemberId { get; set; }
    public Member Member { get; set; } = null!;
    public int GymClassId { get; set; }
    public GymClass GymClass { get; set; } = null!;
    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;
    public bool Attended { get; set; } = false;
}

public class Payment
{
    public int Id { get; set; }
    public int MemberId { get; set; }
    public Member Member { get; set; } = null!;
    public decimal Amount { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, Completed, Failed
    public string TransactionId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string Description { get; set; } = string.Empty;
}
