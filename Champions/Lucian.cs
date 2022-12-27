using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.Linq;
using EnsoulSharp;
using EnsoulSharp.SDK;
using System.Threading.Tasks;
using System.Text;
using SharpDX;
using Color = System.Drawing.Color;
using EnsoulSharp.SDK.MenuUI;
using System.Reflection;
using System.Net.Http;
using System.Net;
using System.Runtime.Remoting.Messaging;
using System.Text.RegularExpressions;
using SebbyLib;
using SharpDX.Direct3D9;

namespace ToxicAio.Champions
{
    public class Lucian
    {
        private static Spell Q, Q1, W, E, R, R1;
        private static Menu Config, menuQ, menuW, menuE, menuR, menuL, menuK, menuM, menuD;
        private static SpellSlot igniteSlot;
        private static HitChance hitchance;
        private static AIHeroClient Me = ObjectManager.Player;
        private static bool havePassive = false;
        private static int lastCastTime = 0;

        public static void OnGameLoad()
        {
            if (Me.CharacterName != "Lucian")
            {
                return;
            }

            Q = new Spell(SpellSlot.Q, 555f);
            Q1 = new Spell(SpellSlot.Q, 1000f);
            W = new Spell(SpellSlot.W, 900f);
            E = new Spell(SpellSlot.E, 425f);
            R = new Spell(SpellSlot.R, 1200f);
            R1 = new Spell(SpellSlot.R, 1200f);

            Q.SetTargetted(0.4f, float.MaxValue);
            W.SetSkillshot(0.25f, 55f, 1600f, true, SpellType.Line);
            R.SetSkillshot(0.1f, 110f, float.MaxValue, true, SpellType.Line);
            Q1.SetSkillshot(0.4f, 60f, float.MaxValue, true, SpellType.Line);
            R1.SetSkillshot(0.1f, 110f, float.MaxValue, false, SpellType.Line);

            igniteSlot = Me.GetSpellSlot("SummonerDot");


            Config = new Menu("Lucian", "[ToxicAio]: Luciam", true);

            menuQ = new Menu("Qsettings", "Q settings");
            menuQ.Add(new MenuBool("UseQ", "Use Q in Combo"));
            menuQ.Add(new MenuBool("Qex", "Use Q Extend"));
            Config.Add(menuQ);

            menuW = new Menu("Wsettings", "W settings");
            menuW.Add(new MenuBool("UseW", "Use W in Combo"));
            menuW.Add(new MenuBool("Waa", "use W only in AA Range"));
            menuW.Add(new MenuList("Pred", "Prediction hitchance",
                new string[] {"Low", "Medium", "High", " Very High"}, 2));
            Config.Add(menuW);

            menuE = new Menu("Esettings", "E settings");
            menuE.Add(new MenuBool("UseE", "Use E in Combo"));
            menuE.Add(new MenuBool("Esafe", "Use E Safe Check"));
            menuE.Add(new MenuList("Range", "E Mode",
                new string[] {"Short", "Long"}, 1));
            Config.Add(menuE);

            menuR = new Menu("Rsettings", "R settings");
            menuR.Add(new MenuBool("UseR", "Use R in Combo"));
            Config.Add(menuR);


            menuL = new Menu("Clear", "Clear settings");
            menuL.Add(new MenuBool("LcQ", "Use Q in Laneclear"));
            menuL.Add(new MenuBool("LcW", "Use W in Laneclear"));
            menuL.Add(new MenuBool("JcQ", "Use Q in Jungleclear"));
            menuL.Add(new MenuBool("JcW", "Use W in Jungleclear"));
            Config.Add(menuL);


            menuK = new Menu("Killsteal", "Killsteal settings");
            menuK.Add(new MenuBool("KsQ", "Use Q to Killsteal"));
            menuK.Add(new MenuBool("KsW", "Use W to Killsteal"));
            Config.Add(menuK);


            menuM = new Menu("Misc", "Misc settings");
            menuM.Add(new MenuSliderButton("Skin", "SkindID", 0, 0, 30, false));
            Config.Add(menuM);


            menuD = new Menu("Draw", "Draw settings");
            menuD.Add(new MenuBool("drawQ", "Q Range  (White)", true));
            menuD.Add(new MenuBool("drawW", "W Range  (White)", true));
            menuD.Add(new MenuBool("drawE", "E Range (White)", true));
            menuD.Add(new MenuBool("drawR", "R Range  (Red)", true));
            menuD.Add(new MenuBool("drawD", "Draw Combo Damage", true));
            Config.Add(menuD);

            Config.Attach();

            GameEvent.OnGameTick += OnGameUpdate;
            Drawing.OnDraw += OnDraw;
            AIBaseClient.OnProcessSpellCast += OnProcessSpellCast;
            Spellbook.OnCastSpell += OnCastSpell;
        }

