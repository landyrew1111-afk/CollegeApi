using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CollegeApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReportsController : ControllerBase
    {
        private readonly AppDbContext _db;
        public ReportsController(AppDbContext db) => _db = db;

        [HttpGet("summary")]
        public async Task<IActionResult> Summary()
        {
            return Ok(new SummaryDto
            {
                Users = await _db.Polzovateli.CountAsync(),
                Students = await _db.Studenty.CountAsync(),
                Teachers = await _db.Prepodavateli.CountAsync(),
                Groups = await _db.Gruppy.CountAsync(),
                Disciplines = await _db.Discipliny.CountAsync(),
                Plans = await _db.UchebniyPlan.CountAsync(),
                CurrentGrades = await _db.TekushchieOcenki.CountAsync(),
                FinalGrades = await _db.Uspevaemost.CountAsync()
            });
        }

        // Сводка успеваемости одного студента
        [HttpGet("student/{studentId:int}")]
        public IActionResult Student(int studentId)
        {
            var student = Queries.Students(_db).FirstOrDefault(s => s.Id == studentId);
            if (student == null) return NotFound(new { message = "Студент не найден" });

            var group = _db.Gruppy.FirstOrDefault(g => g.id_gruppy == student.GroupId);
            var plans = group == null
                ? new List<PlanDto>()
                : Queries.Plans(_db).Where(p => p.SpecialtyId == group.id_specialnosti).ToList();

            var current = Queries.Grades(_db, false).Where(g => g.StudentId == studentId).ToList();
            var final = Queries.Grades(_db, true).Where(g => g.StudentId == studentId).ToList();

            var rows = plans.Select(p =>
            {
                var marks = current.Where(g => g.PlanId == p.Id)
                    .OrderBy(g => g.Data)
                    .Select(g => g.Ocenka)
                    .ToList();

                return new ProgressRowDto
                {
                    PlanId = p.Id,
                    DisciplineName = p.DisciplineName,
                    Semestr = p.Semestr,
                    ControlTypeName = p.ControlTypeName,
                    TeacherFio = p.TeacherFio,
                    FinalGrade = final.FirstOrDefault(g => g.PlanId == p.Id)?.Ocenka ?? "—",
                    CurrentGrades = marks.Count == 0 ? "" : string.Join(", ", marks)
                };
            }).ToList();

            var numeric = final
                .Select(g => int.TryParse(g.Ocenka, out var n) ? n : (int?)null)
                .Where(n => n.HasValue)
                .Select(n => n!.Value)
                .ToList();

            return Ok(new StudentProgressDto
            {
                StudentId = student.Id,
                Fio = student.Fio,
                GroupName = student.GroupName,
                NomerZachetki = student.NomerZachetki,
                Rows = rows,
                Average = numeric.Count == 0 ? null : Math.Round(numeric.Average(), 2)
            });
        }

        // Ведомость группы: студенты × дисциплины учебного плана
        [HttpGet("group/{groupId:int}")]
        public IActionResult Group(int groupId)
        {
            var group = _db.Gruppy.FirstOrDefault(g => g.id_gruppy == groupId);
            if (group == null) return NotFound(new { message = "Группа не найдена" });

            var plans = Queries.Plans(_db)
                .Where(p => p.SpecialtyId == group.id_specialnosti)
                .OrderBy(p => p.Semestr)
                .ToList();

            // Если одна дисциплина встречается в нескольких семестрах — уточняем семестр
            var duplicateNames = plans.GroupBy(p => p.DisciplineName)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToHashSet();

            var columns = plans
                .Select(p => duplicateNames.Contains(p.DisciplineName)
                    ? $"{p.DisciplineName} ({p.Semestr} сем.)"
                    : p.DisciplineName)
                .ToList();

            var final = Queries.Grades(_db, true);
            var students = Queries.Students(_db).Where(s => s.GroupId == groupId).ToList();

            var rows = students.Select(s => new StatementRowDto
            {
                StudentId = s.Id,
                Fio = s.Fio,
                NomerZachetki = s.NomerZachetki,
                Marks = plans.Select(p =>
                {
                    var mark = final.FirstOrDefault(g => g.StudentId == s.Id && g.PlanId == p.Id)?.Ocenka ?? "";
                    return string.IsNullOrWhiteSpace(mark) ? "—" : mark;
                }).ToList()
            }).ToList();

            return Ok(new GroupStatementDto
            {
                GroupId = group.id_gruppy,
                GroupName = group.nazvanie_gruppy,
                Disciplines = columns,
                Rows = rows
            });
        }
    }
}
