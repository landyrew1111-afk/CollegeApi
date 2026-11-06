using Microsoft.AspNetCore.Mvc;

namespace CollegeApi.Controllers
{
    // Журнал текущих оценок
    [ApiController]
    [Route("api/[controller]")]
    public class GradesController : ControllerBase
    {
        private readonly AppDbContext _db;
        public GradesController(AppDbContext db) => _db = db;

        [HttpGet]
        public IActionResult Get(
            [FromQuery] int? studentId,
            [FromQuery] int? groupId,
            [FromQuery] int? disciplineId,
            [FromQuery] int? teacherId,
            [FromQuery] int? semestr,
            [FromQuery] string? search)
        {
            IEnumerable<GradeDto> grades = Queries.Grades(_db, false);

            if (studentId.HasValue) grades = grades.Where(g => g.StudentId == studentId.Value);
            if (groupId.HasValue) grades = grades.Where(g => g.GroupId == groupId.Value);
            if (disciplineId.HasValue) grades = grades.Where(g => g.DisciplineId == disciplineId.Value);
            if (teacherId.HasValue) grades = grades.Where(g => g.TeacherId == teacherId.Value);
            if (semestr.HasValue) grades = grades.Where(g => g.Semestr == semestr.Value);
            if (!string.IsNullOrWhiteSpace(search))
            {
                var q = search.Trim().ToLower();
                grades = grades.Where(g =>
                    g.StudentFio.ToLower().Contains(q) ||
                    g.DisciplineName.ToLower().Contains(q) ||
                    (g.Tema ?? "").ToLower().Contains(q));
            }

            return Ok(grades.ToList());
        }

        [HttpPost]
        public IActionResult Create([FromBody] GradeSaveDto dto)
        {
            var error = Validate(dto);
            if (error != null) return BadRequest(new { message = error });

            var entity = new CurrentGrade
            {
                id_studenta = dto.StudentId,
                id_plana = dto.PlanId,
                ocenka = dto.Ocenka.Trim(),
                data_zanyatiya = dto.Data?.Date ?? DateTime.Today,
                tema_zanyatiya = string.IsNullOrWhiteSpace(dto.Tema) ? null : dto.Tema.Trim()
            };
            _db.TekushchieOcenki.Add(entity);
            _db.SaveChanges();

            return Ok(Queries.Grades(_db, false).First(g => g.Id == entity.id_tekushchey_ocenki));
        }

        [HttpPut("{id:int}")]
        public IActionResult Update(int id, [FromBody] GradeSaveDto dto)
        {
            var entity = _db.TekushchieOcenki.Find(id);
            if (entity == null) return NotFound(new { message = "Оценка не найдена" });

            var error = Validate(dto);
            if (error != null) return BadRequest(new { message = error });

            entity.id_studenta = dto.StudentId;
            entity.id_plana = dto.PlanId;
            entity.ocenka = dto.Ocenka.Trim();
            entity.data_zanyatiya = dto.Data?.Date ?? entity.data_zanyatiya;
            entity.tema_zanyatiya = string.IsNullOrWhiteSpace(dto.Tema) ? null : dto.Tema.Trim();
            _db.SaveChanges();

            return Ok(Queries.Grades(_db, false).First(g => g.Id == id));
        }

        [HttpDelete("{id:int}")]
        public IActionResult Delete(int id)
        {
            var entity = _db.TekushchieOcenki.Find(id);
            if (entity == null) return NotFound(new { message = "Оценка не найдена" });
            _db.TekushchieOcenki.Remove(entity);
            _db.SaveChanges();
            return Ok(new { message = "Оценка удалена" });
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
