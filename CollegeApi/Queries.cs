using Microsoft.EntityFrameworkCore;

namespace CollegeApi
{
    // Небольшой слой преобразования сущностей БД в DTO.
    // Данных в базе немного, поэтому часть соединений делается в памяти —
    // так код остаётся простым и понятным.
    public static class Queries
    {
        public static string Fio(User u) =>
            (u.familiya + " " + u.imya + " " + (u.otchestvo ?? "")).Trim();

        public static Dictionary<int, string> RoleNames(AppDbContext db) =>
            db.Roli.ToDictionary(r => r.id_roli, r => r.nazvanie_roli);

        public static Dictionary<int, string> TeacherNames(AppDbContext db)
        {
            var users = db.Polzovateli.ToDictionary(u => u.id_polzovatelya);
            return db.Prepodavateli.ToDictionary(
                t => t.id_prepodavatelya,
                t => users.TryGetValue(t.id_polzovatelya, out var u) ? Fio(u) : "—");
        }

        public static List<UserDto> Users(AppDbContext db)
        {
            var roles = RoleNames(db);
            var groups = db.Gruppy.ToDictionary(g => g.id_gruppy);
            var students = db.Studenty.ToList();
            var teachers = db.Prepodavateli.ToList();

            return db.Polzovateli.OrderBy(u => u.familiya).ThenBy(u => u.imya).ToList()
                .Select(u =>
                {
                    var st = students.FirstOrDefault(s => s.id_polzovatelya == u.id_polzovatelya);
                    var te = teachers.FirstOrDefault(t => t.id_polzovatelya == u.id_polzovatelya);
                    return new UserDto
                    {
                        Id = u.id_polzovatelya,
                        RoleId = u.id_roli,
                        RoleName = roles.TryGetValue(u.id_roli, out var rn) ? rn : "—",
                        Login = u.login,
                        Parol = u.parol,
                        Familiya = u.familiya,
                        Imya = u.imya,
                        Otchestvo = u.otchestvo,
                        FotoUrl = u.foto_url,
                        Fio = Fio(u),
                        StudentId = st?.id_studenta,
                        GroupId = st?.id_gruppy,
                        GroupName = st != null && groups.TryGetValue(st.id_gruppy, out var g) ? g.nazvanie_gruppy : null,
                        NomerZachetki = st?.nomer_zachetki,
                        TeacherId = te?.id_prepodavatelya
                    };
                })
                .ToList();
        }

        public static List<PlanDto> Plans(AppDbContext db)
        {
            var specialties = db.Specialnosti.ToDictionary(s => s.id_specialnosti, s => s.naimenovanie_specialnosti);
            var disciplines = db.Discipliny.ToDictionary(d => d.id_discipliny, d => d.nazvanie_discipliny);
            var controls = db.VidyKontrolya.ToDictionary(c => c.id_vida_kontrolya, c => c.naimenovanie_vida_kontrolya);
            var teachers = TeacherNames(db);

            return db.UchebniyPlan.ToList()
                .OrderBy(p => p.semestr).ThenBy(p => p.id_plana)
                .Select(p => new PlanDto
                {
                    Id = p.id_plana,
                    SpecialtyId = p.id_specialnosti,
                    SpecialtyName = specialties.TryGetValue(p.id_specialnosti, out var sp) ? sp : "—",
                    DisciplineId = p.id_discipliny,
                    DisciplineName = disciplines.TryGetValue(p.id_discipliny, out var dn) ? dn : "—",
                    TeacherId = p.id_prepodavatelya,
                    TeacherFio = teachers.TryGetValue(p.id_prepodavatelya, out var tf) ? tf : "—",
                    Semestr = p.semestr,
                    Hours = p.kolichestvo_chasov,
                    ControlTypeId = p.id_vida_kontrolya,
                    ControlTypeName = controls.TryGetValue(p.id_vida_kontrolya, out var cn) ? cn : "—"
                })
                .ToList();
        }

        public static List<StudentDto> Students(AppDbContext db)
        {
            var groups = db.Gruppy.ToDictionary(g => g.id_gruppy, g => g.nazvanie_gruppy);
            var users = db.Polzovateli.ToDictionary(u => u.id_polzovatelya);

            return db.Studenty.ToList()
                .Select(s =>
                {
                    users.TryGetValue(s.id_polzovatelya, out var u);
                    return new StudentDto
                    {
                        Id = s.id_studenta,
                        UserId = s.id_polzovatelya,
                        Fio = u != null ? Fio(u) : "—",
                        Login = u?.login ?? "",
                        GroupId = s.id_gruppy,
                        GroupName = groups.TryGetValue(s.id_gruppy, out var g) ? g : "—",
                        NomerZachetki = s.nomer_zachetki
                    };
                })
                .OrderBy(s => s.GroupName).ThenBy(s => s.Fio)
                .ToList();
        }

