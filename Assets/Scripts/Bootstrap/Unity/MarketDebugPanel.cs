#nullable enable
using System;
using SocialEvolution.Core;
using SocialEvolution.Domain;
using SocialEvolution.Economy;
using SocialEvolution.World;
using UnityEngine;

namespace SocialEvolution.Bootstrap
{
    /// <summary>
    /// Bootstrap 场景里的市场调试面板。和 <see cref="SimulationDriver"/>、<see cref="StatsDebugPanel"/> 挂在同一个物体上。
    /// 宽屏时画在统计面板右侧，窄屏时画在它下面。只做观察和政府买卖。设岗、设薪、建设仍属于 P0-7。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MarketDebugPanel : MonoBehaviour
    {
        private SimulationDriver? _driver;
        private string _quantityText = "10";
        private string _status = "暂停后买卖，中间价会停在成交价上。";
        private Vector2 _scroll;
        private Texture2D? _background;
        private GUIStyle? _label;
        private GUIStyle? _title;
        private GUIStyle? _button;

        private void Awake()
        {
            _driver = GetComponent<SimulationDriver>();
            EnsureCamera();
        }

        private void OnGUI()
        {
            EnsureStyles();
            float x;
            float y;
            float width;
            float height;
            Place(out x, out y, out width, out height);
            GUI.DrawTexture(new Rect(x, y, width, height), _background!);
            GUILayout.BeginArea(new Rect(x + 12f, y + 8f, width - 24f, height - 16f));
            _scroll = GUILayout.BeginScrollView(_scroll);

            GameSession? session = _driver != null ? _driver.Session : null;
            if (session == null || session.Market == null)
            {
                GUILayout.Label("等待开局。SimulationDriver 会在 Awake 里挂上市场。", _label);
                GUILayout.EndScrollView();
                GUILayout.EndArea();
                return;
            }

            ITimeService time = session.Time;
            CityMarket market = session.Market;
            WorldState world = session.World;

            GUILayout.Label("社会演化 · 市场 P0-6", _title);
            GUILayout.Label(
                "第 " + time.Day + " 日  " + time.Hour + " 时    "
                + (time.IsPaused ? "已暂停" : "运行中")
                + "    倍率 " + time.TimeScale.ToString("0.##"),
                _label);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("暂停 Pause", _button))
            {
                _driver!.Pause();
            }

            if (GUILayout.Button("继续 Resume", _button))
            {
                _driver!.Resume();
            }

            if (GUILayout.Button("1x", _button))
            {
                _driver!.SetTimeScale(TimeScales.X1);
            }

            if (GUILayout.Button("5x", _button))
            {
                _driver!.SetTimeScale(TimeScales.X5);
            }

            GUILayout.EndHorizontal();

            GUILayout.Label("金库 Treasury   " + world.Treasury.Balance, _title);
            GUILayout.Label("数量 Quantity", _label);
            _quantityText = GUILayout.TextField(_quantityText ?? string.Empty, 8);
            GUILayout.Label(_status, _label);
            GUILayout.Space(6f);

            DrawGood(market, world, Good.Food, "食物 Food");
            DrawGood(market, world, Good.Wood, "木材 Wood");
            DrawGood(market, world, Good.Tool, "工具 Tool");

            GUILayout.Space(8f);
            GUILayout.Label("编制工资（只读）。设岗设薪属 P0-7。", _label);
            for (int i = 0; i < world.Sites.Count; i++)
            {
                BuildingSite site = world.Sites[i];
                System.Collections.Generic.IReadOnlyList<JobPosting> postings = site.Building.JobPostings;
                for (int j = 0; j < postings.Count; j++)
                {
                    JobPosting posting = postings[j];
                    GUILayout.Label(
                        site.Building.Name + "  " + posting.Title
                        + "  编制 " + posting.Openings
                        + "  时薪 " + posting.WagePerHour,
                        _label);
                }
            }

            GUILayout.Label(
                market.ScalesWithStaffing
                    ? "每游戏时：按到岗人数 / 编制缩放产量，向下取整。没人上班就不产。有产出则卖掉超出储备的部分，然后结算中间价。"
                    : "每游戏时：农场出食物和木材，工厂耗木材出工具，超出储备的产出卖进集市，然后结算中间价。",
                _label);
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawGood(CityMarket market, WorldState world, Good good, string caption)
        {
            GoodMarketState state = market.GetState(good);
            GUILayout.Label(
                caption
                + "    中间价 " + state.MidPrice
                + "    压力 " + state.InventoryPressure
                + "    最近成交 " + state.RecentVolume
                + " @ " + state.LastTradePrice,
                _title);
            GUILayout.Label(
                "集市 " + Sum(world, BuildingKind.Market, good)
                + "    政府 " + Sum(world, BuildingKind.Government, good)
                + "    农场 " + Sum(world, BuildingKind.Farm, good)
                + "    工厂 " + Sum(world, BuildingKind.Factory, good),
                _label);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("买入 Buy", _button))
            {
                Trade(market, good, buy: true);
            }

            if (GUILayout.Button("卖出 Sell", _button))
            {
                Trade(market, good, buy: false);
            }

            GUILayout.EndHorizontal();
            GUILayout.Space(4f);
        }

