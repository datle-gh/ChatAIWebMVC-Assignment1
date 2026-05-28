namespace Presentation.Models;

public sealed class AdminUserIndexViewModel
{
    public IReadOnlyList<AdminUserListItemViewModel> Users { get; set; } = [];
}
