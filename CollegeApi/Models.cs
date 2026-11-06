using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CollegeApi
{
    [Table("22_Roli")]
    public class Role
    {
        [Key]
        [Column("id_roli")]
        public int id_roli { get; set; }

        [Column("nazvanie_roli")]
        public string nazvanie_roli { get; set; } = "";
    }

    [Table("22_Specialnosti")]
    public class Specialty
    {
        [Key]
        [Column("id_specialnosti")]
        public int id_specialnosti { get; set; }

        [Column("kod_specialnosti")]
        public string kod_specialnosti { get; set; } = "";

        [Column("naimenovanie_specialnosti")]
        public string naimenovanie_specialnosti { get; set; } = "";
    }

    [Table("22_Gruppy")]
    public class Group
    {
        [Key]
        [Column("id_gruppy")]
        public int id_gruppy { get; set; }

        [Column("nazvanie_gruppy")]
        public string nazvanie_gruppy { get; set; } = "";

        [Column("id_specialnosti")]
        public int id_specialnosti { get; set; }
    }

    [Table("22_Discipliny")]
    public class Discipline
    {
        [Key]
        [Column("id_discipliny")]
        public int id_discipliny { get; set; }

        [Column("nazvanie_discipliny")]
        public string nazvanie_discipliny { get; set; } = "";
    }

    [Table("22_Vidy_kontrolya")]
    public class ControlType
    {
        [Key]
        [Column("id_vida_kontrolya")]
        public int id_vida_kontrolya { get; set; }

        [Column("naimenovanie_vida_kontrolya")]
        public string naimenovanie_vida_kontrolya { get; set; } = "";

        [Column("tip_kontrolya")]
        public int tip_kontrolya { get; set; }
    }

    [Table("22_Polzovateli")]
    public class User
    {
        [Key]
        [Column("id_polzovatelya")]
        public int id_polzovatelya { get; set; }

        [Column("id_roli")]
        public int id_roli { get; set; }

        [Column("login")]
        public string login { get; set; } = "";

        [Column("parol")]
        public string parol { get; set; } = "";

        [Column("familiya")]
        public string familiya { get; set; } = "";

        [Column("imya")]
        public string imya { get; set; } = "";

        [Column("otchestvo")]
        public string? otchestvo { get; set; }

        [Column("foto_url")]
        public string? foto_url { get; set; }
    }

    [Table("22_Studenty")]
    public class Student
    {
        [Key]
        [Column("id_studenta")]
        public int id_studenta { get; set; }

        [Column("id_polzovatelya")]
        public int id_polzovatelya { get; set; }

        [Column("id_gruppy")]
        public int id_gruppy { get; set; }

        [Column("nomer_zachetki")]
        public string nomer_zachetki { get; set; } = "";
    }

    [Table("22_Prepodavateli")]
    public class Teacher
    {
        [Key]
        [Column("id_prepodavatelya")]
        public int id_prepodavatelya { get; set; }

        [Column("id_polzovatelya")]
        public int id_polzovatelya { get; set; }
    }

    [Table("22_Uchebniy_plan")]
    public class Plan
    {
        [Key]
        [Column("id_plana")]
        public int id_plana { get; set; }

        [Column("id_specialnosti")]
        public int id_specialnosti { get; set; }

        [Column("id_discipliny")]
        public int id_discipliny { get; set; }

        [Column("id_prepodavatelya")]
        public int id_prepodavatelya { get; set; }

        [Column("semestr")]
        public int semestr { get; set; }

        [Column("kolichestvo_chasov")]
        public int kolichestvo_chasov { get; set; }

        [Column("id_vida_kontrolya")]
        public int id_vida_kontrolya { get; set; }
    }

    [Table("22_Uspevaemost")]
    public class FinalGrade
    {
        [Key]
        [Column("id_uspevaemosti")]
        public int id_uspevaemosti { get; set; }

        [Column("id_studenta")]
        public int id_studenta { get; set; }

        [Column("id_plana")]
        public int id_plana { get; set; }

        [Column("ocenka")]
        public string ocenka { get; set; } = "";

        [Column("data_sdachi")]
        public DateTime? data_sdachi { get; set; }
    }

    [Table("22_Tekushchie_ocenki")]
    public class CurrentGrade
    {
        [Key]
        [Column("id_tekushchey_ocenki")]
        public int id_tekushchey_ocenki { get; set; }

        [Column("id_studenta")]
        public int id_studenta { get; set; }

        [Column("id_plana")]
        public int id_plana { get; set; }

        [Column("data_zanyatiya")]
        public DateTime data_zanyatiya { get; set; }

        [Column("tema_zanyatiya")]
        public string? tema_zanyatiya { get; set; }

        [Column("ocenka")]
        public string ocenka { get; set; } = "";
    }
}
