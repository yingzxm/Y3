#nullable enable
using System;
using System.Collections.Generic;
using SocialEvolution.Domain;
using SocialEvolution.Labor;
using SocialEvolution.World;
using DomainJobPosting = SocialEvolution.Domain.JobPosting;
using LaborJobPosting = SocialEvolution.Labor.JobPosting;

namespace SocialEvolution.Bootstrap
{
    /// <summary>
    /// 一座建筑的产量比例：已登记岗位的在职人数 / 编制之和。
    /// 还没登记劳动条件的岗位不计入。一座建筑没有任何已登记岗位时返回 1。
    /// 登记之后如果没人上班，返回 0。
    /// </summary>
    public static class LaborOutputScale
    {
        public static float ForBuilding(ILaborMarket labor, WorldState world, BuildingId buildingId)
        {
            if (labor == null)
            {
                throw new ArgumentNullException(nameof(labor));
            }

            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            BuildingSite? site = null;
            IReadOnlyList<BuildingSite> sites = world.Sites;
            for (int i = 0; i < sites.Count; i++)
            {
                if (sites[i].Id == buildingId)
                {
                    site = sites[i];
                    break;
                }
            }

            if (site == null)
            {
                return 1f;
            }

            int openings = 0;
            int filled = 0;
            IReadOnlyList<DomainJobPosting> postings = site.Building.JobPostings;
            for (int i = 0; i < postings.Count; i++)
            {
                JobPostingId postingId = postings[i].Id;
                LaborJobPosting? view;
                if (!labor.TryGetPosting(postingId, out view) || view == null)
                {
                    continue;
                }

                openings += view.Openings;
                filled += labor.GetFilledCount(postingId);
            }

            if (openings <= 0)
            {
                return 1f;
            }

            if (filled <= 0)
            {
                return 0f;
            }

            if (filled >= openings)
            {
                return 1f;
            }

            return (float)((double)filled / openings);
        }
    }
}
