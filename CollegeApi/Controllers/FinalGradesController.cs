using Microsoft.AspNetCore.Mvc;

namespace CollegeApi.Controllers
{
    // Итоговые оценки (ведомость)
    [ApiController]
    [Route("api/[controller]")]
    public class FinalGradesController : ControllerBase
    {
        private readonly AppDbContext _db;
        public FinalGradesController(AppDbContext db) => _db = db;

        [HttpGet]
        public IActionResult Get(
            [FromQuery] int? studentId,
            [FromQuery] int? groupId,
            [FromQuery] int? disciplineId,
            [FromQuery] int? teacherId,
            [FromQuery] int? semestr)
        {
            IEnumerable<GradeDto> grades = Queries.Grades(_db, true);

            if (studentId.HasValue) grades = grades.Where(g => g.StudentId == studentId.Value);
            if (groupId.HasValue) grades = grades.Where(g => g.GroupId == groupId.Value);
            if (disciplineId.HasValue) grades = grades.Where(g => g.DisciplineId == disciplineId.Value);
            if (teacherId.HasValue) grades = grades.Where(g => g.TeacherId == teacherId.Value);
            if (semestr.HasValue) grades = grades.Where(g => g.Semestr == semestr.Value);

            return Ok(grades.ToList());
        }

        [HttpPost]
        public IActionResult Create([FromBody] GradeSaveDto dto)
        {
            var error = Validate(dto);
            if (error != null) return BadRequest(new { message = error });

            var entity = new FinalGrade
            {
                id_studenta = dto.StudentId,
                id_plana = dto.PlanId,
                ocenka = dto.Ocenka.Trim(),
                data_sdachi = dto.Data?.Date ?? DateTime.Today
            };
            _db.Uspevaemost.Add(entity);
            _db.SaveChanges();

            return Ok(Queries.Grades(_db, true).First(g => g.Id == entity.id_uspevaemosti));
        }

        [HttpPut("{id:int}")]
        public IActionResult Update(int id, [FromBody] GradeSaveDto dto)
        {
            var entity = _db.Uspevaemost.Find(id);
            if (entity == null) return NotFound(new { message = "Итоговая оценка не найдена" });

            var error = Validate(dto);
            if (error != null) return BadRequest(new { message = error });

            entity.id_studenta = dto.StudentId;
            entity.id_plana = dto.PlanId;
            entity.ocenka = dto.Ocenka.Trim();
            entity.data_sdachi = dto.Data?.Date ?? entity.data_sdachi;
            _db.SaveChanges();

            return Ok(Queries.Grades(_db, true).First(g => g.Id == id));
        }

        [HttpDelete("{id:int}")]
        public IActionResult Delete(int id)
        {
            var entity = _db.Uspevaemost.Find(id);
            if (entity == null) return NotFound(new { message = "Итоговая оценка не найдена" });
            _db.Uspevaemost.Remove(entity);
            _db.SaveChanges();
            return Ok(new { message = "Итоговая оценка удалена" });
        }

        private string? Validate(GradeSaveDto dto)
        {
            if (dto.StudentId <= 0) return "Выберите студента";
            if (dto.PlanId <= 0) return "Выберите дисциплину";
            if (string.IsNullOrWhiteSpace(dto.Ocenka)) return "Укажите оценку";
            if (!_db.Studenty.Any(s => s.id_studenta == dto.StudentId)) return "Студент не найден";
            if (!_db.UchebniyPlan.Any(p => p.id_plana == dto.PlanId)) return "Дисциплина не найдена";
            return null;
        }
    }
}
