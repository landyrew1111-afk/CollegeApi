using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CollegeApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GroupsController : ControllerBase
    {
        private readonly AppDbContext _db;
        public GroupsController(AppDbContext db) => _db = db;

        [HttpGet]
        public IActionResult Get() => Ok(Queries.Groups(_db));

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] GroupDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name)) return BadRequest(new { message = "Укажите название группы" });
            if (!await _db.Specialnosti.AnyAsync(s => s.id_specialnosti == dto.SpecialtyId))
                return BadRequest(new { message = "Выберите специальность" });

            var entity = new Group { nazvanie_gruppy = dto.Name.Trim(), id_specialnosti = dto.SpecialtyId };
            _db.Gruppy.Add(entity);
            await _db.SaveChangesAsync();
            return Ok(Queries.Groups(_db).First(g => g.Id == entity.id_gruppy));
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] GroupDto dto)
        {
            var entity = await _db.Gruppy.FindAsync(id);
            if (entity == null) return NotFound(new { message = "Группа не найдена" });
            if (string.IsNullOrWhiteSpace(dto.Name)) return BadRequest(new { message = "Укажите название группы" });
            if (!await _db.Specialnosti.AnyAsync(s => s.id_specialnosti == dto.SpecialtyId))
                return BadRequest(new { message = "Выберите специальность" });

            entity.nazvanie_gruppy = dto.Name.Trim();
            entity.id_specialnosti = dto.SpecialtyId;
            await _db.SaveChangesAsync();
            return Ok();
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (await _db.Studenty.AnyAsync(s => s.id_gruppy == id))
                return BadRequest(new { message = "Нельзя удалить: в группе есть студенты" });

            var entity = await _db.Gruppy.FindAsync(id);
            if (entity == null) return NotFound(new { message = "Группа не найдена" });
            _db.Gruppy.Remove(entity);
            await _db.SaveChangesAsync();
            return Ok();
        }
    }
}
