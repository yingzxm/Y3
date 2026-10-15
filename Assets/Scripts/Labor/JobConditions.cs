#nullable enable
using System;
using SocialEvolution.Domain;

namespace SocialEvolution.Labor
{
    /// <summary>
    /// 以 <see cref="JobPostingId"/> 为键的劳动条件：工时、强度、班次、学历和年龄。
    /// 编制、时薪、技能编号和技能等级门槛在 Domain 的岗位上。
    /// </summary>
    public sealed class JobConditions
    {
        public JobConditions(
            JobPostingId postingId,
            float workHours,
            float workIntensity,
            WorkShift shift,
            EducationLevel minEducation = EducationLevel.None,
            int minAge = 0,
            int? maxAge = null,
            int priority = 0)
        {
            if (!IsFiniteNonNegative(workHours))
            {
                throw new ArgumentOutOfRangeException(nameof(workHours), workHours, "工时不能为负或非有限值。");
            }

            if (!IsFiniteNonNegative(workIntensity))
            {
                throw new ArgumentOutOfRangeException(nameof(workIntensity), workIntensity, "劳动强度不能为负或非有限值。");
            }

            if (!Enum.IsDefined(typeof(WorkShift), shift))
            {
                throw new ArgumentOutOfRangeException(nameof(shift), shift, "未知班次。");
            }

            if (!Enum.IsDefined(typeof(EducationLevel), minEducation))
            {
                throw new ArgumentOutOfRangeException(nameof(minEducation), minEducation, "未知学历门槛。");
            }

            if (minAge < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(minAge), minAge, "最低年龄不能为负。");
            }

            if (maxAge.HasValue && maxAge.Value < minAge)
            {
                throw new ArgumentOutOfRangeException(nameof(maxAge), maxAge, "最高年龄不能低于最低年龄。");
            }

            PostingId = postingId;
            WorkHours = workHours;
            WorkIntensity = workIntensity;
            Shift = shift;
            MinEducation = minEducation;
            MinAge = minAge;
            MaxAge = maxAge;
            Priority = priority;
        }

        public JobPostingId PostingId { get; }

        public float WorkHours { get; }

        public float WorkIntensity { get; }

        public WorkShift Shift { get; }

        public EducationLevel MinEducation { get; }

        public int MinAge { get; }

        public int? MaxAge { get; }

        /// <summary>雇主侧排序提示。求职评分不读取它。</summary>
        public int Priority { get; }

        private static bool IsFiniteNonNegative(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0f;
        }
    }
}
