using Microsoft.EntityFrameworkCore;
using ShiftManagement.Core.Models;

namespace ShiftManagement.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(ShiftDbContext context)
    {
        await context.Database.EnsureCreatedAsync();

        // 施設設定
        if (!await context.FacilitySettings.AnyAsync())
        {
            context.FacilitySettings.Add(new FacilitySettings
            {
                FacilityName = "住宅型有料老人ホーム ライフステイむなかた",
                MinDaysOff = 9,
                MaxConsecutiveWorkDays = 5,
                CountNightOffAsRest = false,
                NightDutyLoadFactor = 3.0,
                EarlyDutyLoadFactor = 2.0,
                LateDutyLoadFactor = 2.0,
                DayDutyLoadFactor = 1.0,
                UpdatedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();
        }

        // 職種
        if (!await context.JobTypes.AnyAsync())
        {
            var jobTypes = new List<JobType>
            {
                new() { Code = "NURSING", DisplayName = "看護職", DisplayOrder = 1 },
                new() { Code = "CARE", DisplayName = "介護職", DisplayOrder = 2 }
            };
            context.JobTypes.AddRange(jobTypes);
            await context.SaveChangesAsync();
        }

        // 勤務形態
        if (!await context.EmploymentTypes.AnyAsync())
        {
            var employmentTypes = new List<EmploymentType>
            {
                new() { Code = "FULL_TIME", DisplayName = "常勤", DisplayOrder = 1 },
                new() { Code = "PART_TIME", DisplayName = "パート", DisplayOrder = 2 },
                new() { Code = "NIGHT_ONLY", DisplayName = "夜専", DisplayOrder = 3 }
            };
            context.EmploymentTypes.AddRange(employmentTypes);
            await context.SaveChangesAsync();
        }

        // 資格
        if (!await context.Qualifications.AnyAsync())
        {
            var qualifications = new List<Qualification>
            {
                new() { Code = "NURSE", DisplayName = "看護師" },
                new() { Code = "CARE_WORKER", DisplayName = "介護福祉士" },
                new() { Code = "HELPER", DisplayName = "ヘルパー" },
                new() { Code = "BEGINNER", DisplayName = "初任者研修終了" },
                new() { Code = "PRACTICAL", DisplayName = "実務者研修終了" }
            };
            context.Qualifications.AddRange(qualifications);
            await context.SaveChangesAsync();
        }

        // 勤務種類
        if (!await context.ShiftTypes.AnyAsync())
        {
            var shiftTypes = new List<ShiftType>
            {
                new()
                {
                    Code = "OFF", DisplaySymbol = "ヤ", DisplayName = "休み",
                    Floor = null, ShiftCategory = ShiftCategory.Off,
                    LoadFactor = 0, IsWorkDay = false, DisplayOrder = 1,
                    ExcelBackgroundColor = "#FFFFFF", ExcelFontColor = "#000000"
                },
                new()
                {
                    Code = "DAY", DisplaySymbol = "日", DisplayName = "日勤",
                    Floor = null, ShiftCategory = ShiftCategory.Day,
                    LoadFactor = 1.0, IsWorkDay = true, DisplayOrder = 2,
                    ExcelBackgroundColor = "#FFFFFF", ExcelFontColor = "#0000FF"
                },
                new()
                {
                    Code = "EARLY_2F", DisplaySymbol = "2早", DisplayName = "2階早出",
                    Floor = 2, ShiftCategory = ShiftCategory.Early,
                    LoadFactor = 2.0, IsWorkDay = true, DisplayOrder = 3,
                    ExcelBackgroundColor = "#FFF2CC", ExcelFontColor = "#000000"
                },
                new()
                {
                    Code = "LATE_2F", DisplaySymbol = "2オ", DisplayName = "2階遅出",
                    Floor = 2, ShiftCategory = ShiftCategory.Late,
                    LoadFactor = 2.0, IsWorkDay = true, DisplayOrder = 4,
                    ExcelBackgroundColor = "#FCE4D6", ExcelFontColor = "#000000"
                },
                new()
                {
                    Code = "NIGHT_2F", DisplaySymbol = "2－", DisplayName = "2階夜勤入り",
                    Floor = 2, ShiftCategory = ShiftCategory.NightIn,
                    LoadFactor = 3.0, IsWorkDay = true, DisplayOrder = 5,
                    ExcelBackgroundColor = "#D9E1F2", ExcelFontColor = "#000000"
                },
                new()
                {
                    Code = "EARLY_3F", DisplaySymbol = "3早", DisplayName = "3階早出",
                    Floor = 3, ShiftCategory = ShiftCategory.Early,
                    LoadFactor = 2.0, IsWorkDay = true, DisplayOrder = 6,
                    ExcelBackgroundColor = "#E2EFDA", ExcelFontColor = "#000000"
                },
                new()
                {
                    Code = "LATE_3F", DisplaySymbol = "3オ", DisplayName = "3階遅出",
                    Floor = 3, ShiftCategory = ShiftCategory.Late,
                    LoadFactor = 2.0, IsWorkDay = true, DisplayOrder = 7,
                    ExcelBackgroundColor = "#DDEBF7", ExcelFontColor = "#000000"
                },
                new()
                {
                    Code = "NIGHT_3F", DisplaySymbol = "3－", DisplayName = "3階夜勤入り",
                    Floor = 3, ShiftCategory = ShiftCategory.NightIn,
                    LoadFactor = 3.0, IsWorkDay = true, DisplayOrder = 8,
                    ExcelBackgroundColor = "#EDEDED", ExcelFontColor = "#000000"
                },
                new()
                {
                    Code = "NIGHT_OFF", DisplaySymbol = "－", DisplayName = "夜勤明け",
                    Floor = null, ShiftCategory = ShiftCategory.NightOff,
                    LoadFactor = 1.0, IsWorkDay = true, DisplayOrder = 9,
                    ExcelBackgroundColor = "#F2F2F2", ExcelFontColor = "#7F7F7F"
                },
                new()
                {
                    Code = "PAID_OFF", DisplaySymbol = "有", DisplayName = "有給",
                    Floor = null, ShiftCategory = ShiftCategory.PaidOff,
                    LoadFactor = 0, IsWorkDay = false, DisplayOrder = 10,
                    ExcelBackgroundColor = "#E2EFDA", ExcelFontColor = "#375623"
                },
                new()
                {
                    Code = "OFFICE", DisplaySymbol = "事", DisplayName = "事務",
                    Floor = null, ShiftCategory = ShiftCategory.Other,
                    LoadFactor = 1.0, IsWorkDay = true, DisplayOrder = 11,
                    ExcelBackgroundColor = "#FFFFFF", ExcelFontColor = "#000000"
                }
            };
            context.ShiftTypes.AddRange(shiftTypes);
            await context.SaveChangesAsync();
        }

        // 1日の必要人数設定（デフォルト）
        if (!await context.ShiftRequirements.AnyAsync())
        {
            var shifts = await context.ShiftTypes.ToListAsync();
            var requiredCodes = new[] { "EARLY_2F", "LATE_2F", "NIGHT_2F", "EARLY_3F", "LATE_3F", "NIGHT_3F", "DAY" };
            foreach (var code in requiredCodes)
            {
                var st = shifts.FirstOrDefault(s => s.Code == code);
                if (st != null)
                {
                    context.ShiftRequirements.Add(new ShiftRequirement
                    {
                        ShiftTypeId = st.Id,
                        RequiredCount = 1,
                        IsActive = true
                    });
                }
            }
            await context.SaveChangesAsync();
        }

        // 職員マスタ（14名）
        if (!await context.Staff.AnyAsync())
        {
            var nursingJob = await context.JobTypes.FirstAsync(j => j.Code == "NURSING");
            var careJob = await context.JobTypes.FirstAsync(j => j.Code == "CARE");

            var fullTime = await context.EmploymentTypes.FirstAsync(e => e.Code == "FULL_TIME");
            var partTime = await context.EmploymentTypes.FirstAsync(e => e.Code == "PART_TIME");
            var nightOnly = await context.EmploymentTypes.FirstAsync(e => e.Code == "NIGHT_ONLY");

            var nurseQ = await context.Qualifications.FirstAsync(q => q.Code == "NURSE");
            var careQ = await context.Qualifications.FirstAsync(q => q.Code == "CARE_WORKER");
            var helperQ = await context.Qualifications.FirstAsync(q => q.Code == "HELPER");
            var beginnerQ = await context.Qualifications.FirstAsync(q => q.Code == "BEGINNER");
            var practicalQ = await context.Qualifications.FirstAsync(q => q.Code == "PRACTICAL");

            var staffList = new List<Staff>
            {
                // 看護職
                new() { LastName = "岩﨑", FirstName = "裕子", DisplayName = "岩﨑　裕子", JobTypeId = nursingJob.Id, EmploymentTypeId = fullTime.Id, QualificationId = nurseQ.Id, DisplayOrder = 1, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new() { LastName = "松尾", FirstName = "理恵", DisplayName = "松尾　理恵", JobTypeId = nursingJob.Id, EmploymentTypeId = fullTime.Id, QualificationId = nurseQ.Id, DisplayOrder = 2, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new() { LastName = "長岡", FirstName = "妙子", DisplayName = "長岡　妙子", JobTypeId = nursingJob.Id, EmploymentTypeId = nightOnly.Id, QualificationId = nurseQ.Id, DisplayOrder = 3, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },

                // 介護職
                new() { LastName = "上松", FirstName = "伸次", DisplayName = "上松　伸次", JobTypeId = careJob.Id, EmploymentTypeId = fullTime.Id, QualificationId = careQ.Id, DisplayOrder = 4, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new() { LastName = "荻野", FirstName = "琢磨", DisplayName = "荻野　琢磨", JobTypeId = careJob.Id, EmploymentTypeId = fullTime.Id, QualificationId = careQ.Id, DisplayOrder = 5, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new() { LastName = "長岡", FirstName = "善行", DisplayName = "長岡　善行", JobTypeId = careJob.Id, EmploymentTypeId = fullTime.Id, QualificationId = careQ.Id, DisplayOrder = 6, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new() { LastName = "野口", FirstName = "恵子", DisplayName = "野口　恵子", JobTypeId = careJob.Id, EmploymentTypeId = fullTime.Id, QualificationId = helperQ.Id, DisplayOrder = 7, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new() { LastName = "松浦", FirstName = "洋子", DisplayName = "松浦　洋子", JobTypeId = careJob.Id, EmploymentTypeId = fullTime.Id, QualificationId = helperQ.Id, DisplayOrder = 8, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new() { LastName = "高橋", FirstName = "志奈", DisplayName = "高橋　志奈", JobTypeId = careJob.Id, EmploymentTypeId = fullTime.Id, QualificationId = careQ.Id, DisplayOrder = 9, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new() { LastName = "岩丸", FirstName = "保子", DisplayName = "岩丸　保子", JobTypeId = careJob.Id, EmploymentTypeId = fullTime.Id, QualificationId = careQ.Id, DisplayOrder = 10, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new() { LastName = "永尾", FirstName = "昭貴", DisplayName = "永尾　昭貴", JobTypeId = careJob.Id, EmploymentTypeId = fullTime.Id, QualificationId = careQ.Id, DisplayOrder = 11, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new() { LastName = "浮島", FirstName = "芙美子", DisplayName = "浮島　芙美子", JobTypeId = careJob.Id, EmploymentTypeId = fullTime.Id, QualificationId = beginnerQ.Id, DisplayOrder = 12, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new() { LastName = "梶原", FirstName = "京子", DisplayName = "梶原　京子", JobTypeId = careJob.Id, EmploymentTypeId = fullTime.Id, QualificationId = careQ.Id, DisplayOrder = 13, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new() { LastName = "三吉", FirstName = "孝明", DisplayName = "三吉　孝明", JobTypeId = careJob.Id, EmploymentTypeId = partTime.Id, QualificationId = practicalQ.Id, DisplayOrder = 14, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
            };

            context.Staff.AddRange(staffList);
            await context.SaveChangesAsync();

            // 固定制約のシード
            var nagaoka = staffList.First(s => s.DisplayName.Contains("長岡") && s.DisplayName.Contains("妙子"));
            var uematsu = staffList.First(s => s.DisplayName.Contains("上松"));
            var matsuura = staffList.First(s => s.DisplayName.Contains("松浦"));
            var miyoshi = staffList.First(s => s.DisplayName.Contains("三吉"));

            var permConstraints = new List<StaffPermanentConstraint>
            {
                // 長岡妙子: 2階夜勤のみ可能（希望日のみ）
                new() { StaffId = nagaoka.Id, Type = ConstraintType.NagaokaSpecialRule, Level = ConstraintLevel.Hard, Priority = 5, Note = "2階夜勤専従（希望日のみ勤務）" },
                // 上松伸次: 3階勤務不可
                new() { StaffId = uematsu.Id, Type = ConstraintType.Floor3Unavailable, Level = ConstraintLevel.Hard, Priority = 5, Note = "2階勤務専従（3階勤務禁止）" },
                // 松浦洋子: 夜勤不可
                new() { StaffId = matsuura.Id, Type = ConstraintType.NightUnavailable, Level = ConstraintLevel.Hard, Priority = 5, Note = "夜勤不可（日勤優先）" },
                // 三吉孝明: 2階勤務不可, 夜勤不可（3階早出・遅出・休みのみ）
                new() { StaffId = miyoshi.Id, Type = ConstraintType.Floor2Unavailable, Level = ConstraintLevel.Hard, Priority = 5, Note = "3階早出・遅出・休みのみ（2階禁止）" },
                new() { StaffId = miyoshi.Id, Type = ConstraintType.NightUnavailable, Level = ConstraintLevel.Hard, Priority = 5, Note = "夜勤不可" }
            };

            context.StaffPermanentConstraints.AddRange(permConstraints);
            await context.SaveChangesAsync();
        }
    }
}