        static int comb(Menu submenu, string sig)
        {
            return submenu[sig].GetValue<MenuList>().Index;
        }

        public static void OnGameUpdate(EventArgs args)
        {

            if (Me.HasBuff("LucianR"))
            {
                Me.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
                return;
            }

            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
            {
                LogicE();
                LogicQ();
                LogicW();
                LogicR();
            }

            if (Orbwalker.ActiveMode == OrbwalkerMode.LaneClear)
            {
                Jungle();
                Laneclear();
            }

            if (Orbwalker.ActiveMode == OrbwalkerMode.LastHit)
            {
            }

            if (Orbwalker.ActiveMode == OrbwalkerMode.Harass)
            {

            }
            Killsteal();
            skind();
        }

        private static void skind()
        {
            if (Config["Misc"].GetValue<MenuSliderButton>("Skin").Enabled)
            {
                int skinnu = Config["Misc"].GetValue<MenuSliderButton>("Skin").Value;

                if (Me.SkinId != skinnu)
                    Me.SetSkin(skinnu);
            }
        }

        private static float ComboDamage(AIBaseClient enemy)
        {
            var damage = 0d;
            if (igniteSlot != SpellSlot.Unknown && Me.Spellbook.CanUseSpell(igniteSlot) == SpellState.Ready)
                damage += Me.GetSummonerSpellDamage(enemy, SummonerSpell.Ignite);
            if (Q.IsReady())
                damage += Me.GetSpellDamage(enemy, SpellSlot.Q);
            if (W.IsReady())
                damage += Me.GetSpellDamage(enemy, SpellSlot.W);
            if (E.IsReady())
                damage += Me.GetAutoAttackDamage(enemy) * 2;
            if (R.IsReady())
                damage += Me.GetSpellDamage(enemy, SpellSlot.R);

            return (float) damage;
        }

        private static void OnDraw(EventArgs args)
        {
            if (Config["Draw"].GetValue<MenuBool>("drawQ").Enabled)
            {
                Render.Circle.DrawCircle(Me.Position, Q.Range, System.Drawing.Color.White);
            }

            if (Config["Draw"].GetValue<MenuBool>("drawW").Enabled)
            {
                Render.Circle.DrawCircle(Me.Position, W.Range, System.Drawing.Color.White);
            }

            if (Config["Draw"].GetValue<MenuBool>("drawE").Enabled)
            {
                Render.Circle.DrawCircle(Me.Position, E.Range, System.Drawing.Color.White);
            }

            if (Config["Draw"].GetValue<MenuBool>("drawR").Enabled)
            {
                Render.Circle.DrawCircle(Me.Position, R.Range, System.Drawing.Color.Red);
            }

            if (Config["Draw"].GetValue<MenuBool>("drawD").Enabled)
            {
                foreach (
                    var enemyVisible in
                    ObjectManager.Get<AIHeroClient>().Where(enemyVisible => enemyVisible.IsValidTarget()))
                {

                    if (ComboDamage(enemyVisible) > enemyVisible.Health)
                    {
                        Drawing.DrawText(Drawing.WorldToScreen(enemyVisible.Position)[0] + 50,
                            Drawing.WorldToScreen(enemyVisible.Position)[1] - 40, Color.Red,
                            "Combo=Kill");
                    }
                    else if (ComboDamage(enemyVisible) +
                        Me.GetAutoAttackDamage(enemyVisible, true) * 2 > enemyVisible.Health)
                    {
                        Drawing.DrawText(Drawing.WorldToScreen(enemyVisible.Position)[0] + 50,
                            Drawing.WorldToScreen(enemyVisible.Position)[1] - 40, Color.Orange,
                            "Combo + 2 AA = Kill");
                    }
                    else
                        Drawing.DrawText(Drawing.WorldToScreen(enemyVisible.Position)[0] + 50,
                            Drawing.WorldToScreen(enemyVisible.Position)[1] - 40, Color.Green,
                            "Unkillable with combo + 2 AA");
                }
            }
        }

