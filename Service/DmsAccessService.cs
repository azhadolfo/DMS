using Document_Management.Models;

namespace Document_Management.Service
{
    public interface IDmsAccessService
    {
        string? Username { get; }
        bool IsAuthenticated();
        bool IsAdmin();
        bool CanAccessCompany(string company);
        bool CanAccessDepartment(string department);
        bool CanUpload();
        bool CanAccessTrash();
        bool CanMutate(FileDocument fileDocument);
    }

    public class DmsAccessService : IDmsAccessService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public DmsAccessService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string? Username => _httpContextAccessor.HttpContext?.Session.GetString("username");

        private string? UserRole => _httpContextAccessor.HttpContext?.Session.GetString("userRole")?.ToLowerInvariant();

        public bool IsAuthenticated()
        {
            return !string.IsNullOrWhiteSpace(Username);
        }

        public bool IsAdmin()
        {
            return UserRole == "admin";
        }

        public bool CanAccessCompany(string company)
        {
            if (!IsAuthenticated())
            {
                return false;
            }

            if (IsAdmin())
            {
                return true;
            }

            return GetAccessValues("userAccessCompanies").Contains(company, StringComparer.OrdinalIgnoreCase);
        }

        public bool CanAccessDepartment(string department)
        {
            if (!IsAuthenticated())
            {
                return false;
            }

            if (IsAdmin())
            {
                return true;
            }

            return GetAccessValues("userAccessDepartments").Contains(department, StringComparer.OrdinalIgnoreCase);
        }

        public bool CanUpload()
        {
            return IsAuthenticated() && (IsAdmin() || UserRole == "uploader");
        }

        public bool CanAccessTrash()
        {
            return IsAuthenticated() && (IsAdmin() || UserRole == "uploader");
        }

        public bool CanMutate(FileDocument fileDocument)
        {
            if (!IsAuthenticated())
            {
                return false;
            }

            return IsAdmin()
                || UserRole == "uploader"
                || string.Equals(fileDocument.Username, Username, StringComparison.OrdinalIgnoreCase);
        }

        private IReadOnlyCollection<string> GetAccessValues(string sessionKey)
        {
            var rawValue = _httpContextAccessor.HttpContext?.Session.GetString(sessionKey);
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return [];
            }

            return rawValue
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .ToArray();
        }
    }
}
