#nullable enable
using System;
using SocialEvolution.Domain;
using SocialEvolution.Stats;
using UnityEngine;

namespace SocialEvolution.Bootstrap
{
    /// <summary>
    /// 演示用的统计和点选。挂在和 <see cref="SimulationDriver"/> 同一个物体上。
    /// <see cref="Start"/> 里 <see cref="DemoReadout.Attach"/>，之后每个游戏时把行动写进市民日志。
    /// 完整界面皮肤留给 P0-9。用法见 docs/stats.md。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SimulationDriver))]
    public sealed class StatsDebugPanel : MonoBehaviour
    {
        private string _idText = "1";
        private string _message = string.Empty;

        public DemoReadout? Readout { get; private set; }

        private void Start()
        {
            SimulationDriver driver = GetComponent<SimulationDriver>();
            if (driver == null || driver.Session == null)
            {
                _message = "没有 GameSession。";
                return;
            }

            Readout = DemoReadout.Attach(driver.Session.Time, driver.Session.World);
            _idText = "1";
        }

        /// <summary>场景或按钮按市民编号点选。编号必须是本局市民。</summary>
        public bool SelectCitizen(int citizenId)
        {
            if (Readout == null || citizenId <= 0)
            {
                return false;
            }

            bool selected = Readout.Select(new CitizenId(citizenId));
            if (selected)
            {
                _idText = citizenId.ToString();
                _message = string.Empty;
            }
            else
            {
                _message = "没有这个市民。";
            }

            return selected;
        }

        public bool SelectNextCitizen()
        {
            if (Readout == null)
            {
                return false;
            }

            bool moved = Readout.SelectNext();
            SyncIdText();
            return moved;
        }

        public bool SelectPreviousCitizen()
        {
            if (Readout == null)
            {
                return false;
            }

            bool moved = Readout.SelectPrevious();
            SyncIdText();
            return moved;
        }

        /// <summary>当前市民最近三行日志，从旧到新。不足三行时返回 false。</summary>
        public bool TryGetThreeRecentLogs(out string first, out string second, out string third)
        {
            if (Readout == null)
            {
                first = string.Empty;
                second = string.Empty;
                third = string.Empty;
                return false;
            }

            return Readout.TryGetThreeRecentLogs(out first, out second, out third);
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(8f, 8f, 440f, 560f));
            if (Readout == null)
            {
                GUILayout.Label(string.IsNullOrEmpty(_message) ? "统计还没接上。" : _message);
                GUILayout.EndArea();
                return;
            }

            CityStats stats = Readout.Read();
            GUILayout.Label("人口 " + stats.Population);
            GUILayout.Label("就业率 " + (stats.EmploymentRate * 100d).ToString("0.0") + "%  (" + stats.EmployedCount + ")");
            GUILayout.Label("平均时薪 " + stats.AverageWage.ToString("0.##"));
            GUILayout.Label(
                "中间价  食物 " + stats.FoodMidPrice
                + "   木材 " + stats.WoodMidPrice
                + "   工具 " + stats.ToolMidPrice);
            GUILayout.Label("贫富 " + stats.RichPoorIndex.ToString("0.000") + "  （0 均等，1 极分化）");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("上一位"))
            {
                SelectPreviousCitizen();
            }

            if (GUILayout.Button("下一位"))
            {
                SelectNextCitizen();
            }

            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            _idText = GUILayout.TextField(_idText, GUILayout.Width(80f));
            if (GUILayout.Button("选中"))
            {
                int id;
                if (int.TryParse(_idText, out id))
                {
                    SelectCitizen(id);
                }
                else
                {
                    _message = "编号要是正整数。";
                }
            }

            GUILayout.EndHorizontal();
            if (!string.IsNullOrEmpty(_message))
            {
                GUILayout.Label(_message);
            }

            CitizenView view;
            if (Readout.TryGetSelected(out view))
            {
                GUILayout.Label(view.Name + "  #" + view.Id.Value + "  现金 " + view.Cash);
                if (view.Employed)
                {
                    GUILayout.Label("工作 " + view.JobTitle + "  时薪 " + view.WagePerHour);
                }
                else
                {
                    GUILayout.Label("工作 无");
                }

                if (view.HasTopNeed)
                {
                    GUILayout.Label("最急需求 " + view.TopNeedLabel + "  " + view.TopNeedUrgency.ToString("0.00"));
                }
                else
                {
                    GUILayout.Label("最急需求 无");
                }

                GUILayout.Label("最近行动");
                for (int i = 0; i < view.RecentLogs.Count; i++)
                {
                    GUILayout.Label(view.RecentLogs[i].Text);
                }

                if (view.RecentLogs.Count < 3)
                {
                    GUILayout.Label("日志还不到 3 行。");
                }
            }

            GUILayout.EndArea();
        }

        private void SyncIdText()
        {
            CitizenView view;
            if (Readout != null && Readout.TryGetSelected(out view))
            {
                _idText = view.Id.Value.ToString();
                _message = string.Empty;
            }
        }
    }
}
