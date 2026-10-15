using System.Net;
using Microsoft.AspNetCore.DataProtection;
using QRCoder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using SmartGymManagement.Data;
using SmartGymManagement.Enums;
using SmartGymManagement.Models;
using SmartGymManagement.Services;
using static SmartGymManagement.Services.FitnessCalculationService;

var passed = 0;
void Check(bool condition, string description)
{
    if (!condition) throw new Exception("FAILED: " + description);
    passed++;
    Console.WriteLine("PASS " + description);
}

Check(CalculateBMI(170, 70) == 24.22m, "BMI uses metres");
Check(GetBMICategory(18.499m) == BMICategory.Underweight, "BMI below threshold stays underweight before rounding");
Check(GetBMICategory(18.5m) == BMICategory.Normal, "BMI lower normal boundary");
Check(GetBMICategory(25m) == BMICategory.Overweight, "BMI overweight boundary");
Check(GetBMICategory(30m) == BMICategory.Obese, "BMI obese boundary");
var throws = false;
try { CalculateBMI(0, 70); } catch (ArgumentOutOfRangeException) { throws = true; }
Check(throws, "Zero height cannot divide by zero");
Check(CalculateBMR(170, 70, 30, "Male") == 1617.5m, "Male BMR reference case");
Check(CalculateBMR(170, 70, 30, "Female") == 1451.5m, "Female BMR reference case");
Check(CalculateTDEE(1600, ActivityLevel.ModeratelyActive) == 2480, "TDEE activity factor");
Check(CalculateTargetCalories(1000, FitnessGoal.LoseWeight) == 1200, "Calories lower limit");
Check(CalculateTargetCalories(2000, FitnessGoal.GainMuscle) == 2300, "Muscle gain calories");
var macros = CalculateMacros(2000, FitnessGoal.MaintainWeight);
Check(macros.Protein == 125 && macros.Carbs == 225 && Math.Abs(macros.Fat * 9 + macros.Protein * 4 + macros.Carbs * 4 - 2000) < 1,
    "Macros preserve calorie budget within rounding");
Check(CalculateIdealBodyWeight(140, "Female") == null, "Short-height ideal-weight estimate omitted");
Check(CalculateBodyFatPercentage(170, "Male", 40, 35, null) == null, "Invalid logarithm input rejected");
Check(CalculateBodyFatPercentage(100, "Female", 20, 250, 250) == null, "Body fat above 100 percent rejected");
Check(CalculateLeanBodyMass(70, 120) == null, "Lean mass cannot become negative");
Check(CalculateLeanBodyMass(70, 20) == 56, "Lean mass reference case");
Check(CalculateWaistToHeightRatio(85, 170) == 0.5m, "Waist-to-height can use waist alone");
Check(CalculateBodyFatPercentage(170, "Female", 35, 75, null) == null, "Incomplete body-fat assessment remains optional");
Check(CalculateAge(GymClock.Today.AddYears(-30)) == 30, "Age on birthday");
Check(CalculateAge(GymClock.Today.AddYears(-30).AddDays(1)) == 29, "Age before birthday");

var january = new DateTime(2026, 1, 1);
Check(MembershipRules.EndDate(january, 1) == january, "One-day membership has one inclusive day");
Check(MembershipRules.EndDate(january, 30) == new DateTime(2026, 1, 30), "Thirty-day membership is not thirty-one days");
Check(MembershipRules.Overlaps(january, january.AddDays(29), january.AddDays(29), january.AddDays(58)), "Memberships sharing an inclusive boundary overlap");
Check(!MembershipRules.Overlaps(january, january.AddDays(29), january.AddDays(30), january.AddDays(59)), "Next-day renewal does not overlap");
var now = new DateTime(2026, 1, 10, 12, 0, 0);
var classStart = now.AddHours(-1);
var classEnd = now;
Check(AttendanceWindowRules.Validate(ClassStatus.Completed, classStart, classEnd, now.AddHours(1)) == null,
    "Completed class still accepts late attendance within two hours");
Check(AttendanceWindowRules.Validate(ClassStatus.Completed, classStart, classEnd, now.AddHours(2)) == null,
    "Attendance includes the exact closing boundary");
Check(AttendanceWindowRules.Validate(ClassStatus.Completed, classStart, classEnd, now.AddHours(2).AddTicks(1)) != null,
    "Completed class rejects attendance beyond the deadline");
Check(AttendanceWindowRules.Validate(ClassStatus.Scheduled, classStart, classEnd, classStart.AddMinutes(-30)) == null,
    "Attendance includes the exact opening boundary");
