using Microsoft.EntityFrameworkCore;
using GymAPI.Data;
using GymAPI.DTOs;
using GymAPI.Models;
using System.Text.Json;

namespace GymAPI.Services;

// ── Member Service ────────────────────────────────────────────────────────────
public interface IMemberService
{
    Task<PagedResponse<MemberDto>> GetAllAsync(int page, int pageSize);
    Task<MemberDto> CreateAsync(CreateMemberRequest request);
    Task<MemberDto?> GetByIdAsync(int id);
    Task<bool> UpdateAsync(int id, UpdateMemberRequest request);
    Task<bool> DeactivateAsync(int id);
}

public class MemberService : IMemberService
{
    private readonly GymDbContext _db;
    private readonly ICacheService _cache;

    public MemberService(GymDbContext db, ICacheService cache)
    { _db = db; _cache = cache; }

    public async Task<PagedResponse<MemberDto>> GetAllAsync(int page, int pageSize)
    {
        var cacheKey = $"members:page:{page}:{pageSize}";
        var cached = await _cache.GetAsync<PagedResponse<MemberDto>>(cacheKey);
        if (cached != null) return cached;

        var query = _db.Members.Include(m => m.User).Include(m => m.Plan).Where(m => m.IsActive);
        var total = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize)
            .Select(m => new MemberDto(m.Id, m.User.Name, m.User.Email, m.Phone, m.BirthDate, m.Plan.Name, m.Plan.Price, m.PlanEndDate, m.IsActive))
            .ToListAsync();

        var result = new PagedResponse<MemberDto>(items, total, page, pageSize);
        await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));
        return result;
    }
    public async Task<MemberDto> CreateAsync(CreateMemberRequest request)
    {
        var user = new User
        {
            Name = request.Name,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = "Member"
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var member = new Member
        {
            UserId = user.Id,
            Phone = request.Phone,
            BirthDate = request.BirthDate,
            PlanId = request.PlanId,
            PlanEndDate = DateTime.UtcNow.AddMonths(1),
            IsActive = true
        };

        _db.Members.Add(member);

        await _db.SaveChangesAsync();

        await _cache.RemoveByPatternAsync("members:page");

        var plan = await _db.Plans.FindAsync(request.PlanId);

        return new MemberDto(
            member.Id,
            user.Name,
            user.Email,
            member.Phone,
            member.BirthDate,
            plan?.Name ?? "",
            plan?.Price ?? 0,
            member.PlanEndDate,
            member.IsActive
        );
    }

    public async Task<MemberDto?> GetByIdAsync(int id)
    {
        var cacheKey = $"member:{id}";
        var cached = await _cache.GetAsync<MemberDto>(cacheKey);
        if (cached != null) return cached;

        var m = await _db.Members.Include(m => m.User).Include(m => m.Plan).FirstOrDefaultAsync(m => m.Id == id);
        if (m == null) return null;

        var dto = new MemberDto(m.Id, m.User.Name, m.User.Email, m.Phone, m.BirthDate, m.Plan.Name, m.Plan.Price, m.PlanEndDate, m.IsActive);
        await _cache.SetAsync(cacheKey, dto);
        return dto;
    }

    public async Task<bool> UpdateAsync(int id, UpdateMemberRequest request)
    {
        var member = await _db.Members.FindAsync(id);
        if (member == null) return false;
        member.Phone = request.Phone;
        member.PlanId = request.PlanId;
        await _db.SaveChangesAsync();
        await _cache.RemoveAsync($"member:{id}");
        await _cache.RemoveByPatternAsync("members:page");
        return true;
    }

    public async Task<bool> DeactivateAsync(int id)
    {
        var member = await _db.Members.FindAsync(id);
        if (member == null) return false;
        member.IsActive = false;
        await _db.SaveChangesAsync();
        await _cache.RemoveAsync($"member:{id}");
        await _cache.RemoveByPatternAsync("members:page");
        return true;
    }
}

