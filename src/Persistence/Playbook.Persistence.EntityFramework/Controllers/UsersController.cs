using Microsoft.AspNetCore.Mvc;
using Playbook.Persistence.EntityFramework.Application;
using Playbook.Persistence.EntityFramework.Domain;

namespace Playbook.Persistence.EntityFramework.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController(IUnitOfWork uow) : ControllerBase
{
    // 1. GET: Fetch User with Roles (Demonstrating 'Include' usage)
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetUser(Guid id, CancellationToken ct)
    {
        // Notice: uow.UserRepository is strongly typed!
        var user = await uow.UserRepository.FindOneAsNoTrackingAsync(
            predicate: u => u.Id == id,
            cancellationToken: ct);

        if (user == null) return NotFound();

        return Ok(user);
    }

    // 3. POST: Create User & Role (Demonstrating Transactional Integrity)
    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request, CancellationToken ct)
    {
        await uow.BeginTransactionAsync(ct);
        try
        {
            var user = new UserEntity
            {
                Name = request.Name,
                Surname = request.Surname,
                Email = request.Email // Auto-encrypted by Interceptor
            };

            uow.UserRepository.Add(user);

            await uow.CommitTransactionAsync(ct);
            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
        }
        catch (Exception)
        {
            await uow.RollbackTransactionAsync(ct);
            return BadRequest("Could not create user. Transaction rolled back.");
        }
    }

    // 4. GET: Paged List (Demonstrating Pagination Extension)
    [HttpGet("paged")]
    public async Task<IActionResult> GetPaged([FromQuery] int index = 0, [FromQuery] int size = 10, CancellationToken ct = default)
    {
        var pagedUsers = await uow.UserRepository.FindAllByPaginateAsync(
            predicate: u => u.IsActive,
            orderBy: q => q.OrderBy(u => u.Surname),
            index: index,
            size: size,
            cancellationToken: ct);

        return Ok(pagedUsers);
    }
}

public record CreateUserRequest(string Name, string Surname, string Email);