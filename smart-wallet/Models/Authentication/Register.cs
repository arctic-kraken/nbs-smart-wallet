using System.ComponentModel.DataAnnotations;

namespace nbs_smart_wallet.Models.Authentication
{
	public class Register
	{
		[Required(ErrorMessage = "User Name is required")]
		public string Username { get; set; }

		[EmailAddress]
		[Required(ErrorMessage = "Email is required")]
		public string Email { get; set; }

		[Required(ErrorMessage = "Password is required")]
		[DataType(DataType.Password)]
		public string Password { get; set; }

		[Required(ErrorMessage = "Password confirmation is required")]
		[Compare("Password", ErrorMessage = "Passwords do not match")]
		public string ConfirmPassword { get; set; }

		public List<string> errorMessages { get; set; } = new List<string>();
	}
}