// ── Trainer Service ───────────────────────────────────────────────────────────
public interface ITrainerService
{
    Task<List<TrainerDto>> GetAllAsync();
    Task<TrainerDto?> GetByIdAsync(int id);
    Task<TrainerDto> CreateAsync(CreateTrainerRequest request);
    Task<bool> UpdateAsync(int id, UpdateTrainerRequest request);
    Task<bool> DeactivateAsync(int id);
}

public class TrainerService : ITrainerService
{
    private readonly GymDbContext _db;
    private readonly ICacheService _cache;

    public TrainerService(GymDbContext db, ICacheService cache)
    { _db = db; _cache = cache; }

    public async Task<List<TrainerDto>> GetAllAsync()
    {
        var cached = await _cache.GetAsync<List<TrainerDto>>("trainers:all");
        if (cached != null) return cached;

        var trainers = await _db.Trainers.Include(t => t.User).Where(t => t.IsActive)
            .Select(t => new TrainerDto(t.Id, t.User.Name, t.User.Email, t.Specialization, t.Bio, t.IsActive))
            .ToListAsync();

        await _cache.SetAsync("trainers:all", trainers, TimeSpan.FromMinutes(15));
        return trainers;
    }

    public async Task<TrainerDto?> GetByIdAsync(int id)
    {
        var t = await _db.Trainers.Include(t => t.User).FirstOrDefaultAsync(t => t.Id == id);
        if (t == null) return null;
        return new TrainerDto(t.Id, t.User.Name, t.User.Email, t.Specialization, t.Bio, t.IsActive);
    }

    public async Task<TrainerDto> CreateAsync(CreateTrainerRequest request)
    {
        var user = new User { Name = request.Name, Email = request.Email, PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password), Role = "Trainer" };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        var trainer = new Trainer { UserId = user.Id, Specialization = request.Specialization, Bio = request.Bio };
        _db.Trainers.Add(trainer);
        await _db.SaveChangesAsync();
        await _cache.RemoveAsync("trainers:all");
        return new TrainerDto(trainer.Id, user.Name, user.Email, trainer.Specialization, trainer.Bio, trainer.IsActive);
    }

    public async Task<bool> UpdateAsync(int id, UpdateTrainerRequest request)
    {
        var trainer = await _db.Trainers.FindAsync(id);
        if (trainer == null) return false;
        trainer.Specialization = request.Specialization;
        trainer.Bio = request.Bio;
        await _db.SaveChangesAsync();
        await _cache.RemoveAsync("trainers:all");
        return true;
    }

    public async Task<bool> DeactivateAsync(int id)
    {
        var trainer = await _db.Trainers.FindAsync(id);
        if (trainer == null) return false;
        trainer.IsActive = false;
        await _db.SaveChangesAsync();
        await _cache.RemoveAsync("trainers:all");
        return true;
    }
}

// ── Plan Service ──────────────────────────────────────────────────────────────
public interface IPlanService
{
    Task<List<PlanDto>> GetAllAsync();
    Task<PlanDto> CreateAsync(CreatePlanRequest request);
    Task<bool> DeleteAsync(int id);
}

public class PlanService : IPlanService
{
    private readonly GymDbContext _db;
    private readonly ICacheService _cache;

    public PlanService(GymDbContext db, ICacheService cache)
    { _db = db; _cache = cache; }

    public async Task<List<PlanDto>> GetAllAsync()
    {
        var cached = await _cache.GetAsync<List<PlanDto>>("plans:all");
        if (cached != null) return cached;

        var plans = await _db.Plans.Where(p => p.IsActive)
            .Select(p => new PlanDto(p.Id, p.Name, p.Description, p.Price, p.DurationMonths, p.MaxClassesPerMonth, p.IncludesPersonalTrainer))
            .ToListAsync();

        await _cache.SetAsync("plans:all", plans, TimeSpan.FromMinutes(30));
        return plans;
    }

