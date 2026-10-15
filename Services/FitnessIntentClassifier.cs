using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace SmartGymManagement.Services;

public enum FitnessIntent { Help, Membership, Bookings, Attendance, Water, Nutrition, Metrics, Knowledge, Other }

public static class FitnessIntentClassifier
{
    public static string Normalize(string text)
    {
        var builder = new StringBuilder();
        foreach (var character in text.ToLowerInvariant().Normalize(NormalizationForm.FormD))
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
                builder.Append(character == 'đ' ? 'd' : character);
        return Regex.Replace(builder.ToString(), @"[^a-z0-9]+", " ").Trim();
    }

    public static bool Matches(string question, params string[] keywords)
    {
        var normalized = " " + Normalize(question) + " ";
        return keywords.Any(keyword => normalized.Contains(" " + Normalize(keyword) + " ", StringComparison.Ordinal));
    }

    public static FitnessIntent Classify(string question)
    {
        if (Matches(question, "help", "hướng dẫn", "giúp tôi", "bạn làm được gì")) return FitnessIntent.Help;
        if (Matches(question, "membership", "package", "gói tập", "hết hạn", "remaining days", "bao nhiêu ngày")) return FitnessIntent.Membership;
        if (Matches(question, "booking", "booked", "upcoming classes", "upcoming class", "next class", "lịch tập", "lớp sắp tới", "đã đặt")) return FitnessIntent.Bookings;
        if (Matches(question, "attendance", "check in", "attended", "điểm danh", "đã tập bao nhiêu", "tham gia")) return FitnessIntent.Attendance;
        if (Matches(question, "water", "hydration", "nước", "uống bao nhiêu")) return FitnessIntent.Water;
        if (Matches(question, "tdee", "bmr", "calories", "calorie", "calo", "macros", "macro", "protein target", "mục tiêu protein", "dinh dưỡng")) return FitnessIntent.Nutrition;
        // Specific training intents precede the general "weight" metric.
        if (Matches(question, "lose weight", "weight loss", "lose fat", "fat loss", "giảm cân", "giảm mỡ",
            "build muscle", "gain muscle", "muscle gain", "tăng cơ", "protein", "đạm", "cardio", "run", "running", "chạy bộ",
            "sleep", "recovery", "ngủ", "hồi phục", "warm up", "khởi động", "stretch", "giãn cơ", "carbs", "carbohydrate", "tinh bột", "dietary fat", "chất béo", "training frequency", "tập mấy buổi"))
            return FitnessIntent.Knowledge;
        if (Matches(question, "bmi", "body fat", "weight", "lean mass", "cân nặng", "mỡ cơ thể", "chỉ số cơ thể")) return FitnessIntent.Metrics;
        return FitnessIntent.Other;
    }
}
