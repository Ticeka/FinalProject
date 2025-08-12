using Microsoft.AspNetCore.Identity;

namespace FinalProject.Models
{
    // เพิ่มฟิลด์ได้ตามต้องการ เช่น DisplayName
    public class ApplicationUser : IdentityUser
    {
        public string? DisplayName { get; set; }
    }
}
