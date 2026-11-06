using Microsoft.AspNetCore.Mvc;

namespace CollegeApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StudentsController : ControllerBase
    {
        private readonly AppDbContext _db;
        public StudentsController(AppDbContext db) => _db = db;

        [HttpGet]
        public IActionResult Get([FromQuery] int? groupId)
        {
            var students = Queries.Students(_db);
            if (groupId.HasValue) students = students.Where(s => s.GroupId == groupId.Value).ToList();
            return Ok(students);
        }

        [HttpGet("{id:int}")]
        public IActionResult GetById(int id)
        {
            var student = Queries.Students(_db).FirstOrDefault(s => s.Id == id);
            return student == null ? NotFound(new { message = "Студент не найден" }) : Ok(student);
        }
    }
}