        public static List<TeacherDto> Teachers(AppDbContext db)
        {
            var users = db.Polzovateli.ToDictionary(u => u.id_polzovatelya);
            return db.Prepodavateli.ToList()
                .Select(t =>
                {
                    users.TryGetValue(t.id_polzovatelya, out var u);
                    return new TeacherDto
                    {
                        Id = t.id_prepodavatelya,
                        UserId = t.id_polzovatelya,
                        Fio = u != null ? Fio(u) : "—",
                        Login = u?.login ?? ""
                    };
                })
                .OrderBy(t => t.Fio)
                .ToList();
        }

        public static List<GroupDto> Groups(AppDbContext db)
        {
            var specialties = db.Specialnosti.ToDictionary(s => s.id_specialnosti, s => s.naimenovanie_specialnosti);
            var counts = db.Studenty.GroupBy(s => s.id_gruppy).ToDictionary(g => g.Key, g => g.Count());

            return db.Gruppy.ToList()
                .Select(g => new GroupDto
                {
                    Id = g.id_gruppy,
                    Name = g.nazvanie_gruppy,
                    SpecialtyId = g.id_specialnosti,
                    SpecialtyName = specialties.TryGetValue(g.id_specialnosti, out var sn) ? sn : "—",
                    StudentCount = counts.TryGetValue(g.id_gruppy, out var c) ? c : 0
                })
                .OrderBy(g => g.Name)
                .ToList();
        }

        public static List<SpecialtyDto> Specialties(AppDbContext db) =>
            db.Specialnosti.ToList()
                .Select(s => new SpecialtyDto
                {
                    Id = s.id_specialnosti,
                    Kod = s.kod_specialnosti,
                    Name = s.naimenovanie_specialnosti
                })
                .OrderBy(s => s.Kod)
                .ToList();

        public static List<DisciplineDto> Disciplines(AppDbContext db) =>
            db.Discipliny.ToList()
                .Select(d => new DisciplineDto { Id = d.id_discipliny, Name = d.nazvanie_discipliny })
                .OrderBy(d => d.Name)
                .ToList();

        public static List<ControlTypeDto> ControlTypes(AppDbContext db) =>
            db.VidyKontrolya.ToList()
                .Select(c => new ControlTypeDto
                {
                    Id = c.id_vida_kontrolya,
                    Name = c.naimenovanie_vida_kontrolya,
                    Tip = c.tip_kontrolya
                })
                .OrderBy(c => c.Name)
                .ToList();

        // Общая сборка строк журнала оценок (текущих или итоговых).
        public static List<GradeDto> Grades(AppDbContext db, bool final)
        {
            var plans = Plans(db).ToDictionary(p => p.Id);
            var students = db.Studenty.ToDictionary(s => s.id_studenta);
            var users = db.Polzovateli.ToDictionary(u => u.id_polzovatelya);
            var groups = db.Gruppy.ToDictionary(g => g.id_gruppy, g => g.nazvanie_gruppy);

            string studentFio(int studentId, out int groupId, out string groupName)
            {
                groupId = 0;
                groupName = "—";
                if (!students.TryGetValue(studentId, out var st)) return "—";
                groupId = st.id_gruppy;
                if (groups.TryGetValue(st.id_gruppy, out var gn)) groupName = gn;
                return users.TryGetValue(st.id_polzovatelya, out var u) ? Fio(u) : "—";
            }

            GradeDto Build(int id, int studentId, int planId, string ocenka, DateTime? data, string? tema)
            {
                var fio = studentFio(studentId, out var gid, out var gname);
                plans.TryGetValue(planId, out var p);
                return new GradeDto
                {
                    Id = id,
                    Kind = final ? "final" : "current",
                    StudentId = studentId,
                    StudentFio = fio,
                    GroupId = gid,
                    GroupName = gname,
                    PlanId = planId,
                    DisciplineId = p?.DisciplineId ?? 0,
                    DisciplineName = p?.DisciplineName ?? "—",
                    TeacherId = p?.TeacherId ?? 0,
                    TeacherFio = p?.TeacherFio ?? "—",
                    Semestr = p?.Semestr ?? 0,
                    ControlTypeId = p?.ControlTypeId ?? 0,
                    ControlTypeName = p?.ControlTypeName ?? "—",
                    Ocenka = ocenka,
                    Data = data,
                    Tema = tema
                };
            }

            if (final)
            {
                return db.Uspevaemost.ToList()
                    .Select(x => Build(x.id_uspevaemosti, x.id_studenta, x.id_plana, x.ocenka, x.data_sdachi, null))
                    .OrderByDescending(x => x.Data)
                    .ToList();
            }

            return db.TekushchieOcenki.ToList()
                .Select(x => Build(x.id_tekushchey_ocenki, x.id_studenta, x.id_plana, x.ocenka, x.data_zanyatiya, x.tema_zanyatiya))
                .OrderByDescending(x => x.Data)
                .ToList();
        }
    }
}
