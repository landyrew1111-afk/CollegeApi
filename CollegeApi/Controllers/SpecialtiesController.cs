using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CollegeApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SpecialtiesController : ControllerBase
    {
        private readonly AppDbContext _db;
        public SpecialtiesController(AppDbContext db) => _db = db;

        [HttpGet]
        public IActionResult Get() => Ok(Queries.Specialties(_db));

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] SpecialtyDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Kod) || string.IsNullOrWhiteSpace(dto.Name))
                return BadRequest(new { message = "Укажите код и название специальности" });

            var entity = new Specialty { kod_specialnosti = dto.Kod.Trim(), naimenovanie_specialnosti = dto.Name.Trim() };
            _db.Specialnosti.Add(entity);
            await _db.SaveChangesAsync();
            return Ok(new SpecialtyDto { Id = entity.id_specialnosti, Kod = entity.kod_specialnosti, Name = entity.naimenovanie_specialnosti });
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] SpecialtyDto dto)
        {
            var entity = await _db.Specialnosti.FindAsync(id);
            if (entity == null) return NotFound(new { message = "Специальность не найдена" });
            if (string.IsNullOrWhiteSpace(dto.Kod) || string.IsNullOrWhiteSpace(dto.Name))
                return BadRequest(new { message = "Укажите код и название специальности" });

            entity.kod_specialnosti = dto.Kod.Trim();
            entity.naimenovanie_specialnosti = dto.Name.Trim();
            await _db.SaveChangesAsync();
            return Ok();
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (await _db.Gruppy.AnyAsync(g => g.id_specialnosti == id))
                return BadRequest(new { message = "Нельзя удалить: к специальности привязаны группы" });
            if (await _db.UchebniyPlan.AnyAsync(p => p.id_specialnosti == id))
                return BadRequest(new { message = "Нельзя удалить: специальность используется в учебном плане" });

            var entity = await _db.Specialnosti.FindAsync(id);
            if (entity == null) return NotFound(new { message = "Специальность не найдена" });
            _db.Specialnosti.Remove(entity);
            await _db.SaveChangesAsync();
            return Ok();
        }
    }
}