        private void Trade(CityMarket market, Good good, bool buy)
        {
            if (!int.TryParse(_quantityText, out int quantity))
            {
                _status = "数量要写成整数。";
                return;
            }

            long before = market.GetState(good).MidPrice;
            long treasuryBefore = market.World.Treasury.Balance;
            MarketResult result = buy
                ? market.TryGovernmentBuy(good, quantity)
                : market.TryGovernmentSell(good, quantity);
            long after = market.GetState(good).MidPrice;
            if (!result.Succeeded)
            {
                _status = NameOf(good) + " 没有成交：" + result.Rejection + "。库存、金库和中间价未改。";
                return;
            }

            _status = (buy ? "政府买入 " : "政府卖出 ")
                + NameOf(good) + " " + result.Quantity
                + "，成交价 " + result.UnitPrice
                + "，中间价 " + before + " → " + after
                + "，金库 " + treasuryBefore + " → " + market.World.Treasury.Balance;
        }

        private static void Place(out float x, out float y, out float width, out float height)
        {
            width = 620f;
            if (Screen.width >= 1100f)
            {
                x = 460f;
                y = 8f;
                height = Screen.height - 16f;
                if (height < 240f)
                {
                    height = 240f;
                }

                return;
            }

            x = 8f;
            y = 576f;
            height = 640f;
        }

        private static int Sum(WorldState world, BuildingKind kind, Good good)
        {
            int total = 0;
            for (int i = 0; i < world.Sites.Count; i++)
            {
                BuildingSite site = world.Sites[i];
                if (site.Building.Kind == kind)
                {
                    total += site.Inventory.GetQuantity(good);
                }
            }

            return total;
        }

        private static string NameOf(Good good)
        {
            switch (good)
            {
                case Good.Food:
                    return "食物";
                case Good.Wood:
                    return "木材";
                case Good.Tool:
                    return "工具";
                default:
                    return good.ToString();
            }
        }

        private void EnsureCamera()
        {
            if (Camera.main != null)
            {
                return;
            }

            var cameraObject = new GameObject("Market Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.1f, 0.11f, 0.13f, 1f);
            camera.orthographic = true;
            cameraObject.tag = "MainCamera";
        }

        private void EnsureStyles()
        {
            if (_label != null)
            {
                return;
            }

            Font? font = null;
            try
            {
                font = Font.CreateDynamicFontFromOSFont(
                    new[] { "Microsoft YaHei UI", "Microsoft YaHei", "SimHei", "Noto Sans CJK SC", "Arial" },
                    16);
            }
            catch (Exception)
            {
                font = null;
            }

            _background = new Texture2D(1, 1);
            _background.SetPixel(0, 0, new Color(0.08f, 0.09f, 0.11f, 0.94f));
            _background.Apply();

            _label = new GUIStyle(GUI.skin.label);
            _label.fontSize = 15;
            _label.wordWrap = true;
            _label.normal.textColor = new Color(0.92f, 0.93f, 0.94f, 1f);
            _title = new GUIStyle(_label);
            _title.fontSize = 18;
            _title.fontStyle = FontStyle.Bold;
            _button = new GUIStyle(GUI.skin.button);
            _button.fontSize = 15;
            if (font != null)
            {
                _label.font = font;
                _title.font = font;
                _button.font = font;
            }
        }
    }
}
