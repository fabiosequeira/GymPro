using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using GymAPI.DTOs;
using GymAPI.Services;

namespace GymAPI.Controllers;

// ── Auth Controller ───────────────────────────────────────────────────────────
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    public AuthController(IAuthService auth) => _auth = auth;

    /// <summary>Login com email e password</summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _auth.LoginAsync(request);
        if (result == null) return Unauthorized(new { message = "Credenciais inválidas" });
        return Ok(new ApiResponse<AuthResponse>(true, "Login bem-sucedido", result));
    }

    /// <summary>Registar novo membro</summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var result = await _auth.RegisterAsync(request);
        if (result == null) return BadRequest(new { message = "Email já em uso ou plano inválido" });
        return Ok(new ApiResponse<AuthResponse>(true, "Conta criada com sucesso", result));
    }
}

// ── Members Controller ────────────────────────────────────────────────────────
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MembersController : ControllerBase
{
    private readonly IMemberService _members;
    public MembersController(IMemberService members) => _members = members;

    /// <summary>Listar todos os membros (Admin)</summary>
    [HttpGet]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _members.GetAllAsync(page, pageSize);
        return Ok(new ApiResponse<PagedResponse<MemberDto>>(true, "OK", result));
    }

    /// <summary>Criar novo membro</summary>
    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Create([FromBody] CreateMemberRequest request)
    {
        var result = await _members.CreateAsync(request);

        return Ok(new ApiResponse<MemberDto>(
            true,
            "Membro criado com sucesso",
            result
        ));
    }

    /// <summary>Obter membro por ID</summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _members.GetByIdAsync(id);
        if (result == null) return NotFound();
        return Ok(new ApiResponse<MemberDto>(true, "OK", result));
    }

    /// <summary>Atualizar dados do membro</summary>
    [HttpPut("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateMemberRequest request)
    {
        var ok = await _members.UpdateAsync(id, request);
        return ok ? Ok(new { message = "Atualizado com sucesso" }) : NotFound();
    }

    /// <summary>Desativar membro</summary>
    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Deactivate(int id)
    {
        var ok = await _members.DeactivateAsync(id);
        return ok ? Ok(new { message = "Membro desativado" }) : NotFound();
    }
}

// ── Trainers Controller ───────────────────────────────────────────────────────
[ApiController]
[Route("api/[controller]")]
public class TrainersController : ControllerBase
{
    private readonly ITrainerService _trainers;
    public TrainersController(ITrainerService trainers) => _trainers = trainers;

    /// <summary>Listar todos os treinadores</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _trainers.GetAllAsync();
        return Ok(new ApiResponse<List<TrainerDto>>(true, "OK", result));
    }

    /// <summary>Obter treinador por ID</summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _trainers.GetByIdAsync(id);
        if (result == null) return NotFound();
        return Ok(new ApiResponse<TrainerDto>(true, "OK", result));
    }

    /// <summary>Criar treinador (Admin)</summary>
    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Create([FromBody] CreateTrainerRequest request)
    {
        var result = await _trainers.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, new ApiResponse<TrainerDto>(true, "Treinador criado", result));
    }

    /// <summary>Atualizar treinador (Admin)</summary>
    [HttpPut("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateTrainerRequest request)
    {
        var ok = await _trainers.UpdateAsync(id, request);
        return ok ? Ok(new { message = "Treinador atualizado" }) : NotFound();
    }

    /// <summary>Desativar treinador (Admin)</summary>
    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Deactivate(int id)
    {
        var ok = await _trainers.DeactivateAsync(id);
        return ok ? Ok(new { message = "Treinador desativado" }) : NotFound();
    }
}

// ── Plans Controller ──────────────────────────────────────────────────────────
[ApiController]
[Route("api/[controller]")]
public class PlansController : ControllerBase
{
    private readonly IPlanService _plans;
    public PlansController(IPlanService plans) => _plans = plans;

    /// <summary>Listar todos os planos</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _plans.GetAllAsync();
        return Ok(new ApiResponse<List<PlanDto>>(true, "OK", result));
    }

    /// <summary>Criar plano (Admin)</summary>
    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Create([FromBody] CreatePlanRequest request)
    {
        var result = await _plans.CreateAsync(request);
        return Ok(new ApiResponse<PlanDto>(true, "Plano criado", result));
    }

    /// <summary>Eliminar plano (Admin)</summary>
    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(int id)
    {
        var ok = await _plans.DeleteAsync(id);
        return ok ? Ok(new { message = "Plano eliminado" }) : NotFound();
    }
}

