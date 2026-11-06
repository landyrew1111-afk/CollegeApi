using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CollegeApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PlansController : ControllerBase
    {
        private readonly AppDbContext _db;
        public PlansController(AppDbContext db) => _db = db;

        [HttpGet]
        public IActionResult Get([FromQuery] int? teacherId, [FromQuery] int? specialtyId)
        {
            var plans = Queries.Plans(_db);
            if (teacherId.HasValue) plans = plans.Where(p => p.TeacherId == teacherId.Value).ToList();
            if (specialtyId.HasValue) plans = plans.Where(p => p.SpecialtyId == specialtyId.Value).ToList();
            return Ok(plans);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] PlanSaveDto dto)
        {
            var error = await Validate(dto);
            if (error != null) return BadRequest(new { message = error });

            var entity = new Plan
            {
                id_specialnosti = dto.SpecialtyId,
                id_discipliny = dto.DisciplineId,
                id_prepodavatelya = dto.TeacherId,
                semestr = dto.Semestr,
                kolichestvo_chasov = dto.Hours,
                id_vida_kontrolya = dto.ControlTypeId
            };
            _db.UchebniyPlan.Add(entity);
            await _db.SaveChangesAsync();
            return Ok(Queries.Plans(_db).First(p => p.Id == entity.id_plana));
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] PlanSaveDto dto)
        {
            var entity = await _db.UchebniyPlan.FindAsync(id);
            if (entity == null) return NotFound(new { message = "Строка учебного плана не найдена" });

            var error = await Validate(dto);
            if (error != null) return BadRequest(new { message = error });

            entity.id_specialnosti = dto.SpecialtyId;
            entity.id_discipliny = dto.DisciplineId;
            entity.id_prepodavatelya = dto.TeacherId;
            entity.semestr = dto.Semestr;
            entity.kolichestvo_chasov = dto.Hours;
            entity.id_vida_kontrolya = dto.ControlTypeId;
            await _db.SaveChangesAsync();
            return Ok();
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (await _db.TekushchieOcenki.AnyAsync(g => g.id_plana == id) ||
                await _db.Uspevaemost.AnyAsync(g => g.id_plana == id))
                return BadRequest(new { message = "Нельзя удалить: по дисциплине уже выставлены оценки" });

            var entity = await _db.UchebniyPlan.FindAsync(id);
            if (entity == null) return NotFound(new { message = "Строка учебного плана не найдена" });
            _db.UchebniyPlan.Remove(entity);
            await _db.SaveChangesAsync();
            return Ok();
        }

        private async Task<string?> Validate(PlanSaveDto dto)
        {
            if (dto.SpecialtyId <= 0) return "Выберите специальность";
            if (dto.DisciplineId <= 0) return "Выберите дисциплину";
            if (dto.TeacherId <= 0) return "Выберите преподавателя";
            if (dto.ControlTypeId <= 0) return "Выберите вид контроля";
            if (dto.Semestr < 1 || dto.Semestr > 12) return "Семестр должен быть от 1 до 12";
            if (dto.Hours < 0) return "Количество часов не может быть отрицательным";
            if (!await _db.Prepodavateli.AnyAsync(t => t.id_prepodavatelya == dto.TeacherId))
                return "Преподаватель не найден";
            return null;
        }
    }
}