        private static void OnCastSpell(Spellbook sender, SpellbookCastSpellEventArgs Args)
        {
            if (sender?.Owner != null && sender.Owner.IsMe)
            {
                if (Args.Slot == SpellSlot.Q || Args.Slot == SpellSlot.W || Args.Slot == SpellSlot.E)
                {
                    havePassive = true;
                    lastCastTime = Variables.GameTimeTickCount;
                }

                if (Args.Slot == SpellSlot.E && Orbwalker.ActiveMode != OrbwalkerMode.None)
                {
                    Orbwalker.ResetAutoAttackTimer();
                }
            }
        }

        private static void OnProcessSpellCast(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
        {
            if (sender.IsMe)
            {
                if (args.Slot == SpellSlot.Q || args.Slot == SpellSlot.W || args.Slot == SpellSlot.E)
                {
                    havePassive = true;
                    lastCastTime = Variables.GameTimeTickCount;
                }
            }
        }

        private static void QLogic(AIHeroClient target, bool useExtendQ = true)
        {
            if (!Q.IsReady() || target == null || target.IsDead || target.IsInvulnerable)
            {
                return;
            }

            if (target.IsValidTarget(Q.Range))
            {
                Q.CastOnUnit(target);
            }
            else if (target.IsValidTarget(Q1.Range) && useExtendQ)
            {
                var coll = GameObjects.EnemyMinions.Where(x =>
                        x.IsValidTarget(Q.Range) && (x.IsMinion() || x.GetJungleType() != JungleType.Unknown))
                    .ToList();

                if (!coll.Any())
                {
                    return;
                }

                foreach (var minion in coll)
                {
                    var qPred = Q1.GetPrediction(target);
                    var qPolygon = new Geometry.Rectangle(Me.PreviousPosition,
                        Me.PreviousPosition.Extend(minion.Position, Q1.Range), Q1.Width);

                    if (qPolygon.IsInside(qPred.UnitPosition.ToVector2()) && minion.IsValidTarget(Q.Range))
                    {
                        Q.CastOnUnit(minion);
                        break;
                    }
                }
            }
        }

        private static void LogicQ()
        {
            var qtarget = Q.GetTarget(Q.Range);
            var q2target = Q1.GetTarget(Q1.Range);
            var useQ = Config["Qsettings"].GetValue<MenuBool>("UseQ").Enabled;
            var useQextend = Config["Qsettings"].GetValue<MenuBool>("Qex").Enabled;
            if (qtarget == null) return;

            if (Q.IsReady() && useQ && !useQextend && qtarget.IsValidTarget(Q.Range))
            {
                Q.Cast(qtarget);
            }
            else if (Q.IsReady() && !havePassive && Me.Buffs.All(x => x.Name.ToLower() != "lucianpassivebuff") &&
                     !Me.IsDashing() && useQextend && useQ)
            {
                if (q2target.IsValidTarget(Q1.Range))
                {
                    QLogic(q2target, useQextend);
                }
            }
        }

        private static void LogicW()
        {
            var wtarget = W.GetTarget(W.Range);
            var useW = Config["Wsettings"].GetValue<MenuBool>("UseW").Enabled;
            var waa = Config["Wsettings"].GetValue<MenuBool>("Waa").Enabled;
            var input = W.GetPrediction(wtarget);
            if (wtarget == null) return;

            switch (comb(menuW, "Pred"))
            {
                case 0:
                    hitchance = HitChance.Low;
                    break;
                case 1:
                    hitchance = HitChance.Medium;
                    break;
                case 2:
                    hitchance = HitChance.High;
                    break;
                case 3:
                    hitchance = HitChance.VeryHigh;
                    break;
                default:
                    hitchance = HitChance.High;
                    break;
            }

            if (W.IsReady() && useW && wtarget.IsValidTarget(W.Range) && input.Hitchance >= hitchance && !waa)
            {
                W.Cast(input.CastPosition);
            }
            else if (W.IsReady() && useW && waa && wtarget.IsValidTarget(W.Range) && input.Hitchance >= hitchance &&
                     wtarget.InAutoAttackRange())
            {
                W.Cast(input.CastPosition);
            }
        }

        private static void LogicE()
        {
            var etarget = E.GetTarget(E.Range);
            var useE = Config["Esettings"].GetValue<MenuBool>("UseE").Enabled;
            var useESafe = Config["Esettings"].GetValue<MenuBool>("Esafe").Enabled;
            var dashRange = Me.PreviousPosition.DistanceToCursor() > Me.GetRealAutoAttackRange(etarget) ? E.Range : 130;
            var dashPos = Me.PreviousPosition.Extend(Game.CursorPos, dashRange);
            if (etarget == null) return;

            switch (comb(menuE, "Range"))
            {
                case 0:

                    if (E.IsReady() && useE && etarget.IsValidTarget(E.Range) && useESafe &&
                        dashPos.CountEnemyHeroesInRange(500) >= 3 && dashPos.CountAllyHeroesInRange(400) < 3)
                    {
                        return;
                    }
                    else if (E.IsReady() && useE && etarget.IsValidTarget(E.Range) && useESafe &&
                             dashPos.CountEnemyHeroesInRange(500) >= 3 && dashPos.CountAllyHeroesInRange(400) >= 3)
                    {
                        E.Cast(dashPos);
                    }
                    else if (E.IsReady() && useE && etarget.IsValidTarget(E.Range) &&
                             dashPos.CountEnemyHeroesInRange(500) < 2)
                    {
                        E.Cast(dashPos);
                    }
                    else if (E.IsReady() && useE && etarget.IsValidTarget(E.Range) && !useESafe)
                    {
                        E.Cast(dashPos);
                    }

                    break;

                case 1:

                    if (E.IsReady() && useE && etarget.IsValidTarget(E.Range) && useESafe &&
                        dashPos.CountEnemyHeroesInRange(500) >= 3 && dashPos.CountAllyHeroesInRange(400) < 3)
                    {
                        return;
                    }
                    else if (E.IsReady() && useE && etarget.IsValidTarget(E.Range) && useESafe &&
                             dashPos.CountEnemyHeroesInRange(500) >= 3 && dashPos.CountAllyHeroesInRange(400) >= 3)
                    {
                        E.Cast(Game.CursorPos);
                    }
                    else if (E.IsReady() && useE && etarget.IsValidTarget(E.Range) && !useESafe)
                    {
                        E.Cast(Game.CursorPos);
                    }

                    break;
            }
        }

        private static void LogicR()
        {
            var rtarget = R.GetTarget(R.Range);
            var user = Config["Rsettings"].GetValue<MenuBool>("UseR").Enabled;
            var rLevel = Me.Spellbook.GetSpell(SpellSlot.R).Level;
            var rAmmo = new[] {22, 28, 34}[rLevel];
            var rDMG = R.GetDamage(rtarget) * rAmmo;

            if (rtarget.IsValidTarget(R.Range) && !rtarget.IsInvulnerable && !Me.IsUnderEnemyTurret() &&
                !rtarget.IsValidTarget(Me.GetRealAutoAttackRange(rtarget)))
            {
                if (GameObjects.EnemyHeroes.Any(x => x.NetworkId != rtarget.NetworkId && x.Distance(rtarget) <= 550))
                {
                    return;
                }

                if (rtarget.Health + rtarget.HPRegenRate * 3 < rDMG)
                {
                    if (rtarget.DistanceToPlayer() <= 800 && rtarget.Health < rDMG * 0.6 && user)
                    {
                        R.Cast(rtarget);
                    }
                }
            }
        }

        private static void Jungle()
        {
            var JcWw = Config["Clear"].GetValue<MenuBool>("JcW");
            var JcQq = Config["Clear"].GetValue<MenuBool>("JcQ");
            var mobs = GameObjects.Jungle.Where(x => x.IsValidTarget(Q.Range)).OrderBy(x => x.MaxHealth)
                .ToList<AIBaseClient>();
            if (mobs.Count > 0)
            {
                var mob = mobs[0];
                if (JcWw.Enabled && W.IsReady() && ObjectManager.Player.Distance(mob.Position) < W.Range) W.Cast(mob);
                if (JcQq.Enabled && Q.IsReady() && ObjectManager.Player.Distance(mob.Position) < Q.Range) Q.Cast(mob);
            }
        }

        private static void Laneclear()
        {
            var lcq = Config["Clear"].GetValue<MenuBool>("LcQ");
            if (lcq.Enabled && Q.IsReady())
            {
                var minions = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(Q.Range) && x.IsMinion())
                    .Cast<AIBaseClient>().ToList();
                if (minions.Any())
                {
                    var qFarmLocation = Q.GetLineFarmLocation(minions);
                    if (qFarmLocation.Position.IsValid())
                    {
                        Q.Cast(qFarmLocation.Position);
                        return;
                    }
                }
            }

            var lcw = Config["Clear"].GetValue<MenuBool>("LcW");
            if (lcw.Enabled && W.IsReady())
            {
                var minions = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(W.Range) && x.IsMinion())
                    .Cast<AIBaseClient>().ToList();
                if (minions.Any())
                {
                    var wFarmLocation = W.GetLineFarmLocation(minions);
                    if (wFarmLocation.Position.IsValid())
                    {
                        W.Cast(wFarmLocation.Position);
                        return;
                    }
                }
            }
        }

