namespace EduAI.Desktop.Services
{
    public class AuthService
    {
        public UserModel? CurrentUser { get; private set; }
        public string? Token { get; private set; }
        public event Action? OnChange;
        private void NotifyStateChanged() => OnChange?.Invoke();
        public void SetLogin(UserModel user, string token)
        {
            CurrentUser = user;
            Token = token;
            NotifyStateChanged();
        }

        public void Logout()
        {
            CurrentUser = null;
            Token = null;
            NotifyStateChanged();
        }
        }

    public class UserModel
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public int GradeId { get; set; }
        public int ClassGrade { get; set; }
        public string Role { get; set; } = "Student";
        public bool CanCreateExam { get; set; } = false;
    }
}