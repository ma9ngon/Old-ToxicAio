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
using System.Text.RegularExpressions;
using SebbyLib;

namespace ToxicAio.Champions
{
    public class Blitzcrank
    {
        private static Spell Q, W, E, R;
        private static Menu Config, menuQ, menuW, menuE, menuR, menuL, menuK, menuM, menuD, menuP;
        private static SpellSlot igniteSlot;
        private static HitChance hitchance;
        private static AIHeroClient Me = ObjectManager.Player;

        public static void OnGameLoad()
        {
            if (Me.CharacterName != "Blitzcrank")
            {
                return;
            }

            Q = new Spell(SpellSlot.Q, 1115f);
            W = new Spell(SpellSlot.W, Me.GetRealAutoAttackRange());
            E = new Spell(SpellSlot.E, Me.GetRealAutoAttackRange());
            R = new Spell(SpellSlot.R, 600f);

            Q.SetSkillshot(0.25f, 140f, 1800f, true, SpellType.Line);

            igniteSlot = Me.GetSpellSlot("SummonerDot");


            Config = new Menu("Blitzcrank", "[ToxicAio]: Blitzcrank", true);

            menuQ = new Menu("Qsettings", "Q settings");
            menuQ.Add(new MenuBool("UseQ", "Use Q in Combo"));
            menuQ.Add(new MenuBool("AQ", "Auto Q on Dash/CC"));
            Config.Add(menuQ);
            
            menuW = new Menu("Wsettings", "W settings");
            menuW.Add(new MenuBool("UseW", "use W in Combo"));
            Config.Add(menuW);

            menuE = new Menu("Esettings", "E settings");
            menuE.Add(new MenuBool("UseE", "use E in Combo"));
            Config.Add(menuE);

            menuR = new Menu("Rsettings", "R settings");
            menuR.Add(new MenuBool("UseR", "use R in Combo"));
            Config.Add(menuR);
            
            menuP = new Menu("Psettings", "Pred settings");
            menuP.Add(new MenuBool("QPred", "Enable Q Prediction"));
            menuP.Add(new MenuList("Pred", "Prediction hitchance",
                new string[] {"Low", "Medium", "High", " Very High"}, 2));
            Config.Add(menuP);

            menuL = new Menu("Clear", "Clear settings");
            menuL.Add(new MenuBool("JcQ", "use Q to Jungleclear"));
            menuL.Add(new MenuBool("JcE", "use E to Jungleclear"));
            Config.Add(menuL);

            menuK = new Menu("Killsteal", "Killsteal settings");
            menuK.Add(new MenuBool("KsQ", "use Q to Killsteal"));
            menuK.Add(new MenuBool("KsR", "use R to Killsteal"));
            Config.Add(menuK);

            menuM = new Menu("Misc", "Misc settings");
            menuM.Add(new MenuBool("Rint", "use R to Interrup"));
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
            Interrupter.OnInterrupterSpell += Interrupterr;
        }

        static int comb(Menu submenu, string sig)
        {
            return submenu[sig].GetValue<MenuList>().Index;
        }

        private static void AutoQ()
        {
            var Qtarg = Q.GetTarget(Q.Range);
            var aq = Config["Qsettings"].GetValue<MenuBool>("AQ");
            var pred = Q.GetPrediction(Qtarg);
            if (Qtarg == null) return;
            
            if (!Q.IsReady() || !Q.IsInRange(Qtarg) || !aq.Enabled || !Qtarg.IsValidTarget(Q.Range)) return;
            if (Qtarg.HasBuffOfType(BuffType.Stun) ||
                Qtarg.HasBuffOfType(BuffType.Snare) ||
                Qtarg.HasBuffOfType(BuffType.Knockup) ||
                Qtarg.HasBuffOfType(BuffType.Suppression) ||
                Qtarg.HasBuffOfType(BuffType.Charm) ||
                Qtarg.IsRecalling())
            {
                Q.Cast(Qtarg);
            }
            else if (pred.Hitchance >= HitChance.Dash)
            {
                Q.Cast(pred.CastPosition);
            }
        }

        private static void Interrupterr(AIHeroClient sender, Interrupter.InterruptSpellArgs args)
        {
            if (Config["Misc"].GetValue<MenuBool>("Rint").Enabled && R.IsReady() && sender.IsValidTarget(R.Range))
            {
                R.Cast();
            }
        }

        public static void OnGameUpdate(EventArgs args)
        {

            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
            {
                LogicW();
                LogicQ();
                LogicR();
                LogicE();
            }

            if (Orbwalker.ActiveMode == OrbwalkerMode.LaneClear)
            {
                Jungle();
            }

            if (Orbwalker.ActiveMode == OrbwalkerMode.LastHit)
            {

            }

            if (Orbwalker.ActiveMode == OrbwalkerMode.Harass)
            {

            }
            AutoQ();
            skind();
            Killsteal();
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
            if (igniteSlot != SpellSlot.Unknown &&
                Me.Spellbook.CanUseSpell(igniteSlot) == SpellState.Ready)
                damage += Me.GetSummonerSpellDamage(enemy, SummonerSpell.Ignite);
            if (Q.IsReady())
                damage += Me.GetSpellDamage(enemy, SpellSlot.Q);
            if (E.IsReady())
                damage += Me.GetSpellDamage(enemy, SpellSlot.E);
            if (R.IsReady())
                damage += Me.GetSpellDamage(enemy, SpellSlot.R);

            return (float) damage;
        }

