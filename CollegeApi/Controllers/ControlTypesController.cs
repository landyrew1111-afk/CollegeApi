using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CollegeApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ControlTypesController : ControllerBase
    {
        private readonly AppDbContext _db;
        public ControlTypesController(AppDbContext db) => _db = db;

        [HttpGet]
        public IActionResult Get() => Ok(Queries.ControlTypes(_db));

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ControlTypeDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name)) return BadRequest(new { message = "Укажите название вида контроля" });

            var entity = new ControlType
            {
                naimenovanie_vida_kontrolya = dto.Name.Trim(),
                tip_kontrolya = dto.Tip <= 0 ? 1 : dto.Tip
            };
            _db.VidyKontrolya.Add(entity);
            await _db.SaveChangesAsync();
            return Ok(new ControlTypeDto
            {
                Id = entity.id_vida_kontrolya,
                Name = entity.naimenovanie_vida_kontrolya,
                Tip = entity.tip_kontrolya
            });
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] ControlTypeDto dto)
        {
            var entity = await _db.VidyKontrolya.FindAsync(id);
            if (entity == null) return NotFound(new { message = "Вид контроля не найден" });
            if (string.IsNullOrWhiteSpace(dto.Name)) return BadRequest(new { message = "Укажите название вида контроля" });

            entity.naimenovanie_vida_kontrolya = dto.Name.Trim();
            entity.tip_kontrolya = dto.Tip <= 0 ? entity.tip_kontrolya : dto.Tip;
            await _db.SaveChangesAsync();
            return Ok();
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (await _db.UchebniyPlan.AnyAsync(p => p.id_vida_kontrolya == id))
                return BadRequest(new { message = "Нельзя удалить: вид контроля используется в учебном плане" });

            var entity = await _db.VidyKontrolya.FindAsync(id);
            if (entity == null) return NotFound(new { message = "Вид контроля не найден" });
            _db.VidyKontrolya.Remove(entity);
            await _db.SaveChangesAsync();
            return Ok();
        }
    }
}
