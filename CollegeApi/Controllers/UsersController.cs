using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CollegeApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _db;
        public UsersController(AppDbContext db) => _db = db;

        [HttpGet]
        public IActionResult Get([FromQuery] int? roleId)
        {
            var users = Queries.Users(_db);
            if (roleId.HasValue) users = users.Where(u => u.RoleId == roleId.Value).ToList();
            return Ok(users);
        }

        [HttpGet("{id:int}")]
        public IActionResult GetById(int id)
        {
            var user = Queries.Users(_db).FirstOrDefault(u => u.Id == id);
            return user == null ? NotFound(new { message = "Пользователь не найден" }) : Ok(user);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] UserSaveDto dto)
        {
            var error = Validate(dto);
            if (error != null) return BadRequest(new { message = error });

            var user = new User
            {
                id_roli = dto.RoleId,
                login = dto.Login.Trim(),
                parol = string.IsNullOrWhiteSpace(dto.Parol) ? "12345" : dto.Parol,
                familiya = dto.Familiya.Trim(),
                imya = dto.Imya.Trim(),
                otchestvo = dto.Otchestvo?.Trim(),
                foto_url = dto.FotoUrl
            };

            _db.Polzovateli.Add(user);
            await _db.SaveChangesAsync();

            await SyncRoleRecords(user.id_polzovatelya, dto.RoleId, dto.GroupId, dto.NomerZachetki);
            await _db.SaveChangesAsync();

            return Ok(Queries.Users(_db).First(u => u.Id == user.id_polzovatelya));
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UserSaveDto dto)
        {
            var user = await _db.Polzovateli.FindAsync(id);
            if (user == null) return NotFound(new { message = "Пользователь не найден" });

            var error = Validate(dto, id);
            if (error != null) return BadRequest(new { message = error });

            user.id_roli = dto.RoleId;
            user.login = dto.Login.Trim();
            if (!string.IsNullOrWhiteSpace(dto.Parol)) user.parol = dto.Parol;
            user.familiya = dto.Familiya.Trim();
            user.imya = dto.Imya.Trim();
            user.otchestvo = dto.Otchestvo?.Trim();
            // Фото меняется отдельным запросом; null в общем сохранении значит "не трогать".
            if (dto.FotoUrl != null) user.foto_url = dto.FotoUrl.Trim();

            await SyncRoleRecords(id, dto.RoleId, dto.GroupId, dto.NomerZachetki);
            await _db.SaveChangesAsync();

            return Ok(Queries.Users(_db).First(u => u.Id == id));
        }

        [HttpPost("{id:int}/avatar")]
        public async Task<IActionResult> SetAvatar(int id, [FromBody] AvatarDto dto)
        {
            var user = await _db.Polzovateli.FindAsync(id);
            if (user == null) return NotFound(new { message = "Пользователь не найден" });

            user.foto_url = string.IsNullOrWhiteSpace(dto.FotoUrl) ? null : dto.FotoUrl.Trim();
            await _db.SaveChangesAsync();

            return Ok(new { message = "Фото обновлено", fotoUrl = user.foto_url });
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _db.Polzovateli.FindAsync(id);
            if (user == null) return NotFound(new { message = "Пользователь не найден" });

            var student = await _db.Studenty.FirstOrDefaultAsync(s => s.id_polzovatelya == id);
            if (student != null)
            {
                var current = await _db.TekushchieOcenki.Where(g => g.id_studenta == student.id_studenta).ToListAsync();
                var final = await _db.Uspevaemost.Where(g => g.id_studenta == student.id_studenta).ToListAsync();
                _db.TekushchieOcenki.RemoveRange(current);
                _db.Uspevaemost.RemoveRange(final);
                _db.Studenty.Remove(student);
            }

            var teacher = await _db.Prepodavateli.FirstOrDefaultAsync(t => t.id_polzovatelya == id);
            if (teacher != null)
            {
                var plans = await _db.UchebniyPlan.Where(p => p.id_prepodavatelya == teacher.id_prepodavatelya).ToListAsync();
                foreach (var plan in plans)
                {
                    var current = await _db.TekushchieOcenki.Where(g => g.id_plana == plan.id_plana).ToListAsync();
                    var final = await _db.Uspevaemost.Where(g => g.id_plana == plan.id_plana).ToListAsync();
                    _db.TekushchieOcenki.RemoveRange(current);
                    _db.Uspevaemost.RemoveRange(final);
                }
                _db.UchebniyPlan.RemoveRange(plans);
                _db.Prepodavateli.Remove(teacher);
            }

            // Сначала удаляем зависимые записи (студент/преподаватель, оценки, планы),
            // только потом саму учётную запись — иначе нарушается внешний ключ.
            await _db.SaveChangesAsync();

            _db.Polzovateli.Remove(user);
            await _db.SaveChangesAsync();
            return Ok(new { message = "Пользователь удалён" });
        }

        private string? Validate(UserSaveDto dto, int? exceptId = null)
        {
            if (string.IsNullOrWhiteSpace(dto.Login)) return "Укажите логин";
            if (string.IsNullOrWhiteSpace(dto.Familiya)) return "Укажите фамилию";
            if (string.IsNullOrWhiteSpace(dto.Imya)) return "Укажите имя";
            if (dto.RoleId <= 0) return "Выберите роль";
            if (dto.RoleId == 3 && dto.GroupId is null) return "Для студента выберите группу";

            var login = dto.Login.Trim().ToLower();
            var busy = _db.Polzovateli.Any(u =>
                u.login.ToLower() == login &&
                (!exceptId.HasValue || u.id_polzovatelya != exceptId.Value));
            if (busy) return "Пользователь с таким логином уже существует";

            return null;
        }

        private async Task SyncRoleRecords(int userId, int roleId, int? groupId, string? nomerZachetki)
        {
            var student = await _db.Studenty.FirstOrDefaultAsync(s => s.id_polzovatelya == userId);
            var teacher = await _db.Prepodavateli.FirstOrDefaultAsync(t => t.id_polzovatelya == userId);

            if (roleId == 3)
            {
                if (teacher != null) _db.Prepodavateli.Remove(teacher);
                if (student == null)
                {
                    student = new Student { id_polzovatelya = userId, id_gruppy = groupId ?? 1 };
                    _db.Studenty.Add(student);
                }
                student.id_gruppy = groupId ?? student.id_gruppy;
                if (!string.IsNullOrWhiteSpace(nomerZachetki)) student.nomer_zachetki = nomerZachetki!.Trim();
                else if (string.IsNullOrWhiteSpace(student.nomer_zachetki))
                    student.nomer_zachetki = "2231" + userId.ToString("D3");
            }
            else if (roleId == 2)
            {
                if (student != null)
                {
                    var cur = await _db.TekushchieOcenki.Where(g => g.id_studenta == student.id_studenta).ToListAsync();
                    var fin = await _db.Uspevaemost.Where(g => g.id_studenta == student.id_studenta).ToListAsync();
                    _db.TekushchieOcenki.RemoveRange(cur);
                    _db.Uspevaemost.RemoveRange(fin);
                    _db.Studenty.Remove(student);
                }
                if (teacher == null) _db.Prepodavateli.Add(new Teacher { id_polzovatelya = userId });
            }
            else
            {
                if (student != null)
                {
                    var cur = await _db.TekushchieOcenki.Where(g => g.id_studenta == student.id_studenta).ToListAsync();
                    var fin = await _db.Uspevaemost.Where(g => g.id_studenta == student.id_studenta).ToListAsync();
                    _db.TekushchieOcenki.RemoveRange(cur);
                    _db.Uspevaemost.RemoveRange(fin);
                    _db.Studenty.Remove(student);
                }
                if (teacher != null)
                {
                    var plans = await _db.UchebniyPlan.Where(p => p.id_prepodavatelya == teacher.id_prepodavatelya).ToListAsync();
                    foreach (var plan in plans)
                    {
                        _db.TekushchieOcenki.RemoveRange(_db.TekushchieOcenki.Where(g => g.id_plana == plan.id_plana));
                        _db.Uspevaemost.RemoveRange(_db.Uspevaemost.Where(g => g.id_plana == plan.id_plana));
                    }
                    _db.UchebniyPlan.RemoveRange(plans);
                    _db.Prepodavateli.Remove(teacher);
                }
            }
        }
    }
}