// ── Classes Controller ────────────────────────────────────────────────────────
[ApiController]
[Route("api/[controller]")]
public class ClassesController : ControllerBase
{
    private readonly IClassService _classes;
    public ClassesController(IClassService classes) => _classes = classes;

    /// <summary>Listar todas as aulas</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _classes.GetAllAsync();
        return Ok(new ApiResponse<List<GymClassDto>>(true, "OK", result));
    }

    /// <summary>Obter aula por ID</summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _classes.GetByIdAsync(id);
        if (result == null) return NotFound();
        return Ok(new ApiResponse<GymClassDto>(true, "OK", result));
    }

    /// <summary>Criar aula (Admin/Trainer)</summary>
    [HttpPost]
    [Authorize(Policy = "TrainerOrAdmin")]
    public async Task<IActionResult> Create([FromBody] CreateClassRequest request)
    {
        var result = await _classes.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, new ApiResponse<GymClassDto>(true, "Aula criada", result));
    }

    /// <summary>Atualizar aula (Admin/Trainer)</summary>
    [HttpPut("{id}")]
    [Authorize(Policy = "TrainerOrAdmin")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateClassRequest request)
    {
        var ok = await _classes.UpdateAsync(id, request);
        return ok ? Ok(new { message = "Aula atualizada" }) : NotFound();
    }

    /// <summary>Eliminar aula (Admin)</summary>
    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(int id)
    {
        var ok = await _classes.DeleteAsync(id);
        return ok ? Ok(new { message = "Aula eliminada" }) : NotFound();
    }

    /// <summary>Inscrever membro na aula</summary>
    [HttpPost("{id}/enroll")]
    [Authorize]
    public async Task<IActionResult> Enroll(int id, [FromBody] EnrollRequest request)
    {
        var ok = await _classes.EnrollAsync(request.GymClassId, id);
        return ok ? Ok(new { message = "Inscrição realizada" }) : BadRequest(new { message = "Inscrição falhou (aula cheia ou já inscrito)" });
    }

    /// <summary>Cancelar inscrição</summary>
    [HttpDelete("{id}/enroll/{memberId}")]
    [Authorize]
    public async Task<IActionResult> Unenroll(int id, int memberId)
    {
        var ok = await _classes.UnenrollAsync(memberId, id);
        return ok ? Ok(new { message = "Inscrição cancelada" }) : NotFound();
    }

    /// <summary>Listar inscritos na aula (Admin/Trainer)</summary>
    [HttpGet("{id}/enrollments")]
    [Authorize(Policy = "TrainerOrAdmin")]
    public async Task<IActionResult> GetEnrollments(int id)
    {
        var result = await _classes.GetEnrollmentsAsync(id);
        return Ok(new ApiResponse<List<EnrollmentDto>>(true, "OK", result));
    }
}

// ── Payments Controller ───────────────────────────────────────────────────────
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _payments;
    public PaymentsController(IPaymentService payments) => _payments = payments;

    /// <summary>Processar pagamento (chama mock Mountebank)</summary>
    [HttpPost]
    public async Task<IActionResult> Process([FromBody] CreatePaymentRequest request)
    {
        var result = await _payments.ProcessPaymentAsync(request);
        return result.Success
            ? Ok(new ApiResponse<PaymentResponse>(true, "Pagamento processado", result))
            : BadRequest(new ApiResponse<PaymentResponse>(false, result.Message, result));
    }

    /// <summary>Histórico de pagamentos do membro</summary>
    [HttpGet("member/{memberId}")]
    public async Task<IActionResult> GetMemberPayments(int memberId)
    {
        var result = await _payments.GetMemberPaymentsAsync(memberId);
        return Ok(new ApiResponse<List<PaymentDto>>(true, "OK", result));
    }
}
