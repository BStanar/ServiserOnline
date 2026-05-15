using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace ServiserOnline.Models;

public class LoginViewModel
{
    [Required]
    [Display(Name = "Email")]
    [EmailAddress]
    public string Email { get; set; }

    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Lozinka")]
    public string Password { get; set; }

    [Display(Name = "Zapamti me")]
    public bool RememberMe { get; set; }
}

public class RegisterViewModel
{
    [Required]
    [EmailAddress]
    [Display(Name = "Email")]
    public string Email { get; set; }

    [Required]
    [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
    [DataType(DataType.Password)]
    [Display(Name = "Lozinka")]
    public string Password { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "Potvrdi lozinku")]
    [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
    public string ConfirmPassword { get; set; }
}

public class ForgotPasswordViewModel
{
    [Required]
    [EmailAddress]
    [Display(Name = "Email")]
    public string Email { get; set; }
}

public class ResetPasswordViewModel
{
    [Required]
    [EmailAddress]
    [Display(Name = "Email")]
    public string Email { get; set; }

    [Required]
    [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
    [DataType(DataType.Password)]
    [Display(Name = "Lozinka")]
    public string Password { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "Potvrdi lozinku")]
    [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
    public string ConfirmPassword { get; set; }

    public string Code { get; set; }
}

public class ChangePasswordViewModel
{
    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Current password")]
    public string OldPassword { get; set; }

    [Required]
    [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
    [DataType(DataType.Password)]
    [Display(Name = "New password")]
    public string NewPassword { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "Confirm new password")]
    [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
    public string ConfirmPassword { get; set; }
}

public class ManufacturersViewModel
{
    public Guid SelectedManufaturerID { get; set; }
    public IEnumerable<Manufacturer> Manufaturers { get; set; }
    public IEnumerable<ProductModel> Models { get; set; }
}

public class DeviceServiceHistoryViewModel
{
    public Device Device { get; set; }
    public DeviceInLocation CurrentInstallation { get; set; }
    public List<CaseDevice> History { get; set; }
}

public class HeaderViewModel
{
    public string Title { get; set; }
    public int RowCount { get; set; }
    public string MetaText { get; set; }
    public string CriticalText { get; set; }
    public string SearchPlaceholder { get; set; } = "Pretraži...";
    public string SearchValue { get; set; }
    public string SearchName { get; set; } = "searchString";
    public string AddButtonText { get; set; } = "+ Kreiraj novi";
    public string AddButtonId { get; set; } = "btnOpenCreate";
    public string AddButtonOnClick { get; set; }
    public bool ShowAddButton { get; set; } = true;
    public bool ShowSearch { get; set; } = true;
    public bool ShowDropdownFilter { get; set; } = true;
    public string DropdownName { get; set; }
    public string DropdownLabel { get; set; } = "Model: svi";
    public List<SelectListItem> DropdownItems { get; set; } = new();
    public string FormAction { get; set; } = "Index";
    public string FormController { get; set; }
    public Dictionary<string, string> HiddenFields { get; set; } = new();
    public List<FilterButton> FilterButtons { get; set; } = new();
}

public class FilterButton
{
    public string Text { get; set; }
    public string Badge { get; set; }
    public string Url { get; set; }
    public bool Active { get; set; }
    public string Variant { get; set; }
}

public class ActionMenuItem
{
    public string Text { get; set; }
    public string OnClick { get; set; }
    public string Url { get; set; }
    public string Variant { get; set; }
    public string? DataAction { get; set; }
    public string? DataJson { get; set; }
}

public class GroupRowViewModel
{
    public string GroupKey { get; set; }
    public string Title { get; set; }
    public string Meta { get; set; }
    public int Colspan { get; set; } = 1;
    public string AddButtonText { get; set; }
    public string AddButtonOnClick { get; set; }
    public string? AddButtonDataAction { get; set; }
    public string? AddButtonDataJson { get; set; }

    public List<ActionMenuItem> MenuItems { get; set; } = new();
}

public class ChildGroupRowViewModel
{
    public string ParentGroupKey { get; set; }
    public string GroupKey { get; set; }
    public string Title { get; set; }
    public string Meta { get; set; }
    public int Colspan { get; set; } = 1;
    public string AddButtonText { get; set; }
    public string AddButtonOnClick { get; set; }
    public string? AddButtonDataAction { get; set; }
    public string? AddButtonDataJson { get; set; }

    public List<ActionMenuItem> MenuItems { get; set; } = new();
}

public class CardGroupViewModel
{
    public string Title { get; set; }
    public string Meta { get; set; }
    public string AddButtonText { get; set; }
    public string AddButtonOnClick { get; set; }
    public string? AddButtonDataAction { get; set; }
    public string? AddButtonDataJson { get; set; }

    public List<ActionMenuItem> MenuItems { get; set; } = new();
    public List<CardSectionViewModel> Sections { get; set; } = new();
}

public class CardSectionViewModel
{
    public string Title { get; set; }
    public string Meta { get; set; }
    public string AddButtonText { get; set; }
    public string AddButtonOnClick { get; set; }
    public string? AddButtonDataAction { get; set; }
    public string? AddButtonDataJson { get; set; }

    public List<ActionMenuItem> MenuItems { get; set; } = new();
    public List<CardItemViewModel> Items { get; set; } = new();
    public string EmptyText { get; set; }
}

public class CardItemViewModel
{
    public string Title { get; set; }
    public string Subtitle { get; set; }
    public string MetaLeft { get; set; }
    public string MetaRight { get; set; }
    public string BadgeText { get; set; }
    public string BadgeClass { get; set; }
    public List<ActionMenuItem> MenuItems { get; set; } = new();
}
