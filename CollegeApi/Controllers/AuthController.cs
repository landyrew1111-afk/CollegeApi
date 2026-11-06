using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CollegeApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _db;
        public AuthController(AppDbContext db) => _db = db;

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest data)
        {
            var user = await _db.Polzovateli
                .FirstOrDefaultAsync(u => u.login == data.login && u.parol == data.parol);

            if (user == null)
                return Unauthorized(new { message = "Неверный логин или пароль" });

            var response = new LoginResponse
            {
                UserId = user.id_polzovatelya,
                RoleId = user.id_roli,
                RoleName = "—",
                Fio = Queries.Fio(user),
                Login = user.login,
                FotoUrl = user.foto_url
            };

            var role = await _db.Roli.FirstOrDefaultAsync(r => r.id_roli == user.id_roli);
            if (role != null) response.RoleName = role.nazvanie_roli;

            var student = await _db.Studenty.FirstOrDefaultAsync(s => s.id_polzovatelya == user.id_polzovatelya);
            if (student != null)
            {
                response.StudentId = student.id_studenta;
                response.GroupId = student.id_gruppy;
                response.NomerZachetki = student.nomer_zachetki;
                var group = await _db.Gruppy.FirstOrDefaultAsync(g => g.id_gruppy == student.id_gruppy);
                response.GroupName = group?.nazvanie_gruppy;
            }

            var teacher = await _db.Prepodavateli.FirstOrDefaultAsync(t => t.id_polzovatelya == user.id_polzovatelya);
            if (teacher != null) response.TeacherId = teacher.id_prepodavatelya;

            return Ok(response);
        }

        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest data)
        {
            var user = await _db.Polzovateli.FindAsync(data.UserId);
            if (user == null) return NotFound(new { message = "Пользователь не найден" });
            if (user.parol != data.OldParol)
                return BadRequest(new { message = "Старый пароль указан неверно" });
            if (string.IsNullOrWhiteSpace(data.NewParol) || data.NewParol.Length < 4)
                return BadRequest(new { message = "Новый пароль должен содержать минимум 4 символа" });

            user.parol = data.NewParol;
            await _db.SaveChangesAsync();
            return Ok(new { message = "Пароль изменён" });
        }
    }
}