    public async Task<PlanDto> CreateAsync(CreatePlanRequest request)
    {
        var plan = new Plan { Name = request.Name, Description = request.Description, Price = request.Price, DurationMonths = request.DurationMonths, MaxClassesPerMonth = request.MaxClassesPerMonth, IncludesPersonalTrainer = request.IncludesPersonalTrainer };
        _db.Plans.Add(plan);
        await _db.SaveChangesAsync();
        await _cache.RemoveAsync("plans:all");
        return new PlanDto(plan.Id, plan.Name, plan.Description, plan.Price, plan.DurationMonths, plan.MaxClassesPerMonth, plan.IncludesPersonalTrainer);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var plan = await _db.Plans.FindAsync(id);
        if (plan == null) return false;
        plan.IsActive = false;
        await _db.SaveChangesAsync();
        await _cache.RemoveAsync("plans:all");
        return true;
    }
}

// ── Class Service ─────────────────────────────────────────────────────────────
public interface IClassService
{
    Task<List<GymClassDto>> GetAllAsync();
    Task<GymClassDto?> GetByIdAsync(int id);
    Task<GymClassDto> CreateAsync(CreateClassRequest request);
    Task<bool> UpdateAsync(int id, UpdateClassRequest request);
    Task<bool> DeleteAsync(int id);
    Task<bool> EnrollAsync(int memberId, int classId);
    Task<bool> UnenrollAsync(int memberId, int classId);
    Task<List<EnrollmentDto>> GetEnrollmentsAsync(int classId);
}

public class ClassService : IClassService
{
    private readonly GymDbContext _db;
    private readonly ICacheService _cache;

    public ClassService(GymDbContext db, ICacheService cache)
    { _db = db; _cache = cache; }

    public async Task<List<GymClassDto>> GetAllAsync()
    {
        var cached = await _cache.GetAsync<List<GymClassDto>>("classes:all");
        if (cached != null) return cached;

        var classes = await _db.GymClasses.Include(c => c.Trainer).ThenInclude(t => t.User)
            .Include(c => c.Enrollments).Where(c => c.IsActive)
            .Select(c => new GymClassDto(c.Id, c.Name, c.Description, c.Trainer.User.Name, c.StartTime, c.EndTime, c.MaxCapacity, c.Enrollments.Count, c.Room))
            .ToListAsync();

        await _cache.SetAsync("classes:all", classes, TimeSpan.FromMinutes(5));
        return classes;
    }

    public async Task<GymClassDto?> GetByIdAsync(int id)
    {
        var c = await _db.GymClasses.Include(c => c.Trainer).ThenInclude(t => t.User)
            .Include(c => c.Enrollments).FirstOrDefaultAsync(c => c.Id == id);
        if (c == null) return null;
        return new GymClassDto(c.Id, c.Name, c.Description, c.Trainer.User.Name, c.StartTime, c.EndTime, c.MaxCapacity, c.Enrollments.Count, c.Room);
    }

    public async Task<GymClassDto> CreateAsync(CreateClassRequest request)
    {
        var gymClass = new GymClass { Name = request.Name, Description = request.Description, TrainerId = request.TrainerId, StartTime = request.StartTime, EndTime = request.EndTime, MaxCapacity = request.MaxCapacity, Room = request.Room };
        _db.GymClasses.Add(gymClass);
        await _db.SaveChangesAsync();
        await _cache.RemoveAsync("classes:all");
        var trainer = await _db.Trainers.Include(t => t.User).FirstAsync(t => t.Id == request.TrainerId);
        return new GymClassDto(gymClass.Id, gymClass.Name, gymClass.Description, trainer.User.Name, gymClass.StartTime, gymClass.EndTime, gymClass.MaxCapacity, 0, gymClass.Room);
    }

    public async Task<bool> UpdateAsync(int id, UpdateClassRequest request)
    {
        var gymClass = await _db.GymClasses.FindAsync(id);
        if (gymClass == null) return false;
        gymClass.Name = request.Name; gymClass.Description = request.Description;
        gymClass.StartTime = request.StartTime; gymClass.EndTime = request.EndTime;
        gymClass.MaxCapacity = request.MaxCapacity; gymClass.Room = request.Room;
        await _db.SaveChangesAsync();
        await _cache.RemoveAsync("classes:all");
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var gymClass = await _db.GymClasses.FindAsync(id);
        if (gymClass == null) return false;
        gymClass.IsActive = false;
        await _db.SaveChangesAsync();
        await _cache.RemoveAsync("classes:all");
        return true;
    }

