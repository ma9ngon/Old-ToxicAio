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
    public class Brand
    {
        private static Spell Q, W, E, R;
        private static Menu Config, menuQ, menuW, menuE, menuR, menuL, menuK, menuM, menuD, menuP;
        private static SpellSlot igniteSlot;
        private static HitChance hitchance;
        private static AIHeroClient Me = ObjectManager.Player;

        public static void OnGameLoad()
        {
            if (Me.CharacterName != "Brand")
            {
                return;
            }

            Q = new Spell(SpellSlot.Q, 1040f);
            W = new Spell(SpellSlot.W, 900f);
            E = new Spell(SpellSlot.E, 625f);
            R = new Spell(SpellSlot.R, 750f);
            
            Q.SetSkillshot(0.25f, 120f, 1600f, true, SpellType.Line);
            W.SetSkillshot(0.25f, 260f, float.MaxValue, false, SpellType.Circle);
            E.SetTargetted(0.25f, float.MaxValue);
            R.SetTargetted(0.25f, float.MaxValue);

            igniteSlot = Me.GetSpellSlot("SummonerDot");


            Config = new Menu("Brand", "[ToxicAio]: Brand", true);

            menuQ = new Menu("Qsettings", "Q settings");
            menuQ.Add(new MenuBool("UseQ", "Use Q in Combo"));
            menuQ.Add(new MenuBool("PQ", "Use Q only if it can stun"));
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
            menuP.Add(new MenuBool("WPred", "Enable W Prediction"));
            menuP.Add(new MenuList("Pred", "Prediction hitchance",
                new string[] {"Low", "Medium", "High", " Very High"}, 2));
            Config.Add(menuP);

            menuL = new Menu("Clear", "Clear settings");
            menuL.Add(new MenuBool("LcW", "use W to LaneClear"));
            menuL.Add(new MenuBool("LcE", "use E to LaneClear"));
            menuL.Add(new MenuBool("JcQ", "use Q to Jungleclear"));
            menuL.Add(new MenuBool("JcW", "use W to Jungleclear"));
            menuL.Add(new MenuBool("JcE", "use E to Jungleclear"));
            Config.Add(menuL);

            menuK = new Menu("Killsteal", "Killsteal settings");
            menuK.Add(new MenuBool("KsQ", "use Q to Killsteal"));
            menuK.Add(new MenuBool("KsW", "use W to Killsteal"));
            menuK.Add(new MenuBool("KsE", "use E to Killsteal"));
            menuK.Add(new MenuBool("KsR", "use R to Killsteal"));
            Config.Add(menuK);

            menuM = new Menu("Misc", "Misc settings");
            menuM.Add(new MenuBool("Ag", "AntiGapCloser"));
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
            Orbwalker.OnBeforeAttack += OnBeforeAA;
            AntiGapcloser.OnGapcloser += OnGapCloser;
        }

        static int comb(Menu submenu, string sig)
        {
            return submenu[sig].GetValue<MenuList>().Index;
        }

        public static void OnGameUpdate(EventArgs args)
        {

            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
            {
                LogicW();
                LogicQ();
                LogicR();
            }

            if (Orbwalker.ActiveMode == OrbwalkerMode.LaneClear)
            {
                Laneclear();
                Jungle();
            }

            if (Orbwalker.ActiveMode == OrbwalkerMode.LastHit)
            {

            }

            if (Orbwalker.ActiveMode == OrbwalkerMode.Harass)
            {

            }
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

        private static void OnBeforeAA(object sender, BeforeAttackEventArgs args)
        {
            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
            {
                LogicE();
            }
        }

        private static void OnGapCloser(AIHeroClient sender, AntiGapcloser.GapcloserArgs args)
        {
            var target = sender;
            if (Config["Misc"].GetValue<MenuBool>("Ag").Enabled && !target.HasBuff("brandablaze") && E.IsReady())
            {
                E.Cast(sender);
            }
            else if (Config["Misc"].GetValue<MenuBool>("Ag").Enabled && !target.HasBuff("brandablaze") && !E.IsReady())
            {
                W.Cast(sender);
            }
            else if (Config["Misc"].GetValue<MenuBool>("Ag").Enabled && target.HasBuff("brandablaze"))
            {
                Q.Cast(sender);
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
            var pq = Config["Qsettings"].GetValue<MenuBool>("PQ");
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

            if (Q.IsReady() && qtarget.IsValidTarget(Q.Range) && input.Hitchance >= hitchance && useQ.Enabled && pq.Enabled && qtarget.HasBuff("brandablaze") && qpred.Enabled)
            {
                Q.Cast(input.CastPosition);
            }
            else if (Q.IsReady() && qtarget.IsValidTarget(Q.Range) && input.Hitchance >= hitchance && useQ.Enabled &&
                     !pq.Enabled && !qtarget.HasBuff("brandablaze") && qpred.Enabled)
            {
                Q.Cast(input.CastPosition);
            }
            else if ((Q.IsReady() && qtarget.IsValidTarget(Q.Range) && input.Hitchance >= hitchance && useQ.Enabled && Q.GetDamage(qtarget) > qtarget.Health) && qpred.Enabled)
            {
                Q.Cast(input.CastPosition);
            }
            else if (Q.IsReady() && qtarget.IsValidTarget(Q.Range) && useQ.Enabled && pq.Enabled && qtarget.HasBuff("brandablaze") && !qpred.Enabled)
            {
                Q.Cast(qtarget);
            }
            else if (Q.IsReady() && qtarget.IsValidTarget(Q.Range) && useQ.Enabled &&
                     !pq.Enabled && !qtarget.HasBuff("brandablaze") && !qpred.Enabled)
            {
                Q.Cast(qtarget);
            }
            else if ((Q.IsReady() && qtarget.IsValidTarget(Q.Range) && useQ.Enabled && Q.GetDamage(qtarget) > qtarget.Health) && !qpred.Enabled)
            {
                Q.Cast(qtarget);
            }
        }

        private static void LogicW()
        {
            var useW = Config["Wsettings"].GetValue<MenuBool>("UseW");
            var wtarget = W.GetTarget(W.Range);
            var wpred = Config["Psettings"].GetValue<MenuBool>("WPred");
            var input = W.GetPrediction(wtarget);
            
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

            if (W.IsReady() && wtarget.IsValidTarget(W.Range) && input.Hitchance >= hitchance && useW.Enabled && wpred.Enabled)
            {
                W.Cast(input.CastPosition);
            }
            else if (W.IsReady() && wtarget.IsValidTarget(W.Range) && useW.Enabled && !wpred.Enabled)
            {
                W.Cast(wtarget);
            }
        }

        private static void LogicE()
        {
            var useE = Config["Esettings"].GetValue<MenuBool>("UseE");
            var etarget = E.GetTarget(E.Range);

            if (E.IsReady() && etarget.IsValidTarget(E.Range) && useE.Enabled)
            {
                E.Cast(etarget);
            }
        }

        private static void LogicR()
        {
            var rtarget = R.GetTarget();
            var useR = Config["Rsettings"].GetValue<MenuBool>("UseR");
            if (rtarget == null) return;

            if (R.IsReady() && rtarget.IsValidTarget(R.Range) && Me.CountEnemyHeroesInRange(750) >= 2 && useR.Enabled)
            {
                R.Cast(rtarget);
            }
            else if (R.IsReady() && rtarget.IsValidTarget(R.Range) && Me.CountEnemyHeroesInRange(750) >= 1 &&
                     Me.HealthPercent <= 50 && rtarget.HealthPercent >= 50)
            {
                R.Cast(rtarget);
            }
        }

        private static void Jungle()
        {
            var JcQq = Config["Clear"].GetValue<MenuBool>("JcQ");
            var JcWw = Config["Clear"].GetValue<MenuBool>("JcW");
            var JcEe = Config["Clear"].GetValue<MenuBool>("JcE");
            var mobs = GameObjects.Jungle.Where(x => x.IsValidTarget(E.Range)).OrderBy(x => x.MaxHealth)
                .ToList<AIBaseClient>();
            if (mobs.Count > 0)
            {
                var mob = mobs[0];
                if (JcQq.Enabled && Q.IsReady() && Me.Distance(mob.Position) < Q.Range)
                    Q.Cast(mob.Position);
                if (JcWw.Enabled && W.IsReady() && Me.Distance(mob.Position) < W.Range)
                    W.Cast(mob.Position);
                if (JcEe.Enabled && E.IsReady() && Me.Distance(mob.Position) < E.Range)
                    E.Cast(mob);
            }
        }

        private static void Laneclear()
        {
            var lcw = Config["Clear"].GetValue<MenuBool>("LcW");
            if (lcw.Enabled && W.IsReady())
            {
                var minions = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(W.Range) && x.IsMinion())
                    .Cast<AIBaseClient>().ToList();
                if (minions.Any())
                {
                    var wFarmLocation = W.GetCircularFarmLocation(minions);
                    if (wFarmLocation.Position.IsValid())
                    {
                        W.Cast(wFarmLocation.Position);
                        return;
                    }
                }
            }
            
            var lce = Config["Clear"].GetValue<MenuBool>("LcE");
            if (lce.Enabled && E.IsReady())
            {
                var minions = GameObjects.EnemyMinions.FirstOrDefault(x => x.IsValidTarget(E.Range));
                if (minions == null)
                {
                    return;
                }

                E.CastOnUnit(minions);
            }
        }

        private static void Killsteal()
        {
            var ksQ = Config["Killsteal"].GetValue<MenuBool>("KsQ").Enabled;
            var ksW = Config["Killsteal"].GetValue<MenuBool>("KsW").Enabled;
            var ksE = Config["Killsteal"].GetValue<MenuBool>("KsE").Enabled;
            var ksR = Config["Killsteal"].GetValue<MenuBool>("KsR").Enabled;
            var Qtarget = Q.GetTarget(Q.Range);
            var Wtarget = W.GetTarget(W.Range);
            var Etarget = E.GetTarget(E.Range);
            var Rtarget = R.GetTarget(R.Range);

            if (Qtarget == null) return;
            if (Qtarget.IsInvulnerable) return;
            if (Wtarget == null) return;
            if (Wtarget.IsInvulnerable) return;
            if (Etarget == null) return;
            if (Etarget.IsInvulnerable) return;
            if (Rtarget == null) return;
            if (Rtarget.IsInvulnerable) return;

            if (!(Me.Distance(Qtarget.Position) <= Q.Range) ||
                !(QDamage(Qtarget) >= Qtarget.Health + OktwCommon.GetIncomingDamage(Qtarget))) return;
            if (Q.IsReady() && ksQ) Q.Cast(Qtarget);
            
            if (!(Me.Distance(Wtarget.Position) <= W.Range) ||
                !(WDamage(Wtarget) >= Wtarget.Health + OktwCommon.GetIncomingDamage(Wtarget))) return;
            if (W.IsReady() && ksW) W.Cast(Wtarget);
            
            if (!(Me.Distance(Etarget.Position) <= E.Range) ||
                !(EDamage(Qtarget) >= Etarget.Health + OktwCommon.GetIncomingDamage(Etarget))) return;
            if (E.IsReady() && ksE) E.Cast(Etarget);

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

            var baseDamage = new[] {0, 80, 110, 140, 170, 200}[qLevel];
            var apDamage = new[] {0, 80, 110, 140, 170, 200}[qLevel] + 0.55 * Me.TotalMagicalDamage;
            var qResult = Me.CalculateDamage(Qtarget, DamageType.Magical, baseDamage + apDamage);
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
            
            var baseDamage = new[] {0, 75, 120, 165, 210, 255}[wLevel];
            var apDamage = new[] {0, 75, 120, 165, 210, 255}[wLevel] + 0.60 * Me.TotalMagicalDamage;
            var rResult = Me.CalculateDamage(Wtarget, DamageType.Magical, baseDamage + apDamage);
            return rResult;
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
            
            var baseDamage = new[] {0, 70, 95, 120, 145, 170}[eLevel];
            var apDamage = new[] {0, 70, 95, 120, 145, 170}[eLevel] + 0.45 * Me.TotalMagicalDamage;
            var rResult = Me.CalculateDamage(Etarget, DamageType.Magical, baseDamage + apDamage);
            return rResult;
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

            var baseDamage = new[] {0, 100, 200, 300}[rLevel];
            var apDamage = new[] {0, 100, 205, 300}[rLevel] + 0.25 * Me.TotalMagicalDamage;
            var rResult = Me.CalculateDamage(Rtarget, DamageType.Magical, baseDamage + apDamage);
            return rResult;
        }
    }
}