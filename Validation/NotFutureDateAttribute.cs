using System.ComponentModel.DataAnnotations;

namespace SmartGymManagement.Validation
{
    public class NotFutureDateAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null)
            {
                return ValidationResult.Success;
            }

            if (value is DateTime dateValue)
            {
                DateTime dateOfBirth = dateValue.Date;
                DateTime today = GymClock.Today;

                if (dateOfBirth > today)
                {
                    return new ValidationResult("Date of birth cannot be in the future.");
                }

                if (dateOfBirth > today.AddYears(-6))
                {
                    return new ValidationResult("Member must be at least 6 years old.");
                }

                if (dateOfBirth < today.AddYears(-120))
                {
                    return new ValidationResult("Date of birth is not reasonable.");
                }
            }

            return ValidationResult.Success;
        }
    }
}