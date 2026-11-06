namespace CollegeApi
{
    // ---------- Авторизация ----------
    public class LoginRequest
    {
        public string login { get; set; } = "";
        public string parol { get; set; } = "";
    }

    public class LoginResponse
    {
        public int UserId { get; set; }
        public int RoleId { get; set; }
        public string RoleName { get; set; } = "";
        public string Fio { get; set; } = "";
        public string Login { get; set; } = "";
        public string? FotoUrl { get; set; }
        public int? StudentId { get; set; }
        public int? TeacherId { get; set; }
        public int? GroupId { get; set; }
        public string? GroupName { get; set; }
        public string? NomerZachetki { get; set; }
    }

    public class ChangePasswordRequest
    {
        public int UserId { get; set; }
        public string OldParol { get; set; } = "";
        public string NewParol { get; set; } = "";
    }

    // ---------- Пользователи ----------
    public class UserDto
    {
        public int Id { get; set; }
        public int RoleId { get; set; }
        public string RoleName { get; set; } = "";
        public string Login { get; set; } = "";
        public string Parol { get; set; } = "";
        public string Familiya { get; set; } = "";
        public string Imya { get; set; } = "";
        public string? Otchestvo { get; set; }
        public string? FotoUrl { get; set; }
        public string Fio { get; set; } = "";
        public int? StudentId { get; set; }
        public int? GroupId { get; set; }
        public string? GroupName { get; set; }
        public string? NomerZachetki { get; set; }
        public int? TeacherId { get; set; }
    }

    public class UserSaveDto
    {
        public int RoleId { get; set; } = 3;
        public string Login { get; set; } = "";
        public string Parol { get; set; } = "";
        public string Familiya { get; set; } = "";
        public string Imya { get; set; } = "";
        public string? Otchestvo { get; set; }
        public string? FotoUrl { get; set; }
        public int? GroupId { get; set; }
        public string? NomerZachetki { get; set; }
    }

    public class AvatarDto
    {
        public string? FotoUrl { get; set; }
    }

    public class StudentDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Fio { get; set; } = "";
        public string Login { get; set; } = "";
        public int GroupId { get; set; }
        public string GroupName { get; set; } = "";
        public string NomerZachetki { get; set; } = "";
    }

    public class TeacherDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Fio { get; set; } = "";
        public string Login { get; set; } = "";
    }

    // ---------- Справочники ----------
    public class SpecialtyDto
    {
        public int Id { get; set; }
        public string Kod { get; set; } = "";
        public string Name { get; set; } = "";
    }

    public class GroupDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int SpecialtyId { get; set; }
        public string SpecialtyName { get; set; } = "";
        public int StudentCount { get; set; }
    }

    public class DisciplineDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    public class ControlTypeDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int Tip { get; set; }
    }

    // ---------- Учебный план ----------
    public class PlanDto
    {
        public int Id { get; set; }
        public int SpecialtyId { get; set; }
        public string SpecialtyName { get; set; } = "";
        public int DisciplineId { get; set; }
        public string DisciplineName { get; set; } = "";
        public int TeacherId { get; set; }
        public string TeacherFio { get; set; } = "";
        public int Semestr { get; set; }
        public int Hours { get; set; }
        public int ControlTypeId { get; set; }
        public string ControlTypeName { get; set; } = "";
    }

    public class PlanSaveDto
    {
        public int SpecialtyId { get; set; }
        public int DisciplineId { get; set; }
        public int TeacherId { get; set; }
        public int Semestr { get; set; } = 1;
        public int Hours { get; set; } = 0;
        public int ControlTypeId { get; set; }
    }

    // ---------- Оценки ----------
    public class GradeDto
    {
        public int Id { get; set; }
        public string Kind { get; set; } = "current"; // current | final
        public int StudentId { get; set; }
        public string StudentFio { get; set; } = "";
        public int GroupId { get; set; }
        public string GroupName { get; set; } = "";
        public int PlanId { get; set; }
        public int DisciplineId { get; set; }
        public string DisciplineName { get; set; } = "";
        public int TeacherId { get; set; }
        public string TeacherFio { get; set; } = "";
        public int Semestr { get; set; }
        public int ControlTypeId { get; set; }
        public string ControlTypeName { get; set; } = "";
        public string Ocenka { get; set; } = "";
        public DateTime? Data { get; set; }
        public string? Tema { get; set; }
    }

    public class GradeSaveDto
    {
        public int StudentId { get; set; }
        public int PlanId { get; set; }
        public string Ocenka { get; set; } = "";
        public DateTime? Data { get; set; }
        public string? Tema { get; set; }
    }

    // ---------- Отчёты ----------
    public class ProgressRowDto
    {
        public int PlanId { get; set; }
        public string DisciplineName { get; set; } = "";
        public int Semestr { get; set; }
        public string ControlTypeName { get; set; } = "";
        public string TeacherFio { get; set; } = "";
        public string FinalGrade { get; set; } = "—";
        public string CurrentGrades { get; set; } = "";
    }

    public class StudentProgressDto
    {
        public int StudentId { get; set; }
        public string Fio { get; set; } = "";
        public string GroupName { get; set; } = "";
        public string NomerZachetki { get; set; } = "";
        public List<ProgressRowDto> Rows { get; set; } = new();
        public double? Average { get; set; }
    }

    public class StatementRowDto
    {
        public int StudentId { get; set; }
        public string Fio { get; set; } = "";
        public string NomerZachetki { get; set; } = "";
        public List<string> Marks { get; set; } = new();
    }

    public class GroupStatementDto
    {
        public int GroupId { get; set; }
        public string GroupName { get; set; } = "";
        public List<string> Disciplines { get; set; } = new();
        public List<StatementRowDto> Rows { get; set; } = new();
    }

    public class SummaryDto
    {
        public int Users { get; set; }
        public int Students { get; set; }
        public int Teachers { get; set; }
        public int Groups { get; set; }
        public int Disciplines { get; set; }
        public int Plans { get; set; }
        public int CurrentGrades { get; set; }
        public int FinalGrades { get; set; }
    }
}
