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
    public class Khazix
    {
        private static Spell Q, W, E, R;
        private static Menu Config, menuQ, menuW, menuE, menuR, menuL, menuK, menuM, menuD, menuP;
        private static SpellSlot igniteSlot;
        private static HitChance hitchance;
        private static AIHeroClient Me = ObjectManager.Player;
        private static bool BoolEvoQ, BoolEvoE;

        public static void OnGameLoad()
        {
            if (Me.CharacterName != "Khazix")
            {
                return;
            }

            Q = new Spell(SpellSlot.Q, 325f);
            W = new Spell(SpellSlot.W, 1025f);
            E = new Spell(SpellSlot.E, 700f);
            R = new Spell(SpellSlot.R, Q.Range);

            Q.SetTargetted(0.25f, float.MaxValue);
            W.SetSkillshot(0.25f, 140f, 1700f, true, SpellType.Line);
            E.SetSkillshot(0f, 300f, float.MaxValue, false, SpellType.Circle);

            igniteSlot = Me.GetSpellSlot("SummonerDot");


            Config = new Menu("Khazix", "[ToxicAio]: Khazix", true);

            menuQ = new Menu("Qsettings", "Q settings");
            menuQ.Add(new MenuBool("UseQ", "Use Q in Combo"));
            Config.Add(menuQ);

            menuW = new Menu("Wsettings", "W settings");
            menuW.Add(new MenuBool("UseW", "use W in Combo"));
            Config.Add(menuW);

            menuE = new Menu("Esettings", "E settings");
            menuE.Add(new MenuBool("UseE", "use E in Combo"));
            Config.Add(menuE);

            menuR = new Menu("Rsettings", "R settings");
            menuR.Add(new MenuBool("UseR", "use R in Combo"));
            menuR.Add(new MenuList("Rmode", "R mode",
                new string[] {"Aggressiv", "Defensive", "Logic"}, 2));
            Config.Add(menuR);
            
            menuP = new Menu("Psettings", "Pred settings");
            menuP.Add(new MenuBool("WPred", "Enable W Prediction"));
            menuP.Add(new MenuBool("EPred", "Enable E Prediction"));
            menuP.Add(new MenuList("Pred", "Prediction hitchance",
                new string[] {"Low", "Medium", "High", " Very High"}, 2));
            Config.Add(menuP);

            menuL = new Menu("Clear", "Clear settings");
            menuL.Add(new MenuBool("LcQ", "use Q to Laneclear"));
            menuL.Add(new MenuBool("LcW", "use W to Laneclear"));
            menuL.Add(new MenuBool("JcQ", "use Q to Jungleclear"));
            menuL.Add(new MenuBool("JcW", "use W to Jungleclear"));
            Config.Add(menuL);

            menuK = new Menu("Killsteal", "Killsteal settings");
            menuK.Add(new MenuBool("KsQ", "use Q to Killsteal"));
            menuK.Add(new MenuBool("KsW", "use W to Killsteal"));
            menuK.Add(new MenuBool("KsE", "use E to Killsteal"));
            Config.Add(menuK);

            menuM = new Menu("Misc", "Misc settings");
            menuM.Add(new MenuBool("Wag", "AntiGapCloser"));
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
            AntiGapcloser.OnGapcloser += OnGapCloser;
            Drawing.OnDraw += OnDraw;
        }

        static int comb(Menu submenu, string sig)
        {
            return submenu[sig].GetValue<MenuList>().Index;
        }

        public static void OnGameUpdate(EventArgs args)
        {

            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
            {
                LogicR();
                LogicE();
                LogicQ();
                LogicW();
            }

            if (Orbwalker.ActiveMode == OrbwalkerMode.LaneClear)
            {
                Jungle();
                Lanceclear();
            }

            if (Orbwalker.ActiveMode == OrbwalkerMode.LastHit)
            {

            }

            if (Orbwalker.ActiveMode == OrbwalkerMode.Harass)
            {

            }

            Killsteal();
            Evo();
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

        private static void OnGapCloser(AIHeroClient sender, AntiGapcloser.GapcloserArgs args)
        {
            if (Config["Misc"].GetValue<MenuBool>("Wag").Enabled && Me.HasBuff("KhazixWEvo"))
            {
                var target = sender;
                if (target.IsValidTarget(W.Range))
                {
                    W.Cast(target, true);
                }
            }

            return;
        }

        private static void Evo()
        {
            if (!BoolEvoQ && Me.HasBuff("KhazixQEvo"))
            {
                Q.Range = 375f;
                BoolEvoQ = true;
            }

            if (!BoolEvoE && Me.HasBuff("KhazixEEvo"))
            {
                E.Range = 900f;
                BoolEvoE = true;
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
            if (W.IsReady())
                damage += Me.GetSpellDamage(enemy, SpellSlot.W);
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
            var etarget = E.GetTarget(E.Range);
            var useQ = Config["Qsettings"].GetValue<MenuBool>("UseQ");
            if (qtarget == null) return;

            if (Q.IsReady() && useQ.Enabled && qtarget.IsValidTarget(Q.Range) && Q.IsInRange(qtarget))
            {
                Q.Cast(qtarget);
            }
        }

        private static void LogicW()
        {
            var wtarget = W.GetTarget(W.Range);
            var useW = Config["Wsettings"].GetValue<MenuBool>("UseW");
            var Wpred = Config["Psettings"].GetValue<MenuBool>("WPred");
            var input = W.GetPrediction(wtarget);
            if (wtarget == null) return;

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

            if (W.IsReady() && W.IsInRange(wtarget) && useW.Enabled && input.Hitchance >= hitchance &&
                wtarget.IsValidTarget(W.Range) && Wpred.Enabled)
            {
                W.Cast(input.CastPosition);
            }
            else if (W.IsReady() && W.IsInRange(wtarget) && useW.Enabled &&
                     wtarget.IsValidTarget(W.Range) && !Wpred.Enabled)
            {
                W.Cast(wtarget);
            }
        }

        private static void LogicE()
        {
            var etarget = E.GetTarget(E.Range);
            var useE = Config["Esettings"].GetValue<MenuBool>("UseE");
            var Epred = Config["Psettings"].GetValue<MenuBool>("EPred");
            var input = E.GetPrediction(etarget);
            if (etarget == null) return;

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

            if (E.IsReady() && useE.Enabled && E.IsInRange(etarget) && !Q.IsInRange(etarget) &&
                input.Hitchance >= hitchance && etarget.IsValidTarget(E.Range) && Epred.Enabled)
            {
                E.Cast(input.CastPosition);
            }
            else if (E.IsReady() && useE.Enabled && Q.IsReady() && E.IsInRange(etarget) &&
                     input.Hitchance >= hitchance && etarget.IsValidTarget(E.Range) &&
                     Q.GetDamage(etarget) + E.GetDamage(etarget) >= etarget.Health && Epred.Enabled)
            {
                E.Cast(input.CastPosition);
            }
            else if (E.IsReady() && useE.Enabled && E.IsInRange(etarget) && !Q.IsInRange(etarget) &&
                     etarget.IsValidTarget(E.Range) && !Epred.Enabled)
            {
                E.Cast(etarget);
            }
            else if (E.IsReady() && useE.Enabled && Q.IsReady() && E.IsInRange(etarget) && etarget.IsValidTarget(E.Range) &&
                     Q.GetDamage(etarget) + E.GetDamage(etarget) >= etarget.Health && !Epred.Enabled)
            {
                E.Cast(etarget);
            }
        }

        private static void LogicR()
        {
            var rtarget = R.GetTarget(Q.Range);
            var useR = Config["Rsettings"].GetValue<MenuBool>("UseR");

            switch (comb(menuR, "Rmode"))
            {
                case 0:

                    if (R.IsReady() && useR.Enabled && rtarget.IsValidTarget(R.Range) &&
                        Me.CountEnemyHeroesInRange(450) >= 3)
                    {
                        R.Cast();
                    }

                    break;

                case 1:

                    if (R.IsReady() && useR.Enabled && rtarget.IsValidTarget(R.Range) &&
                        Me.CountEnemyHeroesInRange(450) >= 1 && Me.HealthPercent < 30)
                    {
                        R.Cast();
                    }

                    break;

                case 2:

                    if (R.IsReady() && useR.Enabled && rtarget.IsValidTarget(R.Range) &&
                        Me.CountEnemyHeroesInRange(450) >= 2 && Me.CountAllyHeroesInRange(450) >= 1 &&
                        Me.HealthPercent >= 50 && E.GetDamage(rtarget) + Q.GetDamage(rtarget) > rtarget.Health)
                    {
                        R.Cast();
                        E.Cast(rtarget);
                        Q.Cast(rtarget);
                    }

                    break;
            }
        }

        private static void Jungle()
        {
            var JcQq = Config["Clear"].GetValue<MenuBool>("JcQ");
            var JcWw = Config["Clear"].GetValue<MenuBool>("JcW");
            var mobs = GameObjects.Jungle.Where(x => x.IsValidTarget(Q.Range)).OrderBy(x => x.MaxHealth)
                .ToList<AIBaseClient>();
            if (mobs.Count > 0)
            {
                var mob = mobs[0];
                if (JcQq.Enabled && Q.IsReady() && Me.Distance(mob) < Q.Range) Q.Cast(mob);
                if (JcWw.Enabled && W.IsReady() && Me.Distance(mob.Position) < W.Range) W.Cast(mob.Position);
            }
        }

        private static void Lanceclear()
        {
            var lcw = Config["Clear"].GetValue<MenuBool>("LcW");
            if (lcw.Enabled && W.IsReady())
            {
                var minions = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(W.Range) && x.IsMinion())
                    .Cast<AIBaseClient>().ToList();
                if (minions.Any())
                {
                    var wFarmLoaction = W.GetLineFarmLocation(minions);
                    if (wFarmLoaction.Position.IsValid())
                    {
                        W.Cast(wFarmLoaction.Position);
                        return;
                    }
                }
            }
            
            var lcq = Config["Clear"].GetValue<MenuBool>("LcQ");
            if (lcq.Enabled && Q.IsReady())
            {
                var minions = GameObjects.EnemyMinions.FirstOrDefault(x => x.IsValidTarget(Q.Range));
                if (minions == null)
                {
                    return;
                }

                Q.CastOnUnit(minions);
            }
        }

        private static void Killsteal()
        {
            var ksQ = Config["Killsteal"].GetValue<MenuBool>("KsQ").Enabled;
            var ksW = Config["Killsteal"].GetValue<MenuBool>("KsW").Enabled;
            var ksE = Config["Killsteal"].GetValue<MenuBool>("KsE").Enabled;
            var Qtarget = Q.GetTarget(Q.Range);
            var Wtarget = W.GetTarget(W.Range);
            var Etarget = E.GetTarget(E.Range);

            if (Qtarget == null) return;
            if (Qtarget.IsInvulnerable) return;
            if (Wtarget == null) return;
            if (Wtarget.IsInvulnerable) return;
            if (Etarget == null) return;
            if (Etarget.IsInvulnerable) return;

            if (!(Me.Distance(Qtarget) <= Q.Range) ||
                !(QDamage(Qtarget)>= Qtarget.Health + OktwCommon.GetIncomingDamage(Qtarget))) return;
            if (Q.IsReady() && ksQ) Q.Cast(Qtarget);

            if (!(Me.Distance(Wtarget.Position) <= W.Range) ||
                !(WDamage(Wtarget) >= Wtarget.Health + OktwCommon.GetIncomingDamage(Wtarget))) return;
            if (W.IsReady() && ksW) W.Cast(Wtarget);

            if (!(Me.Distance(Etarget.Position) <= E.Range) ||
                !(EDamage(Etarget) >= Etarget.Health + OktwCommon.GetIncomingDamage(Etarget))) return;
            if (E.IsReady() && ksE) E.Cast(Etarget);
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

            var baseDamage = new[] {0, 60, 85, 110, 135, 160}[qLevel];
            var adDamage = new[] {0, 60, 85, 110, 135, 160}[qLevel] + 1.30 * Me.GetBonusPhysicalDamage();
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

            var baseDamage = new[] {0, 85, 115, 145, 175, 205}[wLevel];
            var adDamage = new[] {0, 85, 115, 145, 175, 205}[wLevel] + 1 * Me.GetBonusPhysicalDamage();
            var wResult = Me.CalculateDamage(Wtarget, DamageType.Physical, baseDamage + adDamage);
            return wResult;
        }
        
        private static double EDamage(AIHeroClient Etarget)
        {
            if (Etarget == null || !Etarget.IsValidTarget())
            {
                return 0;
            }

            var eLevel = Me.Spellbook.GetSpell(SpellSlot.E).Level;
            if (eLevel <= 0)
            {
                return 0;
            }

            var baseDamage = new[] {0, 65, 100, 135, 170, 205}[eLevel];
            var adDamage = new[] {0, 65, 100, 135, 170, 205}[eLevel] + 0.20 * Me.GetBonusPhysicalDamage();
            var eResult = Me.CalculateDamage(Etarget, DamageType.Physical, baseDamage + adDamage);
            return eResult;
        }
    }
}