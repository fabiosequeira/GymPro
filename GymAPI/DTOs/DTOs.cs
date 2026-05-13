namespace GymAPI.DTOs;

// ── Auth ──────────────────────────────────────────────────────────────────────
public record LoginRequest(string Email, string Password);
public record RegisterRequest(string Name, string Email, string Password, string Phone, DateTime BirthDate, int PlanId);
public record AuthResponse(string Token, string Name, string Email, string Role);

// ── Member ────────────────────────────────────────────────────────────────────
public record MemberDto(int Id, string Name, string Email, string Phone, DateTime BirthDate, string PlanName, decimal PlanPrice, DateTime PlanEndDate, bool IsActive);
public record UpdateMemberRequest(string Phone, int PlanId);
public record CreateMemberRequest(string Name, string Email, string Password, string Phone, DateTime BirthDate, int PlanId);

// ── Trainer ───────────────────────────────────────────────────────────────────
public record TrainerDto(int Id, string Name, string Email, string Specialization, string Bio, bool IsActive);
public record CreateTrainerRequest(string Name, string Email, string Password, string Specialization, string Bio);
public record UpdateTrainerRequest(string Specialization, string Bio);

// ── Plan ──────────────────────────────────────────────────────────────────────
public record PlanDto(int Id, string Name, string Description, decimal Price, int DurationMonths, int MaxClassesPerMonth, bool IncludesPersonalTrainer);
public record CreatePlanRequest(string Name, string Description, decimal Price, int DurationMonths, int MaxClassesPerMonth, bool IncludesPersonalTrainer);

// ── Class ─────────────────────────────────────────────────────────────────────
public record GymClassDto(int Id, string Name, string Description, string TrainerName, DateTime StartTime, DateTime EndTime, int MaxCapacity, int EnrolledCount, string Room);
public record CreateClassRequest(string Name, string Description, int TrainerId, DateTime StartTime, DateTime EndTime, int MaxCapacity, string Room);
public record UpdateClassRequest(string Name, string Description, DateTime StartTime, DateTime EndTime, int MaxCapacity, string Room);

// ── Enrollment ────────────────────────────────────────────────────────────────
public record EnrollmentDto(int Id, string MemberName, string ClassName, DateTime EnrolledAt, bool Attended);
public record EnrollRequest(int GymClassId);

// ── Payment ───────────────────────────────────────────────────────────────────
public record PaymentDto(int Id, string MemberName, decimal Amount, string Status, string TransactionId, DateTime CreatedAt, string Description);
public record CreatePaymentRequest(int MemberId, decimal Amount, string Description);
public record PaymentResponse(bool Success, string TransactionId, string Message);

// ── Generic ───────────────────────────────────────────────────────────────────
public record ApiResponse<T>(bool Success, string Message, T? Data);
public record PagedResponse<T>(IEnumerable<T> Items, int Total, int Page, int PageSize);
