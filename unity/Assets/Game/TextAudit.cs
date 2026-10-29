// TextAudit.cs — 文の棚卸し (2026-10-30 ユーザー「カードの効果が見切れていて読めない・行が多すぎる・AI っぽくて分からない。全部添削して」)。
// 撮影キー textaudit=1 で、全ての札 (素・鍛えた後) を CardView で実際に組み、面に載る本文・字の大きさ・行数・面で切れたか (Ellipsis) を JSON に書く。
// 遺物・ギア・敵の技の短い文・用語の説明も同じファイルに。読むのは人間と AI (添削の台帳) で、ゲームは読まない。
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using Newtonsoft.Json.Linq;
using DeckRogue.Engine;
using DeckRogue.Engine.Generated;

namespace DeckRogue.Game
{
    public static class TextAudit
    {
        static readonly Regex Tags = new Regex("<[^>]+>");

        public static string Plain(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return Tags.Replace(s, "").Replace(CardText.ClauseOpen.ToString(), "").Replace(CardText.ClauseClose.ToString(), "");
        }

        /// <summary>全部を JSON に書く。parent は札を一時的に組む場所 (画面の根。撮った後に消す)</summary>
        public static void Write(RectTransform parent, string path)
        {
            var o = new JObject();
            var cards = new JArray();
            var defs = new List<CardDef>(Content.AllCards);
            defs.Add(Content.WoundDef); defs.Add(Content.ScaldDef); defs.Add(Content.BrandDef); defs.Add(Content.GuiltDef); defs.Add(Content.JunkDef);
            foreach (var def in defs)
            {
                var ci = new CardInstance { Uid = "audit-" + def.Id, Def = def };
                var jc = Face(parent, ci);
                try
                {
                    if (Upgrade.CanUpgradeCard(ci))
                    {
                        var up = Upgrade.UpgradeCard(ci);
                        jc["upgraded"] = Face(parent, up);
                    }
                }
                catch (Exception ex) { jc["upgradeError"] = ex.Message; }
                cards.Add(jc);
            }
            o["cards"] = cards;

            var relics = new JArray();
            foreach (var r in Content.AllRelics) relics.Add(new JObject { ["id"] = r.Id, ["name"] = r.Name, ["rarity"] = r.Rarity, ["text"] = r.Description ?? "" });
            o["relics"] = relics;

            var gears = new JArray();
            foreach (var gd in Content.AllGears) gears.Add(new JObject { ["id"] = gd.Id, ["name"] = gd.Name, ["text"] = gd.Text ?? "" });
            o["gears"] = gears;

            var enemies = new JArray();
            foreach (var e in Content.AllEnemies)
            {
                var je = new JObject { ["id"] = e.Id, ["name"] = e.Name, ["flavor"] = e.Flavor ?? "" };
                try { je["traits"] = Plain(CardText.EnemyTraits(e)); } catch (Exception ex) { je["traits"] = "ERR " + ex.Message; }
                var moves = new JArray();
                if (e.Moves != null) foreach (var m in e.Moves) { try { moves.Add(new JObject { ["name"] = m.Id, ["short"] = Plain(CardText.MoveShort(m)) }); } catch (Exception ex) { moves.Add(new JObject { ["name"] = m.Id, ["short"] = "ERR " + ex.Message }); } }
                je["moves"] = moves;
                enemies.Add(je);
            }
            o["enemies"] = enemies;

            var terms = new JArray();
            foreach (var kv in KeywordHelp.Terms) terms.Add(new JObject { ["term"] = kv.Key, ["text"] = kv.Value });
            o["terms"] = terms;

            var leaders = new JArray();
            foreach (var l in Content.AllLeaders) leaders.Add(new JObject { ["id"] = l.Id, ["name"] = l.Name, ["text"] = l.Description ?? "" });
            o["leaders"] = leaders;

            File.WriteAllText(path, o.ToString(Newtonsoft.Json.Formatting.Indented));
        }

        /// <summary>札 1 枚を実際に組んで、面の本文の実測を取る</summary>
        static JObject Face(RectTransform parent, CardInstance ci)
        {
            var def = ci.Def;
            var j = new JObject
            {
                ["id"] = def.Id, ["name"] = def.Name, ["type"] = def.Type, ["cost"] = CardText.CostLabel(def), ["rarity"] = def.Rarity ?? "",
            };
            try { j["color"] = def.Color ?? ""; } catch (Exception) { }
            string body = "", notes = "";
            try { body = Plain(CardText.Body(def)); notes = Plain(CardText.Notes(def)); } catch (Exception ex) { body = "ERR " + ex.Message; }
            j["body"] = body; j["notes"] = notes; j["chars"] = body.Length + notes.Length;
            RectTransform rt = null;
            try
            {
                rt = CardView.Build(parent, ci, null, false, false, "audit-card");
                rt.anchoredPosition = new Vector2(-4000f, 0f);   // 画面の外
                var tmps = rt.GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach (var t in tmps)
                {
                    if (t.name != "body" && !(t.transform.parent != null && t.transform.parent.name == "body")) continue;
                    t.ForceMeshUpdate(true);
                    j["fontSize"] = Math.Round(t.fontSize, 1);
                    j["lines"] = t.textInfo.lineCount;
                    j["truncated"] = CardView.LastBodyTruncated || t.overflowMode == TextOverflowModes.Ellipsis;
                    break;
                }
            }
            catch (Exception ex) { j["faceError"] = ex.Message; }
            finally { if (rt != null) UnityEngine.Object.Destroy(rt.gameObject); }
            return j;
        }
    }
}