Check(AttendanceWindowRules.Validate(ClassStatus.Scheduled, classStart, classEnd, classStart.AddMinutes(-30).AddTicks(-1)) != null,
    "Early attendance is rejected");
Check(AttendanceWindowRules.Validate(ClassStatus.Cancelled, classStart, classEnd, now) != null,
    "Cancelled class never accepts attendance");
Check(!AttendanceWindowRules.AllowsAttendance((ClassStatus)99), "Unknown class status cannot issue an attendance QR");

var reservedClass = new ClassSchedule { ClassId = 10, StartTime = now, EndTime = now.AddHours(1), Status = ClassStatus.Scheduled };
var attendedBooking = new Booking { BookingId = 1, MemberId = 1, ClassId = 10, ClassSchedule = reservedClass, Status = BookingStatus.Attended };
var reservationFixtures = new[] { attendedBooking }.AsQueryable();
Check(reservationFixtures.OverlappingReservations(1, 20, now.AddMinutes(15), now.AddHours(2)).Any(),
    "Rescheduling detects another class already attended after early check-in");
Check(!reservationFixtures.OverlappingReservations(1, 20, now.AddHours(1), now.AddHours(2)).Any(),
    "Adjacent classes do not overlap");
Check(!reservationFixtures.OverlappingReservations(2, 20, now, now.AddHours(1)).Any(),
    "Another member's booking does not prevent rescheduling");
Check(!reservationFixtures.OverlappingReservations(1, 10, now, now.AddHours(1)).Any(),
    "Rescheduling excludes the edited class itself");
foreach (var status in new[] { BookingStatus.Cancelled, BookingStatus.NoShow })
{
    attendedBooking.Status = status;
    Check(!reservationFixtures.OverlappingReservations(1, 20, now, now.AddHours(1)).Any(),
        $"{status} booking does not reserve overlapping time");
}
attendedBooking.Status = BookingStatus.Booked;
Check(reservationFixtures.OverlappingReservations(1, 20, now, now.AddHours(1)).Any(), "Booked class reserves overlapping time");
reservedClass.Status = ClassStatus.Cancelled;
Check(!reservationFixtures.OverlappingReservations(1, 20, now, now.AddHours(1)).Any(), "Cancelled class does not reserve time");

var exportPeriod = new ReportPeriod(new DateTime(2026, 2, 1), new DateTime(2026, 2, 28));
var paymentFixtures = new[]
{
    new PaymentTransaction { PaymentTransactionId = 1, PaymentDate = new DateTime(2026, 1, 15), RefundedAt = new DateTime(2026, 2, 10), Status = PaymentStatus.Refunded },
    new PaymentTransaction { PaymentTransactionId = 2, PaymentDate = exportPeriod.From, RefundedAt = new DateTime(2026, 2, 20), Status = PaymentStatus.Refunded },
    new PaymentTransaction { PaymentTransactionId = 3, PaymentDate = exportPeriod.Until, Status = PaymentStatus.Paid },
    new PaymentTransaction { PaymentTransactionId = 4, PaymentDate = new DateTime(2026, 1, 10), RefundedAt = exportPeriod.Until, Status = PaymentStatus.Refunded },
    new PaymentTransaction { PaymentTransactionId = 5, PaymentDate = exportPeriod.To.AddHours(23), Status = PaymentStatus.Refunded },
    new PaymentTransaction { PaymentTransactionId = 6, PaymentDate = new DateTime(2026, 1, 10), RefundedAt = exportPeriod.From, Status = PaymentStatus.Paid }
}.AsQueryable();
Check(paymentFixtures.InReportPeriod(exportPeriod).Select(p => p.PaymentTransactionId).SequenceEqual(new[] { 1, 2, 5 }),
    "Export includes cross-period refunds and legacy refunds, excludes upper boundary and non-refunded records");
Check(paymentFixtures.InReportPeriod(exportPeriod).Count(p => p.PaymentTransactionId == 2) == 1,
    "Receipt and refund in the same period export once");
Check(BookingTransitionRules.Validate(BookingStatus.Booked, BookingStatus.Attended, false, now, now) != null, "Attendance must use check-in");
Check(BookingTransitionRules.Validate(BookingStatus.Attended, BookingStatus.Cancelled, true, now, now) != null, "Attended booking cannot be cancelled directly");
Check(BookingTransitionRules.Validate(BookingStatus.Booked, BookingStatus.NoShow, false, now.AddHours(-2), now) != null, "No-show waits until check-in deadline passes");
Check(BookingTransitionRules.Validate(BookingStatus.Booked, BookingStatus.NoShow, false, now.AddHours(-3), now) == null, "No-show after deadline is allowed");
Check(BookingTransitionRules.Validate(BookingStatus.Booked, (BookingStatus)99, false, now, now) != null, "Undefined status rejected");
Check(BookingTransitionRules.Validate(BookingStatus.Cancelled, BookingStatus.NoShow, false, now, now) != null, "Cancelled booking cannot become no-show");
Check(BookingTransitionRules.Validate(BookingStatus.Booked, BookingStatus.Cancelled, true, now, now) != null, "Existing attendance protects inconsistent legacy booking");

