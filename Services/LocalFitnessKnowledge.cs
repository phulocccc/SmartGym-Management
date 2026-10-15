namespace SmartGymManagement.Services;

public static class LocalFitnessKnowledge
{
    public static string Reply(string question)
    {
        if (FitnessIntentClassifier.Matches(question, "lose weight", "weight loss", "lose fat", "fat loss", "giảm cân", "giảm mỡ"))
            return "Để giảm mỡ, hãy kết hợp chế độ ăn phù hợp, tập sức mạnh, vận động đều và nghỉ ngơi. Theo dõi xu hướng cân nặng thay vì một lần cân. Tránh nhịn ăn cực đoan; có thể xem calories ước tính ở báo cáo fitness để tham khảo.";
        if (FitnessIntentClassifier.Matches(question, "build muscle", "gain muscle", "muscle gain", "tăng cơ"))
            return "Để tăng cơ, hãy tập sức mạnh với mức tải tăng dần phù hợp, ăn đủ chất và ngủ đều. Theo dõi số lần lặp và mức tạ, dành thời gian hồi phục giữa các buổi tập nặng.";
        if (FitnessIntentClassifier.Matches(question, "protein", "đạm"))
            return "Protein hỗ trợ hồi phục và duy trì cơ. Phân bổ thực phẩm giàu đạm trong các bữa ăn; mục tiêu protein của bạn được hiển thị trong báo cáo fitness. Nếu có bệnh lý hoặc yêu cầu ăn kiêng đặc biệt, hãy trao đổi với chuyên gia dinh dưỡng.";
        if (FitnessIntentClassifier.Matches(question, "warm up", "khởi động"))
            return "Khởi động bằng vận động nhẹ và các động tác linh hoạt phù hợp bài tập, sau đó tăng tải dần. Không bắt đầu ngay bằng mức tạ nặng.";
        if (FitnessIntentClassifier.Matches(question, "sleep", "recovery", "ngủ", "hồi phục"))
            return "Giữ lịch ngủ đều, ăn đủ chất và bố trí ngày nghỉ. Nếu mệt kéo dài, giảm cường độ và trao đổi với huấn luyện viên; không cố tập qua đau hoặc chấn thương.";
        if (FitnessIntentClassifier.Matches(question, "stretch", "giãn cơ"))
            return "Giãn cơ nhẹ và thả lỏng sau tập; không ép động tác gây đau. Chọn bài phù hợp độ linh hoạt và hướng dẫn của huấn luyện viên.";
        if (FitnessIntentClassifier.Matches(question, "cardio", "run", "running", "chạy bộ"))
            return "Bắt đầu cardio ở mức phù hợp thể lực, tăng thời lượng dần và kết hợp tập sức mạnh. Nếu đau ngực, chóng mặt hoặc khó thở bất thường, dừng tập và tìm hỗ trợ y tế.";
        if (FitnessIntentClassifier.Matches(question, "carbs", "carbohydrate", "tinh bột", "dietary fat", "chất béo"))
            return "Tinh bột cung cấp năng lượng cho vận động; chất béo cũng cần thiết trong chế độ ăn cân bằng. Tham khảo lượng macros ước tính trong báo cáo fitness, không loại bỏ hoàn toàn một nhóm chất.";
        return "Chọn lịch tập phù hợp thể lực và khả năng hồi phục. Người mới nên bắt đầu vừa sức, giữ lịch đều và nhờ huấn luyện viên kiểm tra kỹ thuật trước khi tăng cường độ.";
    }
}