        private static void OnDraw(EventArgs args)
        {
            /*if (Config["Draw"].GetValue<MenuBool>("drawQ").Enabled)
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
            }*/

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

        private static void LogicQ()
        {
            var qtarget = Q.GetTarget(Q.Range);
            var useQ = Config["Qsettings"].GetValue<MenuBool>("UseQ");
            var qpred = Config["Psettings"].GetValue<MenuBool>("QPred");
            var input = Q.GetPrediction(qtarget);
            if (qtarget == null) return;

            switch (comb(menuP, "Pred"))
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

            if (Q.IsReady() && qtarget.IsValidTarget(Q.Range) && input.Hitchance >= hitchance && useQ.Enabled && qpred.Enabled)
            {
                Q.Cast(input.CastPosition);
            }
            else if (Q.IsReady() && qtarget.IsValidTarget(Q.Range) && useQ.Enabled && !qpred.Enabled)
            {
                Q.Cast(qtarget);
            }
        }

        private static void LogicW()
        {
            var useW = Config["Wsettings"].GetValue<MenuBool>("UseW");

            if (useW.Enabled && W.IsReady())
            {
                if (Orbwalker.GetTarget() != null)
                {
                    if (W.Cast())
                    {
                        return;
                    }
                }
            }
        }

        private static void LogicE()
        {
            var useE = Config["Esettings"].GetValue<MenuBool>("UseE");

            if (E.IsReady() && useE.Enabled)
            {
                if (Orbwalker.GetTarget() != null)
                {
                    if (E.Cast())
                    {
                        Orbwalker.ResetAutoAttackTimer();
                        return;
                    }
                }
            }
        }

        private static void LogicR()
        {
            var rtarget = R.GetTarget();
            var useR = Config["Rsettings"].GetValue<MenuBool>("UseR");
            if (rtarget == null) return;

            if (R.IsReady() && rtarget.IsValidTarget(R.Range) && R.GetDamage(rtarget) > rtarget.Health && useR.Enabled)
            {
                R.Cast();
            }
        }

        private static void Jungle()
        {
            var JcQq = Config["Clear"].GetValue<MenuBool>("JcQ");
            var JcEe = Config["Clear"].GetValue<MenuBool>("JcE");
            var mobs = GameObjects.Jungle.Where(x => x.IsValidTarget(E.Range)).OrderBy(x => x.MaxHealth)
                .ToList<AIBaseClient>();
            if (mobs.Count > 0)
            {
                var mob = mobs[0];
                if (JcQq.Enabled && Q.IsReady() && Me.Distance(mob.Position) < Q.Range)
                    Q.Cast(mob.Position);
                if (JcEe.Enabled && E.IsReady() && Me.Distance(mob.Position) < E.Range)
                    E.Cast();
            }
        }

        private static void Killsteal()
        {
            var ksQ = Config["Killsteal"].GetValue<MenuBool>("KsQ").Enabled;
            var ksR = Config["Killsteal"].GetValue<MenuBool>("KsR").Enabled;
            var Qtarget = Q.GetTarget(Q.Range);
            var Rtarget = R.GetTarget(R.Range);

            if (Qtarget == null) return;
            if (Qtarget.IsInvulnerable) return;
            if (Rtarget == null) return;
            if (Rtarget.IsInvulnerable) return;

            if (!(Me.Distance(Qtarget.Position) <= Q.Range) ||
                !(QDamage(Qtarget) >= Qtarget.Health + OktwCommon.GetIncomingDamage(Qtarget))) return;
            if (Q.IsReady() && ksQ) Q.Cast(Qtarget);

            if (!(Me.Distance(Rtarget.Position) <= R.Range) ||
                !(RDamage(Rtarget) >= Rtarget.Health + OktwCommon.GetIncomingDamage(Rtarget))) return;
            if (R.IsReady() && ksR) R.Cast(Rtarget);
        }

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

            var baseDamage = new[] {0, 70, 120, 170, 220, 270}[qLevel];
            var apDamage = new[] {0, 70, 120, 170, 220, 270}[qLevel] + 1 * Me.TotalMagicalDamage;
            var qResult = Me.CalculateDamage(Qtarget, DamageType.Magical, baseDamage + apDamage);
            return qResult;
        }

        private static double RDamage(AIHeroClient Rtarget)
        {
            if (Rtarget == null || !Rtarget.IsValidTarget())
            {
                return 0;
            }

            var rLevel = Me.Spellbook.GetSpell(SpellSlot.R).Level;
            if (rLevel <= 0)
            {
                return 0;
            }

            var baseDamage = new[] {0, 250, 375, 500}[rLevel];
            var apDamage = new[] {0, 250, 375, 500}[rLevel] + 1 * Me.TotalMagicalDamage;
            var rResult = Me.CalculateDamage(Rtarget, DamageType.Magical, baseDamage + apDamage);
            return rResult;
        }
    }
}