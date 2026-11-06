using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WpfApp1
{
    public partial class MainWindow : Window
    {
        private readonly LoginResponse _user;
        private bool IsAdmin => _user.RoleId == 1;
        private bool IsTeacher => _user.RoleId == 2;
        private bool IsStudent => _user.RoleId == 3;

        // Справочные данные, загруженные с сервера
        private List<GroupDto> _groups = new();
        private List<SpecialtyDto> _specialties = new();
        private List<DisciplineDto> _disciplines = new();
        private List<ControlTypeDto> _controls = new();
        private List<TeacherDto> _teachers = new();
        private List<PlanDto> _plans = new();
        private List<StudentDto> _students = new();
        private List<RoleDto> _roles = new();
        private List<GradeDto> _gradesAll = new();
        private List<GradeDto> _finalAll = new();

        private int? _editGradeId;
        private int? _editFinalId;
        private int? _editUserId;
        private int? _editPlanId;
        private int? _editGroupId;
        private int? _editSpecId;
        private int? _editDiscId;
        private int? _editControlId;

        private bool _loaded;

        public MainWindow(LoginResponse user)
        {
            InitializeComponent();
            _user = user;

            LblHeaderFio.Text = user.Fio;
            LblHeaderRole.Text = user.RoleName;
            LblAvatar.Text = Initials(user.Fio);

            var values = new[] { "5", "4", "3", "2", "зачёт", "незачёт" };
            CbGValue.ItemsSource = values;
            CbFValue.ItemsSource = values;
            CbGValue.Text = "5";
            CbFValue.Text = "5";
            DpGDate.SelectedDate = DateTime.Today;
            DpFDate.SelectedDate = DateTime.Today;

            ConfigureRoles();
            LoadAvatar();
            Loaded += async (s, e) => await StartAsync();
        }

        private static string Initials(string fio)
        {
            var parts = fio.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return "?";
            if (parts.Length == 1) return parts[0].Substring(0, 1).ToUpper();
            return (parts[0].Substring(0, 1) + parts[1].Substring(0, 1)).ToUpper();
        }

        // ---------------------------------------------------------------- роли

        private void ConfigureRoles()
        {
            BtnNavStatement.Visibility = IsStudent ? Visibility.Collapsed : Visibility.Visible;
            BtnNavUsers.Visibility = IsAdmin ? Visibility.Visible : Visibility.Collapsed;
            BtnNavCatalogs.Visibility = IsAdmin ? Visibility.Visible : Visibility.Collapsed;

            GradeEditor.Visibility = IsStudent ? Visibility.Collapsed : Visibility.Visible;
            GradeFilterCard.Visibility = IsStudent ? Visibility.Collapsed : Visibility.Visible;
            ColGradeKind.Visibility = IsStudent ? Visibility.Visible : Visibility.Collapsed;
            ColGradeStudent.Visibility = IsStudent ? Visibility.Collapsed : Visibility.Visible;
            ColGradeGroup.Visibility = IsStudent ? Visibility.Collapsed : Visibility.Visible;

            if (IsStudent)
            {
                LblGradesTitle.Text = "Мои оценки";
                LblGradesSub.Text = "текущие и итоговые оценки по дисциплинам";
            }
            else if (IsTeacher)
            {
                LblGradesSub.Text = "оценки по моим дисциплинам";
            }
        }

        private async Task StartAsync()
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                await LoadReferenceAsync();
                _loaded = true;
                ShowPanel("Grades");
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Не удалось загрузить данные приложения:\n" + ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private async Task LoadReferenceAsync()
        {
            _groups = await Api.GetAsync<List<GroupDto>>("/Groups");
            _specialties = await Api.GetAsync<List<SpecialtyDto>>("/Specialties");
            _disciplines = await Api.GetAsync<List<DisciplineDto>>("/Disciplines");
            _controls = await Api.GetAsync<List<ControlTypeDto>>("/ControlTypes");
            _teachers = await Api.GetAsync<List<TeacherDto>>("/Teachers");
            _roles = await Api.GetAsync<List<RoleDto>>("/Roles");
            _plans = await Api.GetAsync<List<PlanDto>>("/Plans");
            _students = await Api.GetAsync<List<StudentDto>>("/Students");

            if (IsTeacher)
                _plans = _plans.Where(p => p.TeacherId == _user.TeacherId).ToList();

            if (IsStudent)
                _students = _students.Where(s => s.Id == _user.StudentId).ToList();

            BindFilterCombos();
        }

        // -------------------------------------------------------- навигация

        private IEnumerable<Button> NavButtons => new[]
        {
            BtnNavGrades, BtnNavStatement, BtnNavUsers, BtnNavCatalogs, BtnNavProfile
        };

        private void Nav_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button { Tag: string tag }) ShowPanel(tag);
        }

        private async void ShowPanel(string tag)
        {
            PanelGrades.Visibility = tag == "Grades" ? Visibility.Visible : Visibility.Collapsed;
            PanelStatement.Visibility = tag == "Statement" ? Visibility.Visible : Visibility.Collapsed;
            PanelUsers.Visibility = tag == "Users" ? Visibility.Visible : Visibility.Collapsed;
            PanelCatalogs.Visibility = tag == "Catalogs" ? Visibility.Visible : Visibility.Collapsed;
            PanelProfile.Visibility = tag == "Profile" ? Visibility.Visible : Visibility.Collapsed;

            foreach (var button in NavButtons)
            {
                var active = (string?)button.Tag == tag;
                button.Background = active
                    ? new SolidColorBrush(Color.FromRgb(0xE0, 0xE0, 0xE0))
                    : Brushes.Transparent;
                button.FontWeight = active ? FontWeights.SemiBold : FontWeights.Normal;
            }

            await RunAsync(async () =>
            {
                switch (tag)
                {
                    case "Grades":
                        await LoadGradesAsync();
                        break;
                    case "Statement":
                        await LoadFinalGradesAsync();
                        break;
                    case "Users":
                        await LoadUsersAsync();
                        break;
                    case "Catalogs":
                        await LoadCatalogsAsync();
                        break;
                    case "Profile":
                        await LoadProfileAsync();
                        break;
                }
            });
        }

        // ------------------------------------------------------- успеваемость

        private async Task LoadGradesAsync()
        {
            if (IsStudent)
            {
                var current = await Api.GetAsync<List<GradeDto>>($"/Grades?studentId={_user.StudentId ?? 0}");
                var final = await Api.GetAsync<List<GradeDto>>($"/FinalGrades?studentId={_user.StudentId ?? 0}");
                _gradesAll = current.Concat(final)
                    .OrderBy(g => g.DisciplineName)
                    .ThenBy(g => g.Semestr)
                    .ThenBy(g => g.Data)
                    .ToList();
            }
            else
            {
                var path = IsTeacher ? $"/Grades?teacherId={_user.TeacherId ?? 0}" : "/Grades";
                _gradesAll = await Api.GetAsync<List<GradeDto>>(path);
            }

            ApplyGradeFilter();
        }

        private void ApplyGradeFilter()
        {
            if (IsStudent)
            {
                GridGrades.ItemsSource = _gradesAll;
                return;
            }

            IEnumerable<GradeDto> query = _gradesAll;

            var groupId = SelectedId(CbGGroup);
            if (groupId > 0) query = query.Where(g => g.GroupId == groupId);

            var disciplineId = SelectedId(CbGDisc);
            if (disciplineId > 0) query = query.Where(g => g.DisciplineId == disciplineId);

            var search = TbGSearch.Text.Trim().ToLowerInvariant();
            if (search.Length > 0)
            {
                query = query.Where(g =>
                    g.StudentFio.ToLowerInvariant().Contains(search) ||
                    g.DisciplineName.ToLowerInvariant().Contains(search) ||
                    (g.Tema ?? "").ToLowerInvariant().Contains(search));
            }

            GridGrades.ItemsSource = query.ToList();
        }

        private void GradeFilter_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (!_loaded) return;
            RefreshStudentCombos();
            ApplyGradeFilter();
        }

        private void TbGSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_loaded) return;
            ApplyGradeFilter();
        }

        private async void BtnGRefresh_Click(object sender, RoutedEventArgs e) =>
            await RunAsync(LoadGradesAsync);

        private object? ReadGradeEditor()
        {
            var studentId = SelectedId(CbGStudent);
            var planId = SelectedId(CbGPlan);
            var value = (CbGValue.Text ?? "").Trim();

            if (studentId <= 0)
            {
                MessageBox.Show(this, "Выберите студента", "Проверка", MessageBoxButton.OK, MessageBoxImage.Information);
                return null;
            }
            if (planId <= 0)
            {
                MessageBox.Show(this, "Выберите дисциплину", "Проверка", MessageBoxButton.OK, MessageBoxImage.Information);
                return null;
            }
            if (value.Length == 0)
            {
                MessageBox.Show(this, "Укажите оценку", "Проверка", MessageBoxButton.OK, MessageBoxImage.Information);
                return null;
            }

            return new
            {
                studentId,
                planId,
                ocenka = value,
                data = DpGDate.SelectedDate ?? DateTime.Today,
                tema = string.IsNullOrWhiteSpace(TbGTema.Text) ? null : TbGTema.Text.Trim()
            };
        }

        private async void BtnGAdd_Click(object sender, RoutedEventArgs e) => await RunAsync(async () =>
        {
            var body = ReadGradeEditor();
            if (body == null) return;

            await Api.PostAsync("/Grades", body);
            await LoadGradesAsync();
            ClearGradeEditor();
        });

        private async void BtnGSave_Click(object sender, RoutedEventArgs e) => await RunAsync(async () =>
        {
            if (_editGradeId == null)
            {
                MessageBox.Show(this, "Выберите оценку в таблице", "Проверка", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var body = ReadGradeEditor();
            if (body == null) return;

            await Api.PutAsync($"/Grades/{_editGradeId}", body);
            await LoadGradesAsync();
            ClearGradeEditor();
        });

        private async void BtnGDelete_Click(object sender, RoutedEventArgs e) => await RunAsync(async () =>
        {
            if (_editGradeId == null)
            {
                MessageBox.Show(this, "Выберите оценку в таблице", "Проверка", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (MessageBox.Show(this, "Удалить выбранную оценку?", "Подтверждение",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;

            await Api.DeleteAsync($"/Grades/{_editGradeId}");
            await LoadGradesAsync();
            ClearGradeEditor();
        });

        private void BtnGClear_Click(object sender, RoutedEventArgs e) => ClearGradeEditor();

        private void ClearGradeEditor()
        {
            _editGradeId = null;
            GridGrades.SelectedItem = null;
            CbGValue.Text = "5";
            TbGTema.Clear();
            DpGDate.SelectedDate = DateTime.Today;
        }

        private void GridGrades_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (GridGrades.SelectedItem is not GradeDto grade) return;

            _editGradeId = grade.Id;
            SelectId(CbGStudent, grade.StudentId);
            SelectId(CbGPlan, grade.PlanId);
            CbGValue.Text = grade.Ocenka;
            DpGDate.SelectedDate = grade.Data ?? DateTime.Today;
            TbGTema.Text = grade.Tema ?? "";
        }

        // ---------------------------------------------------------- ведомость

        private async Task LoadFinalGradesAsync()
        {
            var path = IsTeacher ? $"/FinalGrades?teacherId={_user.TeacherId ?? 0}" : "/FinalGrades";
            _finalAll = await Api.GetAsync<List<GradeDto>>(path);
            ApplyFinalFilter();
        }

        private void ApplyFinalFilter()
        {
            IEnumerable<GradeDto> query = _finalAll;

            var groupId = SelectedId(CbSGroup);
            if (groupId > 0) query = query.Where(g => g.GroupId == groupId);

            var disciplineId = SelectedId(CbSDisc);
            if (disciplineId > 0) query = query.Where(g => g.DisciplineId == disciplineId);

            GridFinal.ItemsSource = query.ToList();
        }

        private void StatementFilter_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (!_loaded) return;
            RefreshStatementStudents();
            ApplyFinalFilter();
        }

        private async void BtnSRefresh_Click(object sender, RoutedEventArgs e) =>
            await RunAsync(LoadFinalGradesAsync);

        private object? ReadFinalEditor()
        {
            var studentId = SelectedId(CbFStudent);
            var planId = SelectedId(CbFPlan);
            var value = (CbFValue.Text ?? "").Trim();

            if (studentId <= 0)
            {
                MessageBox.Show(this, "Выберите студента", "Проверка", MessageBoxButton.OK, MessageBoxImage.Information);
                return null;
            }
            if (planId <= 0)
            {
                MessageBox.Show(this, "Выберите дисциплину", "Проверка", MessageBoxButton.OK, MessageBoxImage.Information);
                return null;
            }
            if (value.Length == 0)
            {
                MessageBox.Show(this, "Укажите оценку", "Проверка", MessageBoxButton.OK, MessageBoxImage.Information);
                return null;
            }

            return new
            {
                studentId,
                planId,
                ocenka = value,
                data = DpFDate.SelectedDate ?? DateTime.Today
            };
        }

        private async void BtnFAdd_Click(object sender, RoutedEventArgs e) => await RunAsync(async () =>
        {
            var body = ReadFinalEditor();
            if (body == null) return;

            await Api.PostAsync("/FinalGrades", body);
            await LoadFinalGradesAsync();
            ClearFinalEditor();
        });

        private async void BtnFSave_Click(object sender, RoutedEventArgs e) => await RunAsync(async () =>
        {
            if (_editFinalId == null)
            {
                MessageBox.Show(this, "Выберите строку в таблице", "Проверка", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var body = ReadFinalEditor();
            if (body == null) return;

            await Api.PutAsync($"/FinalGrades/{_editFinalId}", body);
            await LoadFinalGradesAsync();
            ClearFinalEditor();
        });

        private async void BtnFDelete_Click(object sender, RoutedEventArgs e) => await RunAsync(async () =>
        {
            if (_editFinalId == null)
            {
                MessageBox.Show(this, "Выберите строку в таблице", "Проверка", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (MessageBox.Show(this, "Удалить выбранную итоговую оценку?", "Подтверждение",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;

            await Api.DeleteAsync($"/FinalGrades/{_editFinalId}");
            await LoadFinalGradesAsync();
            ClearFinalEditor();
        });

        private void BtnFClear_Click(object sender, RoutedEventArgs e) => ClearFinalEditor();

        private void ClearFinalEditor()
        {
            _editFinalId = null;
            GridFinal.SelectedItem = null;
            CbFValue.Text = "5";
            DpFDate.SelectedDate = DateTime.Today;
        }

        private void GridFinal_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (GridFinal.SelectedItem is not GradeDto grade) return;

            _editFinalId = grade.Id;
            SelectId(CbFStudent, grade.StudentId);
            SelectId(CbFPlan, grade.PlanId);
            CbFValue.Text = grade.Ocenka;
            DpFDate.SelectedDate = grade.Data ?? DateTime.Today;
        }

        // --------------------------------------------------------- пользователи

        private async Task LoadUsersAsync()
        {
            var users = await Api.GetAsync<List<UserDto>>("/Users");
            GridUsers.ItemsSource = users;

            _students = await Api.GetAsync<List<StudentDto>>("/Students");
            _groups = await Api.GetAsync<List<GroupDto>>("/Groups");

            RebindUserGroups();
            RefreshStudentCombos();
            RefreshStatementStudents();
        }

        private void GridUsers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (GridUsers.SelectedItem is not UserDto user) return;

            _editUserId = user.Id;
            TbUFam.Text = user.Familiya;
            TbUName.Text = user.Imya;
            TbUPatr.Text = user.Otchestvo ?? "";
            TbULogin.Text = user.Login;
            TbUPass.Text = "";
            SelectId(CbURole, user.RoleId);
            SelectId(CbUGroup, user.GroupId ?? 0);
            TbUZachetka.Text = user.NomerZachetki ?? "";
            LblUserInfo.Text = "";
            ToggleUserRoleFields();
        }

        private void UserRole_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (!_loaded) return;
            ToggleUserRoleFields();
        }

        private void ToggleUserRoleFields()
        {
            var isStudentRole = SelectedId(CbURole) == 3;
            CbUGroup.IsEnabled = isStudentRole;
            TbUZachetka.IsEnabled = isStudentRole;

            if (!isStudentRole && CbUGroup.SelectedIndex != 0)
                CbUGroup.SelectedIndex = 0;
        }

        private void BtnUNew_Click(object sender, RoutedEventArgs e)
        {
            _editUserId = null;
            GridUsers.SelectedItem = null;
            TbUFam.Clear();
            TbUName.Clear();
            TbUPatr.Clear();
            TbULogin.Clear();
            TbUPass.Clear();
            TbUZachetka.Clear();
            SelectId(CbURole, 3);
            SelectId(CbUGroup, 0);
            LblUserInfo.Text = "";
            ToggleUserRoleFields();
            TbUFam.Focus();
        }

        private async void BtnUSave_Click(object sender, RoutedEventArgs e)
        {
            LblUserInfo.Text = "";

            if (string.IsNullOrWhiteSpace(TbUFam.Text) || string.IsNullOrWhiteSpace(TbUName.Text) ||
                string.IsNullOrWhiteSpace(TbULogin.Text))
            {
                SetUserInfo("Заполните фамилию, имя и логин", false);
                return;
            }

            var roleId = SelectedId(CbURole);
            if (roleId <= 0)
            {
                SetUserInfo("Выберите роль", false);
                return;
            }

            var isStudentRole = roleId == 3;
            var groupId = isStudentRole ? SelectedId(CbUGroup) : 0;
            if (isStudentRole && groupId <= 0)
            {
                SetUserInfo("Выберите группу студента", false);
                return;
            }

            var body = new
            {
                roleId,
                login = TbULogin.Text.Trim(),
                parol = TbUPass.Text.Trim(),
                familiya = TbUFam.Text.Trim(),
                imya = TbUName.Text.Trim(),
                otchestvo = string.IsNullOrWhiteSpace(TbUPatr.Text) ? null : TbUPatr.Text.Trim(),
                groupId = isStudentRole ? (int?)groupId : null,
                nomerZachetki = isStudentRole && !string.IsNullOrWhiteSpace(TbUZachetka.Text)
                    ? TbUZachetka.Text.Trim()
                    : null
            };

            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                if (_editUserId == null)
                {
                    var created = await Api.PostAsync<UserDto>("/Users", body);
                    await LoadUsersAsync();
                    SelectUserRow(created.Id);
                    SetUserInfo("Пользователь создан", true);
                }
                else
                {
                    var id = _editUserId.Value;
                    await Api.PutAsync($"/Users/{id}", body);
                    await LoadUsersAsync();
                    SelectUserRow(id);
                    SetUserInfo("Изменения сохранены", true);
                }
            }
            catch (Exception ex)
            {
                SetUserInfo(ex.Message, false);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private async void BtnUDelete_Click(object sender, RoutedEventArgs e) => await RunAsync(async () =>
        {
            if (_editUserId == null)
            {
                MessageBox.Show(this, "Выберите пользователя в таблице", "Проверка",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (MessageBox.Show(this, "Удалить пользователя вместе с его оценками?", "Подтверждение",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;

            await Api.DeleteAsync($"/Users/{_editUserId}");
            await LoadUsersAsync();
            BtnUNew_Click(sender, e);
            SetUserInfo("Пользователь удалён", true);
        });

        private void SelectUserRow(int id)
        {
            if (GridUsers.ItemsSource is not List<UserDto> users) return;
            var user = users.FirstOrDefault(u => u.Id == id);
            if (user != null) GridUsers.SelectedItem = user;
        }

        private void SetUserInfo(string text, bool success)
        {
            LblUserInfo.Text = text;
            LblUserInfo.Foreground = new SolidColorBrush(success
                ? Color.FromRgb(0x1B, 0x5E, 0x20)
                : Color.FromRgb(0xC6, 0x28, 0x28));
        }

        private void RebindUserGroups()
        {
            var selected = SelectedId(CbUGroup);
            var options = new List<GroupDto> { new() { Id = 0, Name = "— не выбрана —" } };
            options.AddRange(_groups);

            CbUGroup.ItemsSource = options;
            SelectId(CbUGroup, selected);
            if (CbUGroup.SelectedItem == null) CbUGroup.SelectedIndex = 0;
        }

        // ------------------------------------------------------- учебный план

        private void GridPlans_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (GridPlans.SelectedItem is not PlanDto plan) return;

            _editPlanId = plan.Id;
            SelectId(CbPSpec, plan.SpecialtyId);
            SelectId(CbPDisc, plan.DisciplineId);
            SelectId(CbPTeacher, plan.TeacherId);
            SelectId(CbPControl, plan.ControlTypeId);
            TbPSemestr.Text = plan.Semestr.ToString();
            TbPHours.Text = plan.Hours.ToString();
        }

        private void BtnPNew_Click(object sender, RoutedEventArgs e)
        {
            _editPlanId = null;
            GridPlans.SelectedItem = null;
            TbPSemestr.Text = "1";
            TbPHours.Text = "72";
        }

        private async void BtnPSave_Click(object sender, RoutedEventArgs e) => await RunAsync(async () =>
        {
            if (SelectedId(CbPSpec) <= 0 || SelectedId(CbPDisc) <= 0 ||
                SelectedId(CbPTeacher) <= 0 || SelectedId(CbPControl) <= 0)
            {
                MessageBox.Show(this, "Заполните все поля учебного плана", "Проверка",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (!int.TryParse(TbPSemestr.Text, out var semestr) || semestr < 1 || semestr > 12)
            {
                MessageBox.Show(this, "Семестр должен быть числом от 1 до 12", "Проверка",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (!int.TryParse(TbPHours.Text, out var hours) || hours < 0)
            {
                MessageBox.Show(this, "Количество часов указано неверно", "Проверка",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var body = new
            {
                specialtyId = SelectedId(CbPSpec),
                disciplineId = SelectedId(CbPDisc),
                teacherId = SelectedId(CbPTeacher),
                semestr,
                hours,
                controlTypeId = SelectedId(CbPControl)
            };

            if (_editPlanId == null) await Api.PostAsync<PlanDto>("/Plans", body);
            else await Api.PutAsync($"/Plans/{_editPlanId}", body);

            await LoadCatalogsAsync();
            BtnPNew_Click(sender, e);
        });

        private async void BtnPDelete_Click(object sender, RoutedEventArgs e) => await RunAsync(async () =>
        {
            if (_editPlanId == null)
            {
                MessageBox.Show(this, "Выберите строку учебного плана", "Проверка",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (MessageBox.Show(this, "Удалить строку учебного плана?", "Подтверждение",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;

            await Api.DeleteAsync($"/Plans/{_editPlanId}");
            await LoadCatalogsAsync();
            BtnPNew_Click(sender, e);
        });

        // --------------------------------------------------------- справочники

        private async Task LoadCatalogsAsync()
        {
            _groups = await Api.GetAsync<List<GroupDto>>("/Groups");
            _specialties = await Api.GetAsync<List<SpecialtyDto>>("/Specialties");
            _disciplines = await Api.GetAsync<List<DisciplineDto>>("/Disciplines");
            _controls = await Api.GetAsync<List<ControlTypeDto>>("/ControlTypes");
            _teachers = await Api.GetAsync<List<TeacherDto>>("/Teachers");
            _students = await Api.GetAsync<List<StudentDto>>("/Students");

            var plans = await Api.GetAsync<List<PlanDto>>("/Plans");
            _plans = IsTeacher ? plans.Where(p => p.TeacherId == _user.TeacherId).ToList() : plans;

            GridCatGroups.ItemsSource = _groups;
            GridCatSpecs.ItemsSource = _specialties;
            GridCatDiscs.ItemsSource = _disciplines;
            GridCatControls.ItemsSource = _controls;
            GridPlans.ItemsSource = plans;

            BindFilterCombos();
        }

        private void GridCatGroups_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (GridCatGroups.SelectedItem is not GroupDto group) return;
            _editGroupId = group.Id;
            TbCatGroupName.Text = group.Name;
            SelectId(CbCatGroupSpec, group.SpecialtyId);
        }

        private async void BtnCatGroupAdd_Click(object sender, RoutedEventArgs e) => await RunAsync(async () =>
        {
            if (string.IsNullOrWhiteSpace(TbCatGroupName.Text) || SelectedId(CbCatGroupSpec) <= 0)
            {
                MessageBox.Show(this, "Укажите название группы и специальность", "Проверка",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            await Api.PostAsync<GroupDto>("/Groups", new
            {
                name = TbCatGroupName.Text.Trim(),
                specialtyId = SelectedId(CbCatGroupSpec)
            });
            await LoadCatalogsAsync();
            ClearGroupEditor();
        });

        private async void BtnCatGroupSave_Click(object sender, RoutedEventArgs e) => await RunAsync(async () =>
        {
            if (_editGroupId == null)
            {
                MessageBox.Show(this, "Выберите группу в таблице", "Проверка",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            await Api.PutAsync($"/Groups/{_editGroupId}", new
            {
                name = TbCatGroupName.Text.Trim(),
                specialtyId = SelectedId(CbCatGroupSpec)
            });
            await LoadCatalogsAsync();
            ClearGroupEditor();
        });

        private async void BtnCatGroupDelete_Click(object sender, RoutedEventArgs e) => await RunAsync(async () =>
        {
            if (_editGroupId == null) return;
            if (MessageBox.Show(this, "Удалить группу?", "Подтверждение",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;

            await Api.DeleteAsync($"/Groups/{_editGroupId}");
            await LoadCatalogsAsync();
            ClearGroupEditor();
        });

        private void ClearGroupEditor()
        {
            _editGroupId = null;
            GridCatGroups.SelectedItem = null;
            TbCatGroupName.Clear();
        }

        private void GridCatSpecs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (GridCatSpecs.SelectedItem is not SpecialtyDto specialty) return;
            _editSpecId = specialty.Id;
            TbCatSpecKod.Text = specialty.Kod;
            TbCatSpecName.Text = specialty.Name;
        }

        private async void BtnCatSpecAdd_Click(object sender, RoutedEventArgs e) => await RunAsync(async () =>
        {
            if (string.IsNullOrWhiteSpace(TbCatSpecKod.Text) || string.IsNullOrWhiteSpace(TbCatSpecName.Text))
            {
                MessageBox.Show(this, "Укажите код и название специальности", "Проверка",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            await Api.PostAsync<SpecialtyDto>("/Specialties", new
            {
                kod = TbCatSpecKod.Text.Trim(),
                name = TbCatSpecName.Text.Trim()
            });
            await LoadCatalogsAsync();
            ClearSpecEditor();
        });

        private async void BtnCatSpecSave_Click(object sender, RoutedEventArgs e) => await RunAsync(async () =>
        {
            if (_editSpecId == null)
            {
                MessageBox.Show(this, "Выберите специальность в таблице", "Проверка",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            await Api.PutAsync($"/Specialties/{_editSpecId}", new
            {
                kod = TbCatSpecKod.Text.Trim(),
                name = TbCatSpecName.Text.Trim()
            });
            await LoadCatalogsAsync();
            ClearSpecEditor();
        });

        private async void BtnCatSpecDelete_Click(object sender, RoutedEventArgs e) => await RunAsync(async () =>
        {
            if (_editSpecId == null) return;
            if (MessageBox.Show(this, "Удалить специальность?", "Подтверждение",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;

            await Api.DeleteAsync($"/Specialties/{_editSpecId}");
            await LoadCatalogsAsync();
            ClearSpecEditor();
        });

        private void ClearSpecEditor()
        {
            _editSpecId = null;
            GridCatSpecs.SelectedItem = null;
            TbCatSpecKod.Clear();
            TbCatSpecName.Clear();
        }

        private void GridCatDiscs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (GridCatDiscs.SelectedItem is not DisciplineDto discipline) return;
            _editDiscId = discipline.Id;
            TbCatDiscName.Text = discipline.Name;
        }

        private async void BtnCatDiscAdd_Click(object sender, RoutedEventArgs e) => await RunAsync(async () =>
        {
            if (string.IsNullOrWhiteSpace(TbCatDiscName.Text))
            {
                MessageBox.Show(this, "Укажите название дисциплины", "Проверка",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            await Api.PostAsync<DisciplineDto>("/Disciplines", new { name = TbCatDiscName.Text.Trim() });
            await LoadCatalogsAsync();
            ClearDiscEditor();
        });

        private async void BtnCatDiscSave_Click(object sender, RoutedEventArgs e) => await RunAsync(async () =>
        {
            if (_editDiscId == null)
            {
                MessageBox.Show(this, "Выберите дисциплину в таблице", "Проверка",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            await Api.PutAsync($"/Disciplines/{_editDiscId}", new { name = TbCatDiscName.Text.Trim() });
            await LoadCatalogsAsync();
            ClearDiscEditor();
        });

        private async void BtnCatDiscDelete_Click(object sender, RoutedEventArgs e) => await RunAsync(async () =>
        {
            if (_editDiscId == null) return;
            if (MessageBox.Show(this, "Удалить дисциплину?", "Подтверждение",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;

            await Api.DeleteAsync($"/Disciplines/{_editDiscId}");
            await LoadCatalogsAsync();
            ClearDiscEditor();
        });

        private void ClearDiscEditor()
        {
            _editDiscId = null;
            GridCatDiscs.SelectedItem = null;
            TbCatDiscName.Clear();
        }

        private void GridCatControls_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (GridCatControls.SelectedItem is not ControlTypeDto control) return;
            _editControlId = control.Id;
            TbCatControlName.Text = control.Name;
            TbCatControlTip.Text = control.Tip.ToString();
        }

        private async void BtnCatControlAdd_Click(object sender, RoutedEventArgs e) => await RunAsync(async () =>
        {
            if (string.IsNullOrWhiteSpace(TbCatControlName.Text))
            {
                MessageBox.Show(this, "Укажите название вида контроля", "Проверка",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (!int.TryParse(TbCatControlTip.Text, out var tip) || tip <= 0) tip = 1;

            await Api.PostAsync<ControlTypeDto>("/ControlTypes", new
            {
                name = TbCatControlName.Text.Trim(),
                tip
            });
            await LoadCatalogsAsync();
            ClearControlEditor();
        });

        private async void BtnCatControlSave_Click(object sender, RoutedEventArgs e) => await RunAsync(async () =>
        {
            if (_editControlId == null)
            {
                MessageBox.Show(this, "Выберите вид контроля в таблице", "Проверка",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (!int.TryParse(TbCatControlTip.Text, out var tip) || tip <= 0) tip = 1;

            await Api.PutAsync($"/ControlTypes/{_editControlId}", new
            {
                name = TbCatControlName.Text.Trim(),
                tip
            });
            await LoadCatalogsAsync();
            ClearControlEditor();
        });

        private async void BtnCatControlDelete_Click(object sender, RoutedEventArgs e) => await RunAsync(async () =>
        {
            if (_editControlId == null) return;
            if (MessageBox.Show(this, "Удалить вид контроля?", "Подтверждение",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;

            await Api.DeleteAsync($"/ControlTypes/{_editControlId}");
            await LoadCatalogsAsync();
            ClearControlEditor();
        });

        private void ClearControlEditor()
        {
            _editControlId = null;
            GridCatControls.SelectedItem = null;
            TbCatControlName.Clear();
            TbCatControlTip.Clear();
        }

        // ------------------------------------------------------------- профиль

        private async Task LoadProfileAsync()
        {
            LoadAvatar();
            LblProfileFio.Text = _user.Fio;
            LblProfileRole.Text = _user.RoleName;
            LblProfileLogin.Text = "Логин: " + _user.Login;
            LblProfileAvg.Visibility = Visibility.Collapsed;

            if (IsStudent)
            {
                LblProfileExtra.Text = $"Группа: {_user.GroupName} · Зачётная книжка: {_user.NomerZachetki}";

                var progress = await Api.GetAsync<StudentProgressDto>($"/Reports/student/{_user.StudentId ?? 0}");
                if (progress.Average.HasValue)
                {
                    LblProfileAvg.Visibility = Visibility.Visible;
                }
            }
            else
            {
                LblProfileExtra.Text = "";
            }
        }

        private async void BtnChangePass_Click(object sender, RoutedEventArgs e) => await RunAsync(async () =>
        {
            LblPassInfo.Text = "";

            if (string.IsNullOrWhiteSpace(PbOld.Password) || string.IsNullOrWhiteSpace(PbNew.Password))
            {
                LblPassInfo.Foreground = new SolidColorBrush(Color.FromRgb(0xC6, 0x28, 0x28));
                LblPassInfo.Text = "Заполните все поля";
                return;
            }
            if (PbNew.Password != PbNew2.Password)
            {
                LblPassInfo.Foreground = new SolidColorBrush(Color.FromRgb(0xC6, 0x28, 0x28));
                LblPassInfo.Text = "Новые пароли не совпадают";
                return;
            }

            await Api.PostAsync("/Auth/change-password", new
            {
                userId = _user.UserId,
                oldParol = PbOld.Password,
                newParol = PbNew.Password
            });

            PbOld.Clear();
            PbNew.Clear();
            PbNew2.Clear();
            LblPassInfo.Foreground = new SolidColorBrush(Color.FromRgb(0x1B, 0x5E, 0x20));
            LblPassInfo.Text = "Пароль изменён";
        });

        // ------------------------------------------------------------- аватарка

        private void LoadAvatar()
        {
            var image = TryLoadImage(ResolveAvatarPath(_user.FotoUrl));

            if (image != null)
            {
                ImgAvatar.Source = image;
                ImgAvatar.Visibility = Visibility.Visible;
                LblAvatar.Visibility = Visibility.Collapsed;
            }
            else
            {
                ImgAvatar.Source = null;
                ImgAvatar.Visibility = Visibility.Collapsed;
                LblAvatar.Visibility = Visibility.Visible;
                LblAvatar.Text = Initials(_user.Fio);
            }
        }

        // Своё фото — если файл существует, иначе общая картинка из папки программы.
        private static string? ResolveAvatarPath(string? fotoUrl)
        {
            if (!string.IsNullOrWhiteSpace(fotoUrl) && File.Exists(fotoUrl)) return fotoUrl;

            var bundled = Path.Combine(AppContext.BaseDirectory, "avatar.jpg");
            return File.Exists(bundled) ? bundled : null;
        }

        private static BitmapImage? TryLoadImage(string? path)
        {
            if (string.IsNullOrWhiteSpace(path)) return null;

            try
            {
                var image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.UriSource = new Uri(path);
                image.EndInit();
                image.Freeze();
                return image;
            }
            catch
            {
                return null;
            }
        }

        private async void BtnAvatar_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Выбор аватарки",
                Filter = "Изображения (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp"
            };

            if (dialog.ShowDialog(this) != true) return;

            LblAvatarInfo.Text = "";

            try
            {
                var folder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "CollegeDiary", "avatars");
                Directory.CreateDirectory(folder);

                // Старое фото этого пользователя удаляем, чтобы не копился мусор.
                foreach (var old in Directory.GetFiles(folder, "user" + _user.UserId + "_*"))
                    File.Delete(old);

                var extension = Path.GetExtension(dialog.FileName).ToLowerInvariant();
                if (extension != ".jpg" && extension != ".jpeg" &&
                    extension != ".png" && extension != ".bmp")
                    extension = ".jpg";

                var target = Path.Combine(folder,
                    "user" + _user.UserId + "_" + DateTime.Now.ToString("yyyyMMddHHmmss") + extension);

                File.Copy(dialog.FileName, target, true);
                await Api.PostAsync("/Users/" + _user.UserId + "/avatar", new { fotoUrl = target });

                _user.FotoUrl = target;
                LoadAvatar();

                LblAvatarInfo.Foreground = new SolidColorBrush(Color.FromRgb(0x1B, 0x5E, 0x20));
                LblAvatarInfo.Text = "Фото обновлено";
            }
            catch (Exception ex)
            {
                LblAvatarInfo.Foreground = new SolidColorBrush(Color.FromRgb(0xC6, 0x28, 0x28));
                LblAvatarInfo.Text = ex is ApiException ? ex.Message : "Не удалось загрузить фото";
            }
        }

        // ------------------------------------------------------------ выход

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            new LoginWindow().Show();
            Close();
        }

        // --------------------------------------------------------- утилиты

        private void BindFilterCombos()
        {
            var groupId = SelectedId(CbGGroup);
            var statementGroupId = SelectedId(CbSGroup);
            var gradeDiscId = SelectedId(CbGDisc);
            var statementDiscId = SelectedId(CbSDisc);
            var roleId = SelectedId(CbURole);
            var userGroupId = SelectedId(CbUGroup);

            var groupFilter = new List<GroupDto> { new() { Id = 0, Name = "Все группы" } };
            groupFilter.AddRange(_groups);

            var disciplineFilter = new List<DisciplineDto> { new() { Id = 0, Name = "Все дисциплины" } };
            disciplineFilter.AddRange(_disciplines);

            CbGGroup.ItemsSource = groupFilter;
            CbSGroup.ItemsSource = new List<GroupDto>(groupFilter);
            CbGDisc.ItemsSource = disciplineFilter;
            CbSDisc.ItemsSource = new List<DisciplineDto>(disciplineFilter);
            CbGPlan.ItemsSource = _plans;
            CbFPlan.ItemsSource = _plans;
            CbCatGroupSpec.ItemsSource = _specialties;
            CbPSpec.ItemsSource = _specialties;
            CbPDisc.ItemsSource = _disciplines;
            CbPTeacher.ItemsSource = _teachers;
            CbPControl.ItemsSource = _controls;
            CbURole.ItemsSource = _roles;
            RebindUserGroups();

            SelectId(CbGGroup, groupId);
            SelectId(CbSGroup, statementGroupId);
            SelectId(CbGDisc, gradeDiscId);
            SelectId(CbSDisc, statementDiscId);
            SelectId(CbURole, roleId > 0 ? roleId : 3);
            SelectId(CbUGroup, userGroupId);

            if (CbGGroup.SelectedItem == null && CbGGroup.Items.Count > 0) CbGGroup.SelectedIndex = 0;
            if (CbSGroup.SelectedItem == null && CbSGroup.Items.Count > 0) CbSGroup.SelectedIndex = 0;
            if (CbGDisc.SelectedItem == null && CbGDisc.Items.Count > 0) CbGDisc.SelectedIndex = 0;
            if (CbSDisc.SelectedItem == null && CbSDisc.Items.Count > 0) CbSDisc.SelectedIndex = 0;
            if (CbURole.SelectedItem == null) SelectId(CbURole, 3);
            if (CbUGroup.SelectedItem == null && CbUGroup.Items.Count > 0) CbUGroup.SelectedIndex = 0;

            if (CbGPlan.SelectedItem == null && CbGPlan.Items.Count > 0) CbGPlan.SelectedIndex = 0;
            if (CbFPlan.SelectedItem == null && CbFPlan.Items.Count > 0) CbFPlan.SelectedIndex = 0;
            if (CbPSpec.SelectedItem == null && CbPSpec.Items.Count > 0) CbPSpec.SelectedIndex = 0;
            if (CbPDisc.SelectedItem == null && CbPDisc.Items.Count > 0) CbPDisc.SelectedIndex = 0;
            if (CbPTeacher.SelectedItem == null && CbPTeacher.Items.Count > 0) CbPTeacher.SelectedIndex = 0;
            if (CbPControl.SelectedItem == null && CbPControl.Items.Count > 0) CbPControl.SelectedIndex = 0;
            if (CbCatGroupSpec.SelectedItem == null && CbCatGroupSpec.Items.Count > 0) CbCatGroupSpec.SelectedIndex = 0;
            if (string.IsNullOrWhiteSpace(TbPSemestr.Text)) TbPSemestr.Text = "1";
            if (string.IsNullOrWhiteSpace(TbPHours.Text)) TbPHours.Text = "72";

            RefreshStudentCombos();
            RefreshStatementStudents();
        }

        private void RefreshStudentCombos()
        {
            if (IsStudent)
            {
                CbGStudent.ItemsSource = _students;
                CbFStudent.ItemsSource = _students;
                if (_students.Count > 0) CbGStudent.SelectedIndex = 0;
                return;
            }

            var gradeGroupId = SelectedId(CbGGroup);
            var gradeStudents = gradeGroupId > 0
                ? _students.Where(s => s.GroupId == gradeGroupId).ToList()
                : _students.ToList();

            var previous = SelectedId(CbGStudent);
            CbGStudent.ItemsSource = gradeStudents;
            SelectId(CbGStudent, previous);
            if (CbGStudent.SelectedItem == null && gradeStudents.Count > 0) CbGStudent.SelectedIndex = 0;
        }

        private void RefreshStatementStudents()
        {
            var groupId = SelectedId(CbSGroup);
            var students = groupId > 0
                ? _students.Where(s => s.GroupId == groupId).ToList()
                : _students.ToList();

            var previous = SelectedId(CbFStudent);
            CbFStudent.ItemsSource = students;
            SelectId(CbFStudent, previous);
            if (CbFStudent.SelectedItem == null && students.Count > 0) CbFStudent.SelectedIndex = 0;
        }

        private static int SelectedId(ComboBox comboBox)
        {
            if (comboBox.SelectedItem == null) return 0;
            var property = comboBox.SelectedItem.GetType().GetProperty("Id");
            return property?.GetValue(comboBox.SelectedItem) is int id ? id : 0;
        }

        private static void SelectId(ComboBox comboBox, int id)
        {
            foreach (var item in comboBox.Items)
            {
                var property = item?.GetType().GetProperty("Id");
                if (property?.GetValue(item) is int itemId && itemId == id)
                {
                    comboBox.SelectedItem = item;
                    return;
                }
            }
            comboBox.SelectedItem = null;
        }

        private async Task RunAsync(Func<Task> action)
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                await action();
            }
            catch (ApiException ex)
            {
                MessageBox.Show(this, ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }
    }
}
