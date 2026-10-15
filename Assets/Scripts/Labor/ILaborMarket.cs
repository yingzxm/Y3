#nullable enable
using System.Collections.Generic;
using SocialEvolution.Core;
using SocialEvolution.Domain;

namespace SocialEvolution.Labor
{
    /// <summary>
    /// 劳动力市场。入职只有申请人 <see cref="TryApply"/>：合格并且还有空缺就当场录用。
    /// 没有雇主申请池，也没有点名把市民放进岗位的方法。
    /// 一个人同时只做一份工作；已经在职再申请得到 <see cref="LaborApplyResult.RejectedAlreadyEmployed"/>。
    /// </summary>
    public interface ILaborMarket
    {
        /// <summary>登记或改写某个编制的劳动条件。编制本身仍用 World.SetJobPosting。</summary>
        void SetConditions(JobConditions conditions);

        bool TryGetPosting(JobPostingId postingId, out JobPosting? posting);

        IReadOnlyList<JobPosting> GetPostings();

        /// <summary>仍有空缺的岗位，按岗位编号升序。</summary>
        IReadOnlyList<JobPosting> GetOpenPostings();

        int GetFilledCount(JobPostingId postingId);

        /// <summary>max(0, 编制 − 在职)。编制未招满时可以大于 0，不再恒等于编制。</summary>
        int GetVacantSlots(JobPostingId postingId);

        IReadOnlyList<JobEmployment> GetEmployments(JobPostingId postingId);

        bool TryGetEmployment(CitizenId citizenId, out JobEmployment? employment);

        /// <summary>申请人自愿应聘。合格且有空缺则当场入职。</summary>
        LaborApplyResult TryApply(IPersonReadModel applicant, JobPostingId postingId, GameDateTime now);

        /// <summary>申请人辞去自己那一份工作。没在职则返回 false。</summary>
        bool TryQuit(IPersonReadModel applicant);
    }

    public enum LaborApplyResult
    {
        Hired = 0,
        RejectedIneligible = 1,
        RejectedNoVacancy = 2,
        RejectedAlreadyEmployed = 3,
        PostingNotFound = 4,
    }
}