Check(FitnessIntentClassifier.Classify("How can I lose weight?") == FitnessIntent.Knowledge, "Weight-loss question does not route to BMI");
Check(FitnessIntentClassifier.Classify("Tôi muốn giảm cân") == FitnessIntent.Knowledge, "Vietnamese weight-loss intent");
Check(FitnessIntentClassifier.Classify("TOI MUON GIAM CAN") == FitnessIntent.Knowledge, "Unaccented uppercase Vietnamese intent");
Check(FitnessIntentClassifier.Classify("Gói tập còn bao nhiêu ngày?") == FitnessIntent.Membership, "Membership intent");
Check(FitnessIntentClassifier.Classify("What is my latest BMI?") == FitnessIntent.Metrics, "Personal BMI intent");
Check(FitnessIntentClassifier.Classify("What are my upcoming classes?") == FitnessIntent.Bookings, "Quick question matches plural classes");
Check(!FitnessIntentClassifier.Matches("breakfast brunch", "fat", "run"), "Keywords cannot match arbitrary word substrings");
Check(FitnessIntentClassifier.Classify("Mục tiêu protein của tôi") == FitnessIntent.Nutrition, "Personal protein target uses recorded nutrition data");
var period = ReportPeriod.Create(now, now.AddDays(-5));
Check(period.From == period.To && period.Until == period.To.AddDays(1), "Report reversed date range is normalized");

// Offline model checks own their dummy connection; the real EF factory must
// resolve the application's configured database rather than a placeholder.
using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
    .UseSqlServer("Server=offline-model-only;Database=OfflineModelChecks;Integrated Security=True").Options);
using var factoryDb = new DesignTimeDbContextFactory().CreateDbContext([
    "--environment", "Production",
    "--ConnectionStrings:DefaultConnection", "Server=offline-factory-only;Database=OfflineFactoryChecks;Integrated Security=True"]);
Check(factoryDb.Database.GetDbConnection().DataSource == "offline-factory-only" &&
    factoryDb.Database.GetDbConnection().Database == "OfflineFactoryChecks",
    "Design-time factory respects forwarded connection configuration");
Check(factoryDb.Database.GetDbConnection().State == System.Data.ConnectionState.Closed,
    "Design-time configuration does not open a connection or run application startup");
