using Microsoft.AspNetCore.Mvc;
using DAL.UnitOfWork;
using DAL.Models;

[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public TestController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpPost("seed-user")]
    public async Task<IActionResult> SeedUser()
    {
        
        var testUser = new User(
            "Test",
            "User",
            "test_user",
            "test@example.com",
            "password123",
            0,
            0
        );

        await _unitOfWork.User.Add(testUser);
        await _unitOfWork.Save();

        return Ok("Korisnik je uspesno upisan u bazu!");
    }
}