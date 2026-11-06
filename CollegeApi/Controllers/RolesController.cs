using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CollegeApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RolesController : ControllerBase
    {
        private readonly AppDbContext _db;
        public RolesController(AppDbContext db) => _db = db;

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var roles = await _db.Roli.OrderBy(r => r.id_roli).ToListAsync();
            return Ok(roles.Select(r => new { id = r.id_roli, name = r.nazvanie_roli }));
        }
    }
}
