using SmartGymManagement.Enums;
namespace SmartGymManagement.Services;
public static class FitnessCalculationService
{
        public static int? CalculateAge(DateTime? dateOfBirth)
        {
            if (dateOfBirth == null)
            {
                return null;
            }

            DateTime today = GymClock.Today;

            int age =
                today.Year - dateOfBirth.Value.Year;

            if (dateOfBirth.Value.Date >
                today.AddYears(-age))
            {
                age--;
            }

            return age;
        }

        public static decimal CalculateRawBMI(decimal heightCm, decimal weightKg)
        {
            if (heightCm <= 0 || weightKg <= 0) throw new ArgumentOutOfRangeException(nameof(heightCm));
            var heightM = heightCm / 100m;
            return weightKg / (heightM * heightM);
        }

        public static decimal CalculateBMI(
            decimal heightCm,
            decimal weightKg)
        {
            return Math.Round(CalculateRawBMI(heightCm, weightKg), 2);
        }

        public static BMICategory GetBMICategory(decimal bmi)
        {
            if (bmi < 18.5m)
                return BMICategory.Underweight;

            if (bmi < 25m)
                return BMICategory.Normal;

            if (bmi < 30m)
                return BMICategory.Overweight;

            return BMICategory.Obese;
        }

        public static decimal CalculateBMR(
            decimal heightCm,
            decimal weightKg,
            int age,
            string gender)
        {
            decimal result =
                10m * weightKg +
                6.25m * heightCm -
                5m * age;

            result += gender == "Male"
                ? 5m
                : -161m;

            return Math.Round(result, 2);
        }

        public static decimal CalculateTDEE(
            decimal bmr,
            ActivityLevel level)
        {
            decimal multiplier = level switch
            {
                ActivityLevel.Sedentary => 1.200m,
                ActivityLevel.LightlyActive => 1.375m,
                ActivityLevel.ModeratelyActive => 1.550m,
                ActivityLevel.VeryActive => 1.725m,
                ActivityLevel.ExtraActive => 1.900m,
                _ => 1.200m
            };

            return Math.Round(
                bmr * multiplier,
                2
            );
        }

        public static decimal CalculateTargetCalories(
            decimal tdee,
            FitnessGoal goal)
        {
            decimal calories = goal switch
            {
                FitnessGoal.LoseWeight => tdee - 500m,
                FitnessGoal.MaintainWeight => tdee,
                FitnessGoal.GainMuscle => tdee + 300m,
                _ => tdee
            };

            return Math.Round(
                Math.Max(calories, 1200m),
                2
            );
        }

        public static (
            decimal Protein,
            decimal Carbs,
            decimal Fat
        ) CalculateMacros(
            decimal calories,
            FitnessGoal goal)
        {
            decimal proteinPercent;
            decimal carbPercent;
            decimal fatPercent;

            switch (goal)
            {
                case FitnessGoal.LoseWeight:
                    proteinPercent = 0.30m;
                    carbPercent = 0.40m;
                    fatPercent = 0.30m;
                    break;

                case FitnessGoal.GainMuscle:
                    proteinPercent = 0.30m;
                    carbPercent = 0.50m;
                    fatPercent = 0.20m;
                    break;

                default:
                    proteinPercent = 0.25m;
                    carbPercent = 0.45m;
                    fatPercent = 0.30m;
                    break;
            }

            decimal protein =
                calories * proteinPercent / 4m;

            decimal carbs =
                calories * carbPercent / 4m;

            decimal fat =
                calories * fatPercent / 9m;

            return (
                Math.Round(protein, 1),
                Math.Round(carbs, 1),
                Math.Round(fat, 1)
            );
        }

        public static decimal? CalculateIdealBodyWeight(
            decimal heightCm,
            string gender)
        {
            if (heightCm < 152.4m) return null;
            double heightInches =
                (double)heightCm / 2.54;

            double inchesOverFiveFeet =
                Math.Max(0, heightInches - 60);

            double result = gender == "Male"
                ? 50 + 2.3 * inchesOverFiveFeet
                : 45.5 + 2.3 * inchesOverFiveFeet;

            return Math.Round((decimal)result, 2);
        }

        public static decimal? CalculateBodyFatPercentage(
            decimal heightCm,
            string gender,
            decimal? neckCm,
            decimal? waistCm,
            decimal? hipCm)
        {
            if (neckCm == null || waistCm == null)
            {
                return null;
            }

            double heightInches =
                (double)heightCm / 2.54;

            double neckInches =
                (double)neckCm.Value / 2.54;

            double waistInches =
                (double)waistCm.Value / 2.54;

            double result;

            if (gender == "Male")
            {
                double difference =
                    waistInches - neckInches;

                if (difference <= 0)
                    return null;

                result =
                    86.010 * Math.Log10(difference)
                    - 70.041 * Math.Log10(heightInches)
                    + 36.76;
            }
            else
            {
                if (hipCm == null)
                    return null;

                double hipInches =
                    (double)hipCm.Value / 2.54;

                double circumference =
                    waistInches +
                    hipInches -
                    neckInches;

                if (circumference <= 0)
                    return null;

                result =
                    163.205 * Math.Log10(circumference)
                    - 97.684 * Math.Log10(heightInches)
                    - 78.387;
            }

            if (!double.IsFinite(result) || result <= 0 || result >= 100) return null;
            return Math.Round((decimal)result, 2);
        }

        public static decimal? CalculateLeanBodyMass(
            decimal weightKg,
            decimal? bodyFatPercentage)
        {
            if (bodyFatPercentage == null || bodyFatPercentage <= 0 || bodyFatPercentage >= 100)
            {
                return null;
            }

            decimal leanMass =
                weightKg *
                (1m - bodyFatPercentage.Value / 100m);

            return Math.Round(leanMass, 2);
        }

        public static decimal? CalculateWaistToHipRatio(
            decimal? waistCm,
            decimal? hipCm)
        {
            if (waistCm == null ||
                hipCm == null ||
                hipCm <= 0)
            {
                return null;
            }

            return Math.Round(
                waistCm.Value / hipCm.Value,
                2
            );
        }

        public static decimal? CalculateWaistToHeightRatio(
            decimal? waistCm,
            decimal heightCm)
        {
            if (waistCm == null ||
                heightCm <= 0)
            {
                return null;
            }

            return Math.Round(
                waistCm.Value / heightCm,
                2
            );
        }

        public static decimal CalculateEstimatedWater(
            decimal weightKg,
            ActivityLevel activityLevel)
        {
            decimal waterMl =
                weightKg * 35m;

            waterMl += activityLevel switch
            {
                ActivityLevel.LightlyActive => 250m,
                ActivityLevel.ModeratelyActive => 500m,
                ActivityLevel.VeryActive => 750m,
                ActivityLevel.ExtraActive => 1000m,
                _ => 0m
            };

            return Math.Round(
                waterMl / 1000m,
                2
            );
        }

}