    public async Task<bool> EnrollAsync(int memberId, int classId)
    {
        var gymClass = await _db.GymClasses.Include(c => c.Enrollments).FirstOrDefaultAsync(c => c.Id == classId);
        if (gymClass == null || gymClass.Enrollments.Count >= gymClass.MaxCapacity) return false;
        if (await _db.ClassEnrollments.AnyAsync(e => e.MemberId == memberId && e.GymClassId == classId)) return false;
        _db.ClassEnrollments.Add(new ClassEnrollment { MemberId = memberId, GymClassId = classId });
        await _db.SaveChangesAsync();
        await _cache.RemoveAsync("classes:all");
        return true;
    }

    public async Task<bool> UnenrollAsync(int memberId, int classId)
    {
        var enrollment = await _db.ClassEnrollments.FirstOrDefaultAsync(e => e.MemberId == memberId && e.GymClassId == classId);
        if (enrollment == null) return false;
        _db.ClassEnrollments.Remove(enrollment);
        await _db.SaveChangesAsync();
        await _cache.RemoveAsync("classes:all");
        return true;
    }

    public async Task<List<EnrollmentDto>> GetEnrollmentsAsync(int classId) =>
        await _db.ClassEnrollments.Include(e => e.Member).ThenInclude(m => m.User)
            .Include(e => e.GymClass).Where(e => e.GymClassId == classId)
            .Select(e => new EnrollmentDto(e.Id, e.Member.User.Name, e.GymClass.Name, e.EnrolledAt, e.Attended))
            .ToListAsync();
}

// ── Payment Service (calls Mountebank mock) ───────────────────────────────────
public interface IPaymentService
{
    Task<PaymentResponse> ProcessPaymentAsync(CreatePaymentRequest request);
    Task<List<PaymentDto>> GetMemberPaymentsAsync(int memberId);
}

public class PaymentService : IPaymentService
{
    private readonly GymDbContext _db;
    private readonly IHttpClientFactory _httpFactory;

    public PaymentService(GymDbContext db, IHttpClientFactory httpFactory)
    { _db = db; _httpFactory = httpFactory; }

    public async Task<PaymentResponse> ProcessPaymentAsync(CreatePaymentRequest request)
    {
        var client = _httpFactory.CreateClient("PaymentService");
        PaymentResponse paymentResult;

        try
        {
            var payload = new { amount = request.Amount, memberId = request.MemberId, description = request.Description };
            var response = await client.PostAsJsonAsync("/payment/process", payload);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var json = JsonDocument.Parse(content);
                paymentResult = new PaymentResponse(true, Guid.NewGuid().ToString("N")[..16].ToUpper(), "Pagamento processado com sucesso");
            }
            else
            {
                paymentResult = new PaymentResponse(false, string.Empty, "Falha no serviço de pagamento");
            }
        }
        catch
        {
            // Circuit breaker opened or timeout — simulate fallback
            paymentResult = new PaymentResponse(false, string.Empty, "Serviço de pagamento indisponível (circuit breaker)");
        }

        var payment = new Payment
        {
            MemberId = request.MemberId,
            Amount = request.Amount,
            Status = paymentResult.Success ? "Completed" : "Failed",
            TransactionId = paymentResult.TransactionId,
            Description = request.Description,
            CreatedAt = DateTime.UtcNow
        };
        _db.Payments.Add(payment);
        await _db.SaveChangesAsync();

        return paymentResult;
    }

    public async Task<List<PaymentDto>> GetMemberPaymentsAsync(int memberId) =>
     await _db.Payments
         .Include(p => p.Member)
         .ThenInclude(m => m.User)
         .Where(p => p.MemberId == memberId)
         .OrderByDescending(p => p.CreatedAt)
         .Select(p => new PaymentDto(
             p.Id,
             p.Member.User.Name,
             p.Amount,
             p.Status,
             p.TransactionId,
             p.CreatedAt,
             p.Description
         ))
         .ToListAsync();
}
