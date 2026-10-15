using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmartGymManagement.Enums;
using SmartGymManagement.Models;
using SmartGymManagement.Services;

namespace SmartGymManagement.Data
{
    public static class DbSeeder
    {
        private const string AdminEmail = "admin@smartgym.com";
        private static string AdminPassword = string.Empty;

        private static string MemberPassword = string.Empty;

        public static async Task SeedAsync(WebApplication app)
        {
            await SeedAsync(app.Services);
        }

        public static async Task SeedAsync(IServiceProvider services)
        {
            var configuration = services.GetRequiredService<IConfiguration>();
            AdminPassword = configuration["SeedData:AdminPassword"] ?? throw new InvalidOperationException("Configure SeedData:AdminPassword in User Secrets before enabling demo seeding.");
            MemberPassword = configuration["SeedData:MemberPassword"] ?? throw new InvalidOperationException("Configure SeedData:MemberPassword in User Secrets before enabling demo seeding.");
            using var scope = services.CreateScope();

            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            var userManager = scope.ServiceProvider
                .GetRequiredService<UserManager<ApplicationUser>>();

            var roleManager = scope.ServiceProvider
                .GetRequiredService<RoleManager<IdentityRole>>();

            await SeedRolesAsync(roleManager);

            await SeedAdminAsync(userManager);

            await SeedMembershipPackagesAsync(context);

            await SeedTrainersAsync(context);

            await SeedRoomsAsync(context);

            await SeedMembersAsync(
                context,
                userManager
            );

            await SeedClassSchedulesAsync(context);

            await SeedMembershipPurchasesAsync(context);

            await SeedBookingsAndAttendancesAsync(context);

            await SeedBMIRecordsAsync(context);

            await SeedChatbotMessagesAsync(context);
        }

        private static void EnsureIdentity(IdentityResult result)
        {
            if (!result.Succeeded)
                throw new InvalidOperationException("Demo account initialization failed: " + string.Join(", ", result.Errors.Select(e => e.Code)));
        }

        private static async Task SeedRolesAsync(
            RoleManager<IdentityRole> roleManager)
        {
            string[] roles =
            {
                "Admin",
                "Member",
                "Trainer"
            };

            foreach (var role in roles)
            {
                bool exists =
                    await roleManager.RoleExistsAsync(role);

                if (!exists)
                {
                    EnsureIdentity(await roleManager.CreateAsync(
                        new IdentityRole(role)
                    ));
                }
            }
        }

        private static async Task SeedAdminAsync(
            UserManager<ApplicationUser> userManager)
        {
            var admin =
                await userManager.FindByEmailAsync(AdminEmail);

            if (admin == null)
            {
                admin = new ApplicationUser
                {
                    UserName = AdminEmail,
                    Email = AdminEmail,
                    EmailConfirmed = true,
                    FullName = "System Administrator",
                    PhoneNumber = "0900000000",
                    IsActive = true,
                    CreatedAt = GymClock.Now
                };

                EnsureIdentity(await userManager.CreateAsync(
                    admin,
                    AdminPassword
                ));
            }
            bool isAdmin =
                await userManager.IsInRoleAsync(
                    admin,
                    "Admin"
                );

            if (!isAdmin)
            {
                EnsureIdentity(await userManager.AddToRoleAsync(
                    admin,
                    "Admin"
                ));
            }
        }

        private static async Task SeedMembershipPackagesAsync(
            ApplicationDbContext context)
        {
            if (await context.MembershipPackages.AnyAsync())
            {
                return;
            }

            var packages =
                new List<MembershipPackage>
                {
                    new MembershipPackage
                    {
                        PackageName = "Basic Monthly",
                        DurationDays = 30,
                        Price = 350000,
                        IsActive = true
                    },
                    new MembershipPackage
                    {
                        PackageName = "Premium 3 Months",
                        DurationDays = 90,
                        Price = 900000,
                        IsActive = true
                    },
                    new MembershipPackage
                    {
                        PackageName = "Elite Yearly",
                        DurationDays = 365,
                        Price = 3000000,
                        IsActive = true
                    }
                };

            context.MembershipPackages.AddRange(packages);

            await context.SaveChangesAsync();
        }

