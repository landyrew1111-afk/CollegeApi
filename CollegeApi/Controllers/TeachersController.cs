using Microsoft.AspNetCore.Mvc;

namespace CollegeApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TeachersController : ControllerBase
    {
        private readonly AppDbContext _db;
        public TeachersController(AppDbContext db) => _db = db;

        [HttpGet]
        public IActionResult Get() => Ok(Queries.Teachers(_db));

        [HttpGet("{id:int}/plans")]
        public IActionResult GetPlans(int id) =>
            Ok(Queries.Plans(_db).Where(p => p.TeacherId == id).ToList());
    }
}
