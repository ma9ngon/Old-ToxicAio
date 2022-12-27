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
    public class Kassadin
    {
        private static Spell Q, W, E, R;
        private static Menu Config, menuQ, menuW, menuE, menuR, menuL, menuK, menuM, menuD;
        private static SpellSlot igniteSlot;
        private static AIHeroClient Me = ObjectManager.Player;

        public static void OnGameLoad()
        {
            if (Me.CharacterName != "Kassadin")
            {
                return;
            }

            Q = new Spell(SpellSlot.Q, 650f);
            W = new Spell(SpellSlot.W, Me.GetRealAutoAttackRange());
            E = new Spell(SpellSlot.E, 600f);
            R = new Spell(SpellSlot.R, 500f);
            
            Q.SetTargetted(0.25f, 1400f );
            E.SetSkillshot(0.25f, 80f, float.MaxValue, false, SpellType.Arc);
            R.SetSkillshot(0.25f, 150f, float.MaxValue, false, SpellType.Circle);

            igniteSlot = Me.GetSpellSlot("SummonerDot");


            Config = new Menu("Kassadin", "[ToxicAio]: Kassadin", true);

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
            menuR.Add(new MenuSlider("Rstack", "Max R Stacks", 3, 0, 5));
            menuR.Add(new MenuSlider("Rtarg", "Max enemys in R range", 2, 0, 5));
            Config.Add(menuR);

            menuL = new Menu("Clear", "Clear settings");
            menuL.Add(new MenuBool("LhQ", "use Q to Last Hit"));
            menuL.Add(new MenuBool("LcE", "use W to Laneclear"));
            menuL.Add(new MenuBool("LcR", "use R to Laneclear"));
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
        }

        static int comb(Menu submenu, string sig)
        {
            return submenu[sig].GetValue<MenuList>().Index;
        }

        public static void OnGameUpdate(EventArgs args)
        {

            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
            {
                LogicQ();
                LogicR();
                LogicE();
                LogicW();
            }

            if (Orbwalker.ActiveMode == OrbwalkerMode.LaneClear)
            {
                Jungle();
                Lanceclear();
            }

            if (Orbwalker.ActiveMode == OrbwalkerMode.LastHit)
            {
                LastHit();
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

        private static int GetRstacks()
        {
            foreach (var buff in Me.Buffs)
            {
                if (buff.Name == "RiftWalk")
                    return buff.Count;
            }

            return 0;
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
            if (wtarget == null) return;

            if (W.IsReady() && useW.Enabled)
            {
                if (Orbwalker.GetTarget() != null)
                {
                    if (W.Cast())
                    {
                        Orbwalker.ResetAutoAttackTimer();
                        return;
                    }
                }
            }
        }

        private static void LogicE()
        {
            var etarget = E.GetTarget(E.Range);
            var useE = Config["Esettings"].GetValue<MenuBool>("UseE");
            if (etarget == null) return;

            if (E.IsReady() && useE.Enabled && etarget.IsValidTarget(E.Range))
            {
                E.Cast(etarget);
            }
        }

        private static void LogicR()
        {
            var rtarget = R.GetTarget();
            var useR = Config["Rsettings"].GetValue<MenuBool>("UseR");
            var countR = GetRstacks();
            var rstack = Config["Rsettings"].GetValue<MenuSlider>("Rstack");
            var rene = Config["Rsettings"].GetValue<MenuSlider>("Rtarg");

            if (R.IsReady() && useR.Enabled && countR < rstack.Value && rtarget.IsValidTarget() && Me.CountEnemyHeroesInRange(R.Range) < rene.Value)
            {
                R.Cast(rtarget);
            }
        }

        private static void Jungle()
        {
            var JcQq = Config["Clear"].GetValue<MenuBool>("JcQ");
            var JcWw = Config["Clear"].GetValue<MenuBool>("JcW");
            var JcEe = Config["Clear"].GetValue<MenuBool>("JcE");
            var mobs = GameObjects.Jungle.Where(x => x.IsValidTarget(Q.Range)).OrderBy(x => x.MaxHealth)
                .ToList<AIBaseClient>();
            if (mobs.Count > 0)
            {
                var mob = mobs[0];
                if (JcQq.Enabled && Q.IsReady() && Me.Distance(mob.Position) < Q.Range) Q.Cast(mob);
                if (JcWw.Enabled && W.IsReady() && Me.Distance(mob.Position) < W.Range) W.Cast();
                if (JcEe.Enabled && E.IsReady() && Me.Distance(mob.Position) < E.Range) E.Cast(mob.Position);
            }
        }

        private static void Lanceclear()
        {
            var lce = Config["Clear"].GetValue<MenuBool>("LcE");
            if (lce.Enabled && E.IsReady())
            {
                var minions = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(E.Range) && x.IsMinion())
                    .Cast<AIBaseClient>().ToList();
                if (minions.Any())
                {
                    var eFarmLoaction = E.GetLineFarmLocation(minions);
                    if (eFarmLoaction.Position.IsValid())
                    {
                        E.Cast(eFarmLoaction.Position);
                        return;
                    }
                }
            }
            
            var lcr = Config["Clear"].GetValue<MenuBool>("LcR");
            if (lcr.Enabled && R.IsReady())
            {
                var minions = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(R.Range) && x.IsMinion())
                    .Cast<AIBaseClient>().ToList();
                if (minions.Any())
                {
                    var rFarmLoaction = R.GetCircularFarmLocation(minions);
                    if (rFarmLoaction.Position.IsValid())
                    {
                        R.Cast(rFarmLoaction.Position);
                        return;
                    }
                }
            }
        }
        
        private static void LastHit()
        {
            if (Config["Clear"].GetValue<MenuBool>("LhQ").Enabled)
            {
                var allMinions = GameObjects.EnemyMinions.Where(x => x.IsMinion() && !x.IsDead)
                    .OrderBy(x => x.Distance(ObjectManager.Player.Position));

                foreach (var min in allMinions.Where(x => x.IsValidTarget(Q.Range) && x.Health < Q.GetDamage(x)))
                {
                    Orbwalker.ForceTarget = min;
                    Q.Cast(min);
                }
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
            
            if (Wtarget == null) return;
            if (Wtarget.IsInvulnerable) return;
            if (Rtarget == null) return;
            if (Rtarget.IsInvulnerable) return;
            
            if (!(Me.Distance(Qtarget.Position) <= Q.Range) ||
                !(QDamage(Qtarget) >= Qtarget.Health + OktwCommon.GetIncomingDamage(Qtarget))) return;
            if (Q.IsReady() && ksQ) Q.Cast(Qtarget);

            if (!(Me.Distance(Wtarget.Position) <= W.Range) ||
                !(WDamage(Wtarget) >= Wtarget.Health + OktwCommon.GetIncomingDamage(Wtarget))) return;
            if (W.IsReady() && ksW) W.Cast(Wtarget);
            
            if (!(Me.Distance(Etarget.Position) <= E.Range) ||
                !(EDamage(Etarget) >= Etarget.Health + OktwCommon.GetIncomingDamage(Etarget))) return;
            if (E.IsReady() && ksE) E.Cast(Etarget);

            if (!(Me.Distance(Rtarget.Position) <= R.Range) ||
                !(RDamage(Rtarget) >= Rtarget.Health + OktwCommon.GetIncomingDamage(Rtarget))) return;
            if (R.IsReady() && ksR) R.Cast(Rtarget);
        }
        
        private static readonly float[] QBaseDamage = {0f, 65f, 95f, 125f, 155f, 185f, 185f};
        private static readonly float[] WBaseDamage = {0f, 70f, 95f, 120f, 145f, 170f, 170f};
        private static readonly float[] EBaseDamage = {0f, 80f, 105f, 130f, 155f, 180f, 180f};
        private static readonly float[] RBaseDamage = {0f, 80f, 100f, 120f, 120f};
        private static readonly float[] Rstack = {0f, 40f, 50f, 60f, 60f};
        
        private static float QDamage(AIBaseClient Qtarget)
        {
            var qLevel = Q.Level;
            var qBaseDamage = QBaseDamage[qLevel] + .70f * Me.TotalMagicalDamage;
            return (float) Me.CalculateDamage(Qtarget, DamageType.Magical, qBaseDamage);
        }
        
        private static float WDamage(AIBaseClient Wtarget)
        {
            var wLevel = W.Level;
            var wBaseDamage = WBaseDamage[wLevel] + .80f * Me.TotalMagicalDamage;
            return (float) Me.CalculateDamage(Wtarget, DamageType.Magical, wBaseDamage);
        }
        
        private static float EDamage(AIBaseClient Etarget)
        {
            var eLevel = E.Level;
            var eBaseDamage = EBaseDamage[eLevel] + .80f * Me.TotalMagicalDamage;
            return (float) Me.CalculateDamage(Etarget, DamageType.Magical, eBaseDamage);
        }
        
        private static float RDamage(AIBaseClient Rtarget)
        {
            var rLevel = R.Level;
            var rBaseDamage = RBaseDamage[rLevel] + .40f * Me.TotalMagicalDamage + .02 * Me.MaxMana;
            var rsecond = Rstack[rLevel] * GetRstacks() + .10 * Me.TotalMagicalDamage + .01 * Me.MaxMana;
            var total = rBaseDamage + rsecond;
            return (float) Me.CalculateDamage(Rtarget, DamageType.Magical, total);
        }
        
    }
}