        private static async Task SeedTrainersAsync(
            ApplicationDbContext context)
        {
            if (await context.Trainers.AnyAsync())
            {
                return;
            }

            var trainers =
                new List<Trainer>
                {
                    new Trainer
                    {
                        FullName = "David Nguyen",
                        Email = "david.trainer@smartgym.com",
                        Phone = "0911000001",
                        Specialization = "Strength Training",
                        ExperienceYears = 6,
                        Status = TrainerStatus.Active
                    },
                    new Trainer
                    {
                        FullName = "Anna Tran",
                        Email = "anna.trainer@smartgym.com",
                        Phone = "0911000002",
                        Specialization = "Yoga and Mobility",
                        ExperienceYears = 4,
                        Status = TrainerStatus.Active
                    },
                    new Trainer
                    {
                        FullName = "Kevin Le",
                        Email = "kevin.trainer@smartgym.com",
                        Phone = "0911000003",
                        Specialization = "HIIT and Fat Loss",
                        ExperienceYears = 5,
                        Status = TrainerStatus.Active
                    }
                };

            context.Trainers.AddRange(trainers);

            await context.SaveChangesAsync();
        }

        private static async Task SeedRoomsAsync(
            ApplicationDbContext context)
        {
            if (await context.Rooms.AnyAsync())
            {
                return;
            }

            var rooms =
                new List<Room>
                {
                    new Room
                    {
                        RoomName = "Studio A",
                        Capacity = 25,
                        IsActive = true
                    },
                    new Room
                    {
                        RoomName = "Studio B",
                        Capacity = 20,
                        IsActive = true
                    },
                    new Room
                    {
                        RoomName = "Strength Zone",
                        Capacity = 30,
                        IsActive = true
                    },
                    new Room
                    {
                        RoomName = "Yoga Room",
                        Capacity = 18,
                        IsActive = true
                    }
                };

            context.Rooms.AddRange(rooms);

            await context.SaveChangesAsync();
        }

        private static async Task SeedMembersAsync(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            await CreateDemoMemberAsync(
                context,
                userManager,
                fullName: "Linh Tran",
                email: "linh.member@smartgym.com",
                phone: "0922000001",
                dateOfBirth: new DateTime(2001, 5, 12),
                gender: "Female",
                address: "District 1, Ho Chi Minh City",
                emergencyContact: "0988000001"
            );

            await CreateDemoMemberAsync(
                context,
                userManager,
                fullName: "Minh Nguyen",
                email: "minh.member@smartgym.com",
                phone: "0922000002",
                dateOfBirth: new DateTime(1999, 9, 20),
                gender: "Male",
                address: "District 3, Ho Chi Minh City",
                emergencyContact: "0988000002"
            );

            await CreateDemoMemberAsync(
                context,
                userManager,
                fullName: "Huy Pham",
                email: "huy.member@smartgym.com",
                phone: "0922000003",
                dateOfBirth: new DateTime(2002, 2, 8),
                gender: "Male",
                address: "Thu Duc City, Ho Chi Minh City",
                emergencyContact: "0988000003"
            );
        }

        private static async Task CreateDemoMemberAsync(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            string fullName,
            string email,
            string phone,
            DateTime dateOfBirth,
            string gender,
            string address,
            string emergencyContact)
        {
            var user =
                await userManager.FindByEmailAsync(email);

            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                    FullName = fullName,
                    PhoneNumber = phone,
                    IsActive = true,
                    CreatedAt = GymClock.Now
                };

                EnsureIdentity(await userManager.CreateAsync(
                    user,
                    MemberPassword
                ));
            }
            bool isMember =
                await userManager.IsInRoleAsync(
                    user,
                    "Member"
                );

