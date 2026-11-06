using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CollegeApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DisciplinesController : ControllerBase
    {
        private readonly AppDbContext _db;
        public DisciplinesController(AppDbContext db) => _db = db;

        [HttpGet]
        public IActionResult Get() => Ok(Queries.Disciplines(_db));

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] DisciplineDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name)) return BadRequest(new { message = "Укажите название дисциплины" });

            var entity = new Discipline { nazvanie_discipliny = dto.Name.Trim() };
            _db.Discipliny.Add(entity);
            await _db.SaveChangesAsync();
            return Ok(new DisciplineDto { Id = entity.id_discipliny, Name = entity.nazvanie_discipliny });
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] DisciplineDto dto)
        {
            var entity = await _db.Discipliny.FindAsync(id);
            if (entity == null) return NotFound(new { message = "Дисциплина не найдена" });
            if (string.IsNullOrWhiteSpace(dto.Name)) return BadRequest(new { message = "Укажите название дисциплины" });

            entity.nazvanie_discipliny = dto.Name.Trim();
            await _db.SaveChangesAsync();
            return Ok();
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (await _db.UchebniyPlan.AnyAsync(p => p.id_discipliny == id))
                return BadRequest(new { message = "Нельзя удалить: дисциплина используется в учебном плане" });

            var entity = await _db.Discipliny.FindAsync(id);
            if (entity == null) return NotFound(new { message = "Дисциплина не найдена" });
            _db.Discipliny.Remove(entity);
            await _db.SaveChangesAsync();
            return Ok();
        }
    }
}
