#nullable enable
using SocialEvolution.Core;
using SocialEvolution.Domain;

namespace SocialEvolution.Labor
{
    /// <summary>
    /// 一次自愿入职的记录。只由申请人入职成功后写入，本身不招人。
    /// </summary>
    public sealed class JobEmployment
    {
        internal JobEmployment(IPersonReadModel applicant, JobPostingId postingId, GameDateTime hiredAt)
        {
            if (applicant == null)
            {
                throw new System.ArgumentNullException(nameof(applicant));
            }

            CitizenId = applicant.Id;
            PostingId = postingId;
            HiredAt = hiredAt;
        }

        public CitizenId CitizenId { get; }

        public JobPostingId PostingId { get; }

        public GameDateTime HiredAt { get; }
    }
}
