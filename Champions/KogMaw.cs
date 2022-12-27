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
    public class KogMaw
    {
        private static Spell Q, W, E, R;
        private static Menu Config, menuQ, menuW, menuE, menuR, menuL, menuK, menuM, menuD, menuP;
        private static SpellSlot igniteSlot;
        private static HitChance hitchance;
        private static AIHeroClient Me = ObjectManager.Player;

        public static void OnGameLoad()
        {
            if (Me.CharacterName != "KogMaw")
            {
                return;
            }

            Q = new Spell(SpellSlot.Q, 1200f);
            W = new Spell(SpellSlot.W, wRange);
            E = new Spell(SpellSlot.E, 1200f);
            R = new Spell(SpellSlot.R, rRange);

            Q.SetSkillshot(0.25f, 140f, 1650f, true, SpellType.Line);
            E.SetSkillshot(0.25f, 120f, 1400f, false, SpellType.Line);
            R.SetSkillshot(1.20f, 120f, float.MaxValue, false, SpellType.Circle);

            igniteSlot = Me.GetSpellSlot("SummonerDot");


            Config = new Menu("Kogmaw", "[ToxicAio]: Kogmaw", true);

            menuQ = new Menu("Qsettings", "Q settings");
            menuQ.Add(new MenuBool("UseQ", "Use Q in Combo"));
            menuQ.Add(new MenuSlider("Qmana", "Min mana % to use Q", 30, 0, 100));
            Config.Add(menuQ);

            menuW = new Menu("Wsettings", "W settings");
            menuW.Add(new MenuBool("UseW", "use W in Combo"));
            menuW.Add(new MenuSlider("Wmana", "Min mana % to use W", 30, 0, 100));
            Config.Add(menuW);

            menuE = new Menu("Esettings", "E settings");
            menuE.Add(new MenuBool("UseE", "use E in Combo"));
            menuE.Add(new MenuSlider("Emana", "Min mana % to use E", 30, 0, 100));
            Config.Add(menuE);

            menuR = new Menu("Rsettings", "R settings");
            menuR.Add(new MenuBool("UseR", "use R in Combo"));
            menuR.Add(new MenuBool("Raa", "Use R only when target is not in aa range", true));
            menuR.Add(new MenuSlider("Rhp", "max Hp % to use R", 35, 0, 100));
            menuR.Add(new MenuSlider("Rstack", "Max R Stacks", 3, 0, 9));
            menuR.Add(new MenuSlider("Rmana", "Min mana % to use R", 30, 0, 100));
            Config.Add(menuR);
            
            menuP = new Menu("Psettings", "Pred settings");
            menuP.Add(new MenuBool("QPred", "Enable Q Prediction"));
            menuP.Add(new MenuBool("EPred", "Enable E Prediction"));
            menuP.Add(new MenuBool("RPred", "Enable R Prediction"));
            menuP.Add(new MenuList("Pred", "Prediction hitchance",
                new string[] {"Low", "Medium", "High", " Very High"}, 2));
            Config.Add(menuP);

            menuL = new Menu("Clear", "Clear settings");
            menuL.Add(new MenuBool("LcQ", "use Q to Laneclear"));
            menuL.Add(new MenuBool("LcE", "use E to Laneclear"));
            menuL.Add(new MenuBool("LcR", "use R to Laneclear"));
            menuL.Add(new MenuBool("JcQ", "use Q to Jungleclear"));
            menuL.Add(new MenuBool("JcE", "use E to Jungleclear"));
            menuL.Add(new MenuBool("JcR", "use R to Jungleclear"));
            Config.Add(menuL);

            menuK = new Menu("Killsteal", "Killsteal settings");
            menuK.Add(new MenuBool("KsQ", "use Q to Killsteal"));
            menuK.Add(new MenuBool("KsE", "use E to Killsteal"));
            menuK.Add(new MenuBool("KsR", "use R to Killsteal"));
            Config.Add(menuK);

            menuM = new Menu("Misc", "Misc settings");
            menuM.Add(new MenuBool("Eag", "AntiGapCloser"));
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
            AntiGapcloser.OnGapcloser += OnGapCloser;
        }

        static int comb(Menu submenu, string sig)
        {
            return submenu[sig].GetValue<MenuList>().Index;
        }

        private static float wRange => 500 +
                                       new[] {0, 130, 150, 170, 190, 210}[
                                           Me.Spellbook.GetSpell(SpellSlot.W).Level] +
                                       Me.BoundingRadius;

        private static float rRange =>
            new[] {1300, 1300, 1550, 1800}[Me.Spellbook.GetSpell(SpellSlot.R).Level];

        public static void OnGameUpdate(EventArgs args)
        {

            if (W.Level > 0)
            {
                W.Range = wRange;
            }

            if (R.Level > 0)
            {
                R.Range = rRange;
            }

            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
            {
                LogicW();
                LogicR();
                LogicQ();
                LogicE();
            }

            if (Orbwalker.ActiveMode == OrbwalkerMode.LaneClear)
            {
                Jungle();
                Lane();
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

        private static void OnGapCloser(AIHeroClient sender, AntiGapcloser.GapcloserArgs args)
        {
            if (Config["Misc"].GetValue<MenuBool>("Eag").Enabled && E.IsReady())
            {
                var target = sender;
                if (target.IsValidTarget(E.Range))
                {
                    E.Cast(target, true);
                }
            }

            return;
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
            var qma = Config["Qsettings"].GetValue<MenuSlider>("Qmana").Value;
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

            if (Q.IsReady() && Q.IsInRange(input.CastPosition) && useQ.Enabled &&
                Me.ManaPercent > qma && input.Hitchance >= hitchance &&
                qtarget.IsValidTarget(Q.Range) && qpred.Enabled)
            {
                Q.Cast(input.CastPosition);
            }
            else if (Q.IsReady() && Q.IsInRange(input.CastPosition) && useQ.Enabled &&
                     Me.ManaPercent > qma && qtarget.IsValidTarget(Q.Range) && !qpred.Enabled)
            {
                Q.Cast(qtarget);
            }
        }

        private static void LogicW()
        {
            var wtarget = W.GetTarget(W.Range);
            var useW = Config["Wsettings"].GetValue<MenuBool>("UseW");
            var wma = Config["Wsettings"].GetValue<MenuSlider>("Wmana").Value;
            if (wtarget == null) return;

            if (W.IsReady() && useW.Enabled && Me.ManaPercent > wma && wtarget.IsValidTarget(W.Range))
            {
                W.Cast();
            }
        }

        private static void LogicE()
        {
            var etarget = E.GetTarget(E.Range);
            var useE = Config["Esettings"].GetValue<MenuBool>("UseE");
            var ema = Config["Esettings"].GetValue<MenuSlider>("Emana").Value;
            var epred = Config["Psettings"].GetValue<MenuBool>("EPred");
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

            if (E.IsReady() && useE.Enabled && E.IsInRange(input.CastPosition) && input.Hitchance >= hitchance &&
                Me.ManaPercent > ema && etarget.IsValidTarget(E.Range) && epred.Enabled)
            {
                E.Cast(input.CastPosition);
            }
            else if (E.IsReady() && useE.Enabled && E.IsInRange(input.CastPosition) &&
                     Me.ManaPercent > ema && etarget.IsValidTarget(E.Range) && !epred.Enabled)
            {
                E.Cast(etarget);
            }
        }

        private static void LogicR()
        {
            var rtarget = R.GetTarget();
            var useR = Config["Rsettings"].GetValue<MenuBool>("UseR");
            var raa = Config["Rsettings"].GetValue<MenuBool>("Raa");
            var rst = Config["Rsettings"].GetValue<MenuSlider>("Rstack").Value;
            var rhp = Config["Rsettings"].GetValue<MenuSlider>("Rhp").Value;
            var rma = Config["Rsettings"].GetValue<MenuSlider>("Rmana").Value;
            var rpred = Config["Psettings"].GetValue<MenuBool>("RPred");
            var countR = GetRstacks();
            var input = R.GetPrediction(rtarget);
            if (rtarget == null) return;

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

            if (R.IsReady() && useR.Enabled && R.IsInRange(input.CastPosition) && countR < rst && !raa.Enabled &&
                Me.ManaPercent > rma && rtarget.HealthPercent < rhp && input.Hitchance >= hitchance &&
                rtarget.IsValidTarget(R.Range) && rpred.Enabled)
            {
                R.Cast(input.CastPosition);
            }
            else if (R.IsReady() && useR.Enabled && raa.Enabled &&
                     rtarget.HealthPercent < rhp &&
                     input.Hitchance >= hitchance && R.IsInRange(input.CastPosition) &&
                     Me.ManaPercent > rma && countR < rst && rtarget.IsValidTarget(R.Range) && rpred.Enabled)
            {
                if (!rtarget.InAutoAttackRange())
                {
                    R.Cast(input.CastPosition);
                }
            }
            else if (R.IsReady() && useR.Enabled && R.IsInRange(input.CastPosition) && countR < rst && !raa.Enabled &&
                     Me.ManaPercent > rma && rtarget.HealthPercent < rhp &&
                     rtarget.IsValidTarget(R.Range) && !rpred.Enabled)
            {
                R.Cast(rtarget);
            }
            else if (R.IsReady() && useR.Enabled && raa.Enabled &&
                     rtarget.HealthPercent < rhp &&
                     R.IsInRange(input.CastPosition) &&
                     Me.ManaPercent > rma && countR < rst && rtarget.IsValidTarget(R.Range) && !rpred.Enabled)
            {
                if (!rtarget.InAutoAttackRange())
                {
                    R.Cast(rtarget);
                }
            }
        }

        private static void Jungle()
        {
            var JcQq = Config["Clear"].GetValue<MenuBool>("JcQ");
            var JcEe = Config["Clear"].GetValue<MenuBool>("JcE");
            var JcRr = Config["Clear"].GetValue<MenuBool>("JcR");
            var mobs = GameObjects.Jungle.Where(x => x.IsValidTarget(E.Range)).OrderBy(x => x.MaxHealth)
                .ToList<AIBaseClient>();
            if (mobs.Count > 0)
            {
                var mob = mobs[0];
                if (JcQq.Enabled && Q.IsReady() && Me.Distance(mob.Position) < Q.Range)
                    Q.Cast(mob.Position);
                if (JcEe.Enabled && E.IsReady() && Me.Distance(mob.Position) < E.Range)
                    E.Cast(mob.Position);
                if (JcRr.Enabled && R.IsReady() && Me.Distance(mob.Position) < R.Range)
                    R.Cast(mob.Position);
            }
        }

        private static void Lane()
        {
            var lcQ = Config["Clear"].GetValue<MenuBool>("LcQ");
            var lcE = Config["Clear"].GetValue<MenuBool>("LcE");
            var lcR = Config["Clear"].GetValue<MenuBool>("LcR");

            if (lcQ.Enabled && Q.IsReady())
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

            if (lcE.Enabled && E.IsReady())
            {
                var minions = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(E.Range) && x.IsMinion())
                    .Cast<AIBaseClient>().ToList();
                if (minions.Any())
                {
                    var eFarmLocation = E.GetLineFarmLocation(minions);
                    if (eFarmLocation.Position.IsValid())
                    {
                        E.Cast(eFarmLocation.Position);
                        return;
                    }
                }
            }

            if (lcR.Enabled && R.IsReady())
            {
                var minions = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(R.Range) && x.IsMinion())
                    .Cast<AIBaseClient>().ToList();
                if (minions.Any())
                {
                    var rFarmLocation = R.GetCircularFarmLocation(minions);
                    if (rFarmLocation.Position.IsValid())
                    {
                        R.Cast(rFarmLocation.Position);
                        return;
                    }
                }
            }
        }

        private static void Killsteal()
        {
            var ksQ = Config["Killsteal"].GetValue<MenuBool>("KsQ").Enabled;
            var ksE = Config["Killsteal"].GetValue<MenuBool>("KsE").Enabled;
            var ksR = Config["Killsteal"].GetValue<MenuBool>("KsR").Enabled;
            var Qtarget = Q.GetTarget(Q.Range);
            var Etarget = E.GetTarget(E.Range);
            var Rtarget = R.GetTarget(R.Range);

            if (Qtarget == null) return;
            if (Qtarget.IsInvulnerable) return;
            if (Etarget == null) return;
            if (Etarget.IsInvulnerable) return;
            if (Rtarget == null) return;
            if (Rtarget.IsInvulnerable) return;

            if (!(Me.Distance(Qtarget.Position) <= Q.Range) ||
                !(QDamage(Qtarget) >= Qtarget.Health + OktwCommon.GetIncomingDamage(Qtarget))) return;
            if (Q.IsReady() && ksQ) Q.Cast(Qtarget);

            if (!(Me.Distance(Etarget.Position) <= E.Range) ||
                !(EDamage(Etarget) >= Etarget.Health + OktwCommon.GetIncomingDamage(Etarget))) return;
            if (E.IsReady() && ksE) E.Cast(Etarget);
            
            if (!(Me.Distance(Rtarget.Position) <= R.Range) ||
                !(RDamage(Rtarget) >= Rtarget.Health + OktwCommon.GetIncomingDamage(Rtarget))) return;
            if (R.IsReady() && ksR) R.Cast(Rtarget);
        }

        private static int GetRstacks()
        {
            foreach (var buff in Me.Buffs)
            {
                if (buff.Name == "kogmawlivingartillerycost")
                    return buff.Count;
            }

            return 0;
        }
        
        private static readonly float[] QBaseDamage = {0f, 90f, 140f, 190f, 240f, 290f, 290f};
        private static readonly float[] EBaseDamage = {0f, 75f, 120f, 165f, 210f, 255f, 255f};
        private static readonly float[] RBaseDamage = {0f, 100, 140f, 180f, 180f,};
        
        private static double QDamage(AIHeroClient Qtarget)
        {
            var qLevel = Q.Level;
            var QBasedamage = QBaseDamage[qLevel] + 0.70 * Me.TotalMagicalDamage;
            return (float) GameObjects.Player.CalculateDamage(Qtarget, DamageType.Magical, QBasedamage);
        }
        
        private static double EDamage(AIHeroClient Etarget)
        {
            var eLevel = E.Level;
            var EBasedamage = EBaseDamage[eLevel] + 0.50 * Me.TotalMagicalDamage;
            return (float) GameObjects.Player.CalculateDamage(Etarget, DamageType.Magical, EBasedamage);
        }
        
        private static double RDamage(AIHeroClient Rtarget)
        {
            var rLevel = R.Level;
            var RBasedamage = RBaseDamage[rLevel] + 0.65 * Me.GetBonusPhysicalDamage() + 0.35 * Me.TotalMagicalDamage;
            return (float) GameObjects.Player.CalculateDamage(Rtarget, DamageType.Magical, RBasedamage);
        }
    }
}