var previousConnectionOverride = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
try
{
    Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection",
        "Server=offline-environment-only;Database=OfflineEnvironmentChecks;Integrated Security=True");
    using var environmentDb = new DesignTimeDbContextFactory().CreateDbContext(["--environment", "Production"]);
    Check(environmentDb.Database.GetDbConnection().DataSource == "offline-environment-only" &&
        environmentDb.Database.GetDbConnection().Database == "OfflineEnvironmentChecks",
        "Design-time factory loads the connection environment variable over JSON");
    using var commandLineDb = new DesignTimeDbContextFactory().CreateDbContext([
        "--environment", "Production",
        "--ConnectionStrings:DefaultConnection", "Server=offline-command-only;Database=OfflineCommandChecks;Integrated Security=True"]);
    Check(commandLineDb.Database.GetDbConnection().DataSource == "offline-command-only",
        "Forwarded command-line connection configuration overrides environment variables");
}
finally { Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", previousConnectionOverride); }
var missingConnectionRejected = false;
try { DatabaseConfiguration.Configure(new DbContextOptionsBuilder<ApplicationDbContext>(), new ConfigurationBuilder().Build()); }
catch (InvalidOperationException ex) { missingConnectionRejected = ex.Message == "ConnectionStrings:DefaultConnection must be configured."; }
Check(missingConnectionRejected, "Missing runtime or design-time connection fails without revealing secrets");
var overlapSql = db.Bookings.OverlappingReservations(1, 20, now, now.AddHours(1)).ToQueryString();
Check(overlapSql.Contains("[Status] IN (0, 2)"), "Rescheduling reservation states translate to SQL");
var paymentExportSql = db.PaymentTransactions.InReportPeriod(exportPeriod).ToQueryString();
Check(paymentExportSql.Contains("[RefundedAt]") && paymentExportSql.Contains("COALESCE") && paymentExportSql.Contains(" OR "),
    "Export receipt-or-refund predicate translates to SQL with legacy date fallback");
var chatbot = db.Model.FindEntityType(typeof(ChatbotMessage))!;
Check(chatbot.GetForeignKeys().Count() == 1 && chatbot.FindProperty("MemberId1") == null, "Chat messages have one member relationship");
Check(chatbot.GetForeignKeys().Single().PrincipalToDependent?.Name == "ChatbotMessages", "Member chat collection uses the real foreign key");
Check(db.Model.FindEntityType(typeof(Attendance))!.GetIndexes().Any(i => i.IsUnique && i.Properties.Single().Name == "BookingId"), "Attendance uniqueness is enforced in EF model");
Check(db.Model.FindEntityType(typeof(Booking))!.GetIndexes().Any(i => i.IsUnique && i.GetFilter() == "[Status] IN (0, 2)"), "Only reserved booking states require uniqueness");
Check(db.Model.FindEntityType(typeof(Membership))!.FindProperty("PurchasedPrice")!.GetPrecision() == 18, "Historical purchase amount has explicit precision");
var sql = db.Bookings.Where(b => b.Status != BookingStatus.Cancelled && b.ClassSchedule != null &&
    !db.Attendances.Any(a => a.BookingId == b.BookingId)).ToQueryString();
Check(sql.Contains("NOT EXISTS"), "Attendance-based report predicate translates to SQL");
Check(db.Database.GetDbConnection().State == System.Data.ConnectionState.Closed, "Model checks never open a database connection");

var handler = new FakeHandler();
FitnessAssistantService Assistant(Dictionary<string, string?> settings) => new(new HttpClient(handler),
    new ConfigurationBuilder().AddInMemoryCollection(settings).Build(), NullLogger<FitnessAssistantService>.Instance);
var disabled = Assistant(new());
Check(await disabled.ReplyAsync("hello", new { }, [], default) == null && handler.Calls == 0, "Disabled AI never contacts a service");
var settings = new Dictionary<string, string?> { ["FitnessAI:Enabled"] = "true", ["FitnessAI:Endpoint"] = "http://example.com/chat", ["FitnessAI:Model"] = "test-model" };
Check(await Assistant(settings).ReplyAsync("hello", new { }, [], default) == null && handler.Calls == 0, "Remote AI requires HTTPS");
settings["FitnessAI:Endpoint"] = "https://example.com/chat";
handler.Body = "{\"choices\":[{\"message\":{\"content\":\"Câu trả lời thử nghiệm\"}}]}";
Check(await Assistant(settings).ReplyAsync("hello", new { }, [], default) == "Câu trả lời thử nghiệm", "Compatible AI response is parsed");
handler.Status = HttpStatusCode.TooManyRequests;
Check(await Assistant(settings).ReplyAsync("hello", new { }, [], default) == null, "Provider throttling uses local fallback");
handler.Status = HttpStatusCode.OK; handler.Body = "{}";
Check(await Assistant(settings).ReplyAsync("hello", new { }, [], default) == null, "Malformed provider response uses fallback");

var tokens = new BookingQrTokenService(new EphemeralDataProtectionProvider());
var validToken = tokens.Issue(12, 34);
Check(tokens.TryRead(validToken, out var bookingId, out var memberId) && bookingId == 12 && memberId == 34,
    "Signed QR identifies its booking and owner");
Check(!tokens.TryRead(tokens.Issue(12, 34, DateTimeOffset.UtcNow.AddMinutes(-1)), out _, out _), "Expired QR rejected");
var changedToken = validToken[..20] + (validToken[20] == 'A' ? 'B' : 'A') + validToken[21..];
Check(!tokens.TryRead(changedToken, out _, out _), "Tampered QR rejected");
Check(!tokens.TryRead("12:34", out _, out _), "Unsigned booking IDs cannot check in");
Check(!tokens.TryRead(tokens.Issue(-1, 34), out _, out _), "Invalid QR identifiers rejected");
var png = PngByteQRCodeHelper.GetQRCode(validToken, QRCodeGenerator.ECCLevel.Q, 8);
Check(png.Take(8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }), "QR renders locally as PNG without network requests");
Console.WriteLine($"{passed} checks passed. No SQL connection or application startup was used.");

sealed class FakeHandler : HttpMessageHandler
{
    public int Calls { get; private set; }
    public HttpStatusCode Status { get; set; } = HttpStatusCode.OK;
    public string Body { get; set; } = "{}";
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Calls++;
        return Task.FromResult(new HttpResponseMessage(Status) { Content = new StringContent(Body) });
    }
}