            if (!isMember)
            {
                EnsureIdentity(await userManager.AddToRoleAsync(
                    user,
                    "Member"
                ));
            }

            bool memberExists =
                await context.Members
                    .AnyAsync(m => m.UserId == user.Id);

            if (!memberExists)
            {
                var member = new Member
                {
                    UserId = user.Id,
                    DateOfBirth = dateOfBirth,
                    Gender = gender,
                    Address = address,
                    EmergencyContact = emergencyContact,
                    JoinDate = GymClock.Today.AddDays(-35),
                    Status = MemberStatus.Active
                };

                context.Members.Add(member);

                await context.SaveChangesAsync();
            }
        }

        private static async Task SeedClassSchedulesAsync(
            ApplicationDbContext context)
        {
            if (await context.ClassSchedules.AnyAsync())
            {
                return;
            }

            var trainers =
                await context.Trainers
                    .Where(t => t.Status == TrainerStatus.Active)
                    .OrderBy(t => t.TrainerId)
                    .ToListAsync();

            var rooms =
                await context.Rooms
                    .Where(r => r.IsActive)
                    .OrderBy(r => r.RoomId)
                    .ToListAsync();

            if (trainers.Count < 3 || rooms.Count < 3)
            {
                return;
            }

            DateTime today = GymClock.Today;

            var classes =
                new List<ClassSchedule>
                {
                    new ClassSchedule
                    {
                        ClassName = "Demo Morning Yoga",
                        TrainerId = trainers[1].TrainerId,
                        RoomId = rooms[3].RoomId,
                        StartTime = today.AddDays(-7).AddHours(7),
                        EndTime = today.AddDays(-7).AddHours(8),
                        Capacity = 18,
                        Status = ClassStatus.Completed
                    },
                    new ClassSchedule
                    {
                        ClassName = "Demo Strength Basics",
                        TrainerId = trainers[0].TrainerId,
                        RoomId = rooms[2].RoomId,
                        StartTime = today.AddDays(-5).AddHours(18),
                        EndTime = today.AddDays(-5).AddHours(19),
                        Capacity = 25,
                        Status = ClassStatus.Completed
                    },
                    new ClassSchedule
                    {
                        ClassName = "Demo Fat Loss HIIT",
                        TrainerId = trainers[2].TrainerId,
                        RoomId = rooms[0].RoomId,
                        StartTime = today.AddDays(1).AddHours(18),
                        EndTime = today.AddDays(1).AddHours(19),
                        Capacity = 20,
                        Status = ClassStatus.Scheduled
                    },
                    new ClassSchedule
                    {
                        ClassName = "Demo Core Training",
                        TrainerId = trainers[0].TrainerId,
                        RoomId = rooms[1].RoomId,
                        StartTime = today.AddDays(2).AddHours(17),
                        EndTime = today.AddDays(2).AddHours(18),
                        Capacity = 22,
                        Status = ClassStatus.Scheduled
                    },
                    new ClassSchedule
                    {
                        ClassName = "Demo Mobility Flow",
                        TrainerId = trainers[1].TrainerId,
                        RoomId = rooms[3].RoomId,
                        StartTime = today.AddDays(3).AddHours(7),
                        EndTime = today.AddDays(3).AddHours(8),
                        Capacity = 18,
                        Status = ClassStatus.Scheduled
                    }
                };

            context.ClassSchedules.AddRange(classes);

            await context.SaveChangesAsync();
        }

        private static async Task SeedMembershipPurchasesAsync(
            ApplicationDbContext context)
        {
            var members =
                await context.Members
                    .Include(m => m.User)
                    .OrderBy(m => m.MemberId)
                    .ToListAsync();

            var packages =
                await context.MembershipPackages
                    .Where(p => p.IsActive)
                    .OrderBy(p => p.PackageId)
                    .ToListAsync();

            if (!members.Any() || packages.Count < 3)
            {
                return;
            }

            DateTime today = GymClock.Today;

            foreach (var member in members)
            {
                bool hasActiveMembership =
                    await context.Memberships
                        .AnyAsync(m =>
                            m.MemberId == member.MemberId &&
                            m.Status == MembershipStatus.Active &&
                            m.EndDate >= today
                        );

                if (hasActiveMembership)
                {
                    continue;
                }

                MembershipPackage package;

                if (member.User?.Email == "linh.member@smartgym.com")
                {
                    package = packages[1];
                }
                else if (member.User?.Email == "minh.member@smartgym.com")
                {
                    package = packages[2];
                }
                else
                {
                    package = packages[0];
                }

                DateTime startDate =
                    today.AddDays(-10);

                DateTime endDate =
                    startDate.AddDays(package.DurationDays);

                string transactionCode =
                    $"DEMO-{member.MemberId}-{GymClock.Now:yyyyMMddHHmmss}";

                var membership = new Membership
                {
                    MemberId = member.MemberId,
                    PackageId = package.PackageId,
                    StartDate = startDate,
                    EndDate = endDate,
                    PurchasedPackageName = package.PackageName,
                    PurchasedPrice = package.Price,
                    PurchasedDurationDays = package.DurationDays,
                    Status = MembershipStatus.Active,
                    CreatedAt = GymClock.Now
                };

                context.Memberships.Add(membership);

                await context.SaveChangesAsync();

                var payment = new PaymentTransaction
                {
                    MemberId = member.MemberId,
                    MembershipId = membership.MembershipId,
                    Amount = package.Price,
                    PaymentMethod = PaymentMethod.Cash,
                    Status = PaymentStatus.Paid,
                    PaymentDate = GymClock.Now.AddDays(-10),
                    TransactionCode = transactionCode,
                    Note = "Demo membership payment"
                };

                context.PaymentTransactions.Add(payment);

                var invoice = new Invoice
                {
                    InvoiceNumber = $"INV-{transactionCode}",
                    MemberId = member.MemberId,
                    MembershipId = membership.MembershipId,
                    TotalAmount = package.Price,
                    IssueDate = GymClock.Now.AddDays(-10),
                    Status = InvoiceStatus.Issued
                };

                context.Invoices.Add(invoice);

                await context.SaveChangesAsync();
            }
        }

        private static async Task SeedBookingsAndAttendancesAsync(
            ApplicationDbContext context)
        {
            var members =
                await context.Members
                    .Include(m => m.User)
                    .OrderBy(m => m.MemberId)
                    .ToListAsync();

            var classes =
                await context.ClassSchedules
                    .OrderBy(c => c.StartTime)
                    .ToListAsync();

            if (!members.Any() || !classes.Any())
            {
                return;
            }

            var pastClasses =
                classes
                    .Where(c => c.StartTime < GymClock.Now)
                    .ToList();

            var futureClasses =
                classes
                    .Where(c => c.StartTime > GymClock.Now)
                    .ToList();

            foreach (var member in members)
            {
                foreach (var pastClass in pastClasses.Take(2))
                {
                    var booking =
                        await context.Bookings
                            .FirstOrDefaultAsync(b =>
                                b.MemberId == member.MemberId &&
                                b.ClassId == pastClass.ClassId
                            );

                    if (booking == null)
                    {
                        booking = new Booking
                        {
                            MemberId = member.MemberId,
                            ClassId = pastClass.ClassId,
                            BookingDate = pastClass.StartTime.AddDays(-2),
                            Status = BookingStatus.Attended,
                            Note = "Demo attended booking"
                        };

                        context.Bookings.Add(booking);

                        await context.SaveChangesAsync();
                    }
                    else
                    {
                        booking.Status = BookingStatus.Attended;
                    }

                    bool attendanceExists =
                        await context.Attendances
                            .AnyAsync(a =>
                                a.BookingId == booking.BookingId
                            );

                    if (!attendanceExists)
                    {
                        var attendance = new Attendance
                        {
                            MemberId = member.MemberId,
                            BookingId = booking.BookingId,
                            ClassId = pastClass.ClassId,
                            CheckInTime = pastClass.StartTime.AddMinutes(5),
                            Method = AttendanceMethod.Manual,
                            Note = "Demo check-in"
                        };

                        context.Attendances.Add(attendance);
                    }
                }

                foreach (var futureClass in futureClasses.Take(2))
                {
                    bool bookingExists =
                        await context.Bookings
                            .AnyAsync(b =>
                                b.MemberId == member.MemberId &&
                                b.ClassId == futureClass.ClassId
                            );

                    if (!bookingExists)
                    {
                        var booking = new Booking
                        {
                            MemberId = member.MemberId,
                            ClassId = futureClass.ClassId,
                            BookingDate = GymClock.Now.AddDays(-1),
                            Status = BookingStatus.Booked,
                            Note = "Demo upcoming booking"
                        };

                        context.Bookings.Add(booking);
                    }
                }

                await context.SaveChangesAsync();
            }
        }

        private static async Task SeedBMIRecordsAsync(
            ApplicationDbContext context)
        {
            var members =
                await context.Members
                    .Include(m => m.User)
                    .OrderBy(m => m.MemberId)
                    .ToListAsync();

            foreach (var member in members)
            {
                bool hasBMI =
                    await context.BMIRecords
                        .AnyAsync(b =>
                            b.MemberId == member.MemberId
                        );

                if (hasBMI)
                {
                    continue;
                }

                if (member.User?.Email == "linh.member@smartgym.com")
                {
                    context.BMIRecords.Add(
                        CreateBMIRecord(
                            member,
                            heightCm: 162,
                            weightKg: 56,
                            gender: "Female",
                            activityLevel: ActivityLevel.ModeratelyActive,
                            fitnessGoal: FitnessGoal.MaintainWeight,
                            neckCm: 31,
                            waistCm: 68,
                            hipCm: 91
                        )
                    );
                }
                else if (member.User?.Email == "minh.member@smartgym.com")
                {
                    context.BMIRecords.Add(
                        CreateBMIRecord(
                            member,
                            heightCm: 175,
                            weightKg: 74,
                            gender: "Male",
                            activityLevel: ActivityLevel.VeryActive,
                            fitnessGoal: FitnessGoal.GainMuscle,
                            neckCm: 38,
                            waistCm: 82,
                            hipCm: 95
                        )
                    );
                }
                else
                {
                    context.BMIRecords.Add(
                        CreateBMIRecord(
                            member,
                            heightCm: 170,
                            weightKg: 78,
                            gender: "Male",
                            activityLevel: ActivityLevel.LightlyActive,
                            fitnessGoal: FitnessGoal.LoseWeight,
                            neckCm: 39,
                            waistCm: 91,
                            hipCm: 98
                        )
                    );
                }
            }

            await context.SaveChangesAsync();
        }

        private static BMIRecord CreateBMIRecord(
            Member member,
            decimal heightCm,
            decimal weightKg,
            string gender,
            ActivityLevel activityLevel,
            FitnessGoal fitnessGoal,
            decimal neckCm,
            decimal waistCm,
            decimal hipCm)
        {
            int age = FitnessCalculationService.CalculateAge(member.DateOfBirth) ?? 20;
            decimal bmi = FitnessCalculationService.CalculateBMI(heightCm, weightKg);
            var category = FitnessCalculationService.GetBMICategory(FitnessCalculationService.CalculateRawBMI(heightCm, weightKg));
            var bmr = FitnessCalculationService.CalculateBMR(heightCm, weightKg, age, gender);
            var tdee = FitnessCalculationService.CalculateTDEE(bmr, activityLevel);
            var targetCalories = FitnessCalculationService.CalculateTargetCalories(tdee, fitnessGoal);
            var macros = FitnessCalculationService.CalculateMacros(targetCalories, fitnessGoal);
            var proteinGrams = macros.Protein;
            var carbGrams = macros.Carbs;
            var fatGrams = macros.Fat;
            var bodyFat = FitnessCalculationService.CalculateBodyFatPercentage(heightCm, gender, neckCm, waistCm, hipCm);
            var leanBodyMass = FitnessCalculationService.CalculateLeanBodyMass(weightKg, bodyFat);
            var idealBodyWeight = FitnessCalculationService.CalculateIdealBodyWeight(heightCm, gender);
            var waterLiters = FitnessCalculationService.CalculateEstimatedWater(weightKg, activityLevel);
            return new BMIRecord
            {
                MemberId = member.MemberId,
                HeightCm = heightCm,
                WeightKg = weightKg,
                AgeAtRecord = age,
                GenderAtRecord = gender,
                ActivityLevel = activityLevel,
                FitnessGoal = fitnessGoal,

                NeckCm = neckCm,
                WaistCm = waistCm,
                HipCm = hipCm,

                BMIValue = bmi,
                Category = category,

                BMR = bmr,
                TDEE = tdee,
                TargetCalories = targetCalories,

                ProteinGrams = proteinGrams,
                CarbohydrateGrams = carbGrams,
                FatGrams = fatGrams,

                IdealBodyWeightKg = idealBodyWeight,
                BodyFatPercentage = bodyFat,
                LeanBodyMassKg = leanBodyMass,

                WaistToHipRatio =
                    Math.Round(waistCm / hipCm, 2),

                WaistToHeightRatio =
                    Math.Round(waistCm / heightCm, 2),

                EstimatedWaterLiters = waterLiters,

                RecordedAt = GymClock.Now.AddDays(-3),

                Note = "Demo fitness assessment"
            };
        }

        private static BMICategory GetBMICategory(decimal bmi)
        {
            if (bmi < 18.5m)
            {
                return BMICategory.Underweight;
            }

            if (bmi < 25m)
            {
                return BMICategory.Normal;
            }

            if (bmi < 30m)
            {
                return BMICategory.Overweight;
            }

            return BMICategory.Obese;
        }

        private static decimal GetActivityWaterBonus(
            ActivityLevel activityLevel)
        {
            return activityLevel switch
            {
                ActivityLevel.Sedentary => 0m,
                ActivityLevel.LightlyActive => 0.25m,
                ActivityLevel.ModeratelyActive => 0.5m,
                ActivityLevel.VeryActive => 0.75m,
                ActivityLevel.ExtraActive => 1m,
                _ => 0m
            };
        }

        private static int CalculateAge(DateTime dateOfBirth)
        {
            DateTime today = GymClock.Today;

            int age =
                today.Year - dateOfBirth.Year;

            if (dateOfBirth.Date > today.AddYears(-age))
            {
                age--;
            }

            return age;
        }

        private static async Task SeedChatbotMessagesAsync(
            ApplicationDbContext context)
        {
            var members =
                await context.Members
                    .Include(m => m.User)
                    .ToListAsync();

            foreach (var member in members)
            {
                bool hasMessages =
                    await context.ChatbotMessages
                        .AnyAsync(c =>
                            c.MemberId == member.MemberId
                        );

                if (hasMessages)
                {
                    continue;
                }

                var messages =
                    new List<ChatbotMessage>
                    {
                        new ChatbotMessage
                        {
                            MemberId = member.MemberId,
                            UserMessage = "How many days are left on my membership?",
                            BotResponse = "Your membership is active. You can ask me anytime to check your remaining membership days.",
                            CreatedAt = GymClock.Now.AddDays(-1)
                        },
                        new ChatbotMessage
                        {
                            MemberId = member.MemberId,
                            UserMessage = "How much water should I drink?",
                            BotResponse = "Your water target depends on your latest fitness assessment, body weight and activity level.",
                            CreatedAt = GymClock.Now.AddHours(-8)
                        }
                    };

                context.ChatbotMessages.AddRange(messages);
            }

            await context.SaveChangesAsync();
        }
    }
}