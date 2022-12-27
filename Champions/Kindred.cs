using System;
using System.Collections.Generic;
using System.Diagnostics;
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

namespace ToxicAio.Champions
{
    public class Kindred
    {
        private static Spell Q, W, E, R;
        private static Menu Config, menuQ, menuW, menuE, menuR, menuL, menuK, menuM, menuD;
        private static SpellSlot igniteSlot;
        private static AIHeroClient Me = ObjectManager.Player;

        public static void OnGameLoad()
        {
            if (Me.CharacterName != "Kindred")
            {
                return;
            }

            Q = new Spell(SpellSlot.Q, 350f);
            W = new Spell(SpellSlot.W, 548f);
            E = new Spell(SpellSlot.E, Me.GetRealAutoAttackRange());
            R = new Spell(SpellSlot.R, 500f);
            
            E.SetTargetted(0.25f, float.MaxValue);

            igniteSlot = Me.GetSpellSlot("SummonerDot");


            Config = new Menu("Kindred", "[ToxicAio]: Kindred", true);

            menuQ = new Menu("Qsettings", "Q settings");
            menuQ.Add(new MenuBool("UseQ", "Use Q in Combo"));
            menuQ.Add(new MenuList("QMode", "Q Mode",
                new string[] {"Target", "Mouse"}, 1));
            Config.Add(menuQ);

            menuW = new Menu("Wsettings", "W settings");
            menuW.Add(new MenuBool("UseW", "use W in Combo"));
            Config.Add(menuW);

            menuE = new Menu("Esettings", "E settings");
            menuE.Add(new MenuBool("UseE", "use E in Combo"));
            Config.Add(menuE);

            menuR = new Menu("Rsettings", "R settings");
            menuR.Add(new MenuBool("UseR", "use R in Combo"));
            menuR.Add(new MenuSlider("RHp", "Hp % To Use R", 30, 0, 100));
            menuR.Add(new MenuSlider("Rene", "Enemys in R range to Use R", 1, 1, 5));
            foreach (var ally in GameObjects.AllyHeroes)
            {
                menuR.Add(new MenuBool(ally.CharacterName.ToLower(), "use R to save " + ally.CharacterName));
            }
            Config.Add(menuR);

            menuL = new Menu("Clear", "Clear settings");
            menuL.Add(new MenuBool("LcW", "use W to Laneclear"));
            menuL.Add(new MenuBool("JcQ", "use Q to Jungleclear"));
            menuL.Add(new MenuBool("JcW", "use W to Jungleclear"));
            menuL.Add(new MenuBool("JcE", "use E to Jungleclear"));
            Config.Add(menuL);

            menuK = new Menu("Killsteal", "Killsteal settings");
            menuK.Add(new MenuBool("KsQ", "use Q to Killsteal"));
            menuK.Add(new MenuBool("KsW", "use W to Killsteal"));
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
            Orbwalker.OnBeforeAttack += Orbwalker_OnBeforeAttack;
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

            RAllyheal();
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

        private static void Orbwalker_OnBeforeAttack(object sender, BeforeAttackEventArgs args)
        {
            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
            {
                LogicE();
            }
        }

        private static void OnGapCloser(AIHeroClient sender, AntiGapcloser.GapcloserArgs args)
        {
            if (Config["Misc"].GetValue<MenuBool>("Eag").Enabled && E.IsReady())
            {
                var target = sender;
                if (target.IsValidTarget(E.Range))
                {
                    E.Cast(target);
                }
            }

            return;
        }

        private static void RAllyheal()
        {
            var useR = Config["Rsettings"].GetValue<MenuBool>("UseR").Enabled;
            var rhp = Config["Rsettings"].GetValue<MenuSlider>("RHp").Value;
            var rene = Config["Rsettings"].GetValue<MenuSlider>("Rene").Value;
            foreach (var ally in GameObjects.AllyHeroes.Where(y => y.HealthPercent < rhp && useR && y.DistanceToPlayer() < R.Range))
            {
                if (Config["Rsettings"].GetValue<MenuBool>(ally.CharacterName.ToLower()).Enabled && ally.CountEnemyHeroesInRange(R.Range) >= rene)
                {
                    R.Cast(ally);
                }
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
            var qtarget = Q.GetTarget(Me.GetRealAutoAttackRange());
            var useQ = Config["Qsettings"].GetValue<MenuBool>("UseQ");
            var input = Q.GetPrediction(qtarget);
            if (qtarget == null) return;

            switch (comb(menuQ, "QMode"))
            {
                case 0:
                    if (Q.IsReady() && useQ.Enabled && input.Hitchance >= HitChance.Low &&
                        qtarget.IsValidTarget(Me.GetRealAutoAttackRange()))
                    {
                        Q.Cast(input.CastPosition);
                    }

                    break;
                
                case 1:
                    if (Q.IsReady() && useQ.Enabled && qtarget.IsValidTarget(Me.GetRealAutoAttackRange()))
                    {
                        Q.Cast(Game.CursorPos);
                    }

                    break;
                    
            }


        }

        private static void LogicW()
        {
            var wtarget = W.GetTarget(W.Range);
            var useW = Config["Wsettings"].GetValue<MenuBool>("UseW");
            if (wtarget == null) return;

            if (W.IsReady() && useW.Enabled &&wtarget.IsValidTarget(W.Range))
            {
                W.Cast(wtarget);
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

        private static void Jungle()
        {
            var JcQq = Config["Clear"].GetValue<MenuBool>("JcQ");
            var JcWr = Config["Clear"].GetValue<MenuBool>("JcW");
            var JcEe = Config["Clear"].GetValue<MenuBool>("JcE");
            var mobs = GameObjects.Jungle.Where(x => x.IsValidTarget(E.Range)).OrderBy(x => x.MaxHealth)
                .ToList<AIBaseClient>();
            if (mobs.Count > 0)
            {
                var mob = mobs[0];
                if (JcQq.Enabled && Q.IsReady() && Me.Distance(mob.Position) < Q.Range)
                    Q.Cast(Game.CursorPos);
                if (JcEe.Enabled && E.IsReady() && Me.Distance(mob.Position) < E.Range)
                    E.Cast(mob);
                if (JcWr.Enabled && W.IsReady() && Me.Distance(mob.Position) < W.Range)
                    W.Cast(mob.Position);
            }
        }

        private static void Lane()
        {
            var lcW = Config["Clear"].GetValue<MenuBool>("LcW");
            ;

            if (lcW.Enabled && W.IsReady())
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
            var apDamage = new[] {0, 60, 85, 110, 135, 160}[qLevel] + 0.75 * Me.GetBonusPhysicalDamage();
            var qResult = Me.CalculateDamage(Qtarget, DamageType.Physical, baseDamage + apDamage);
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

            var baseDamage = new[] {0, 25, 30, 35, 40, 45}[wLevel];
            var apDamage = new[] {0, 25, 30, 35, 40, 45}[wLevel] + 0.20 * Me.GetBonusPhysicalDamage() + 0.15 + Wtarget.Health;
            var eResult = Me.CalculateDamage(Wtarget, DamageType.Magical, baseDamage + apDamage);
            return eResult;
        }
    }
}