        private static void Killsteal()
        {
            var ksQ = Config["Killsteal"].GetValue<MenuBool>("KsQ").Enabled;
            var ksW = Config["Killsteal"].GetValue<MenuBool>("KsW").Enabled;

            var Qtarget = Q.GetTarget(Q.Range);
            var Wtarget = W.GetTarget(W.Range);


            if (Qtarget == null) return;
            if (Qtarget.IsInvulnerable) return;
            if (Wtarget == null) return;
            if (Wtarget.IsInvulnerable) return;
            
            if (!(Me.Distance(Qtarget.Position) <= Q.Range) ||
                !(QDamage(Qtarget) >= Qtarget.Health + OktwCommon.GetIncomingDamage(Qtarget))) return;
            if (Q.IsReady() && ksQ) Q.Cast(Qtarget);
            
            if (!(Me.Distance(Wtarget.Position) <= W.Range) ||
                !(WDamage(Wtarget) >= Wtarget.Health + OktwCommon.GetIncomingDamage(Wtarget))) return;
            if (W.IsReady() && ksW) W.Cast(Wtarget);
        }
        
        private static readonly double[] QMultiplier = {0, .6, .75, .90, 1.05, 1.20, 1.20};
        
        private static double QDamage(AIHeroClient Qtarget)
        {
            if (Qtarget == null || !Qtarget.IsValidTarget())
            {
                return 0;
            }

            var qLevel = Me.Spellbook.GetSpell(SpellSlot.Q).Level;
            if (qLevel <= 0)
            {
                return 0;
            }

            var baseDamage = new[] {0, 95, 130, 165, 200, 235}[qLevel];
            var adDamage = new[] {0, 95, 130, 165, 200, 235}[qLevel] + QMultiplier[qLevel] +
                           Me.GetBonusPhysicalDamage();
            var qResult = Me.CalculateDamage(Qtarget, DamageType.Physical, baseDamage + adDamage);
            return qResult;
        }

        private static double WDamage(AIHeroClient Wtarget)
        {
            if (Wtarget == null || !Wtarget.IsValidTarget())
            {
                return 0;
            }

            var wLevel = Me.Spellbook.GetSpell(SpellSlot.W).Level;
            if (wLevel <= 0)
            {
                return 0;
            }

            var baseDamage = new[] {0, 75, 110, 145, 180, 215}[wLevel];
            var apDamage = new[] {0, 75, 110, 145, 180, 215}[wLevel] + 0.90 * Me.TotalMagicalDamage;
            var wResult = Me.CalculateDamage(Wtarget, DamageType.Magical, baseDamage + apDamage);
            return wResult;
        }
    }
}