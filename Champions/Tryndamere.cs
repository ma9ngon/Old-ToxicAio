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
    public class Tryndamere
    {
        private static Spell Q, W, E, R;
        private static Menu Config, menuQ, menuW, menuE, menuR, menuL, menuK, menuM, menuD;
        private static SpellSlot igniteSlot;
        private static AIHeroClient Me = ObjectManager.Player;

        public static void OnGameLoad()
        {
            if (Me.CharacterName != "Tryndamere")
            {
                return;
            }

            Q = new Spell(SpellSlot.Q);
            W = new Spell(SpellSlot.W, 850);
            E = new Spell(SpellSlot.E, 660);
            R = new Spell(SpellSlot.R);
            
            E.SetSkillshot(0f, 225f, float.MaxValue, false, SpellType.Line);

            igniteSlot = Me.GetSpellSlot("SummonerDot");


            Config = new Menu("Tryndamere", "[ToxicAio]: Tryndamere", true);

            menuQ = new Menu("Qsettings", "Q settings");
            menuQ.Add(new MenuBool("UseQ", "Use Q in Combo"));
            menuQ.Add(new MenuBool("Qult", "Dont use Q when R is Active"));
            menuQ.Add(new MenuSlider("QHp", "Hp % to use Q", 50, 0, 100));
            menuQ.Add(new MenuSlider("QFury", "Fury & to use Q", 50, 0, 99));
            Config.Add(menuQ);

            menuW = new Menu("Wsettings", "W settings");
            menuW.Add(new MenuBool("UseW", "use W in Combo"));
            Config.Add(menuW);

            menuE = new Menu("Esettings", "E settings");
            menuE.Add(new MenuBool("UseE", "use E in Combo"));
            menuE.Add(new MenuKeyBind("Etower", "use E under enemys tower", Keys.T, KeyBindType.Toggle)).AddPermashow();
            Config.Add(menuE);

            menuR = new Menu("Rsettings", "R settings");
            menuR.Add(new MenuBool("UseR", "use R in Combo"));
            menuR.Add(new MenuSlider("Rhp", "Hp % to use R", 30, 0, 100));
            Config.Add(menuR);

            menuL = new Menu("Clear", "Clear settings");
            menuL.Add(new MenuBool("LcE", "use Q to Laneclear"));
            menuL.Add(new MenuBool("JcE", "use E to Jungleclear"));
            Config.Add(menuL);

            menuK = new Menu("Killsteal", "Killsteal settings");
            menuK.Add(new MenuBool("KsE", "use E to Killsteal"));
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
            AIBaseClient.OnProcessSpellCast += ProcessSpell;
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
                LogicE();
                LogicQ();
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

        private static void ProcessSpell(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
        {
            if (sender == null || !sender.IsValid || args == null)
                return;

            if (!sender.IsAlly && (args.Slot >= SpellSlot.R && sender.Type == GameObjectType.AIHeroClient))
            {
                if (args.Target != null)
                {
                    if (args.Target.IsMe || args.Target.NetworkId == Me.NetworkId)
                    {
                        if (Config["Rsettings"].GetValue<MenuSlider>("Rhp").Value >= Me.HealthPercent)
                        {
                            if (R.Cast())
                            {
                                return;
                            }
                        }
                    }
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
            var useQ = Config["Qsettings"].GetValue<MenuBool>("UseQ");
            var qr = Config["Qsettings"].GetValue<MenuBool>("Qult");
            var qhpp = Config["Qsettings"].GetValue<MenuSlider>("QHp");
            var qfuryy = Config["Qsettings"].GetValue<MenuSlider>("QFury");

            if (Q.IsReady() && useQ.Enabled && Me.HealthPercent < qhpp.Value && Me.ManaPercent > qfuryy.Value && qr.Enabled && !Me.HasBuff("Undying Rage"))
            {
                Q.Cast();
            }
            else if (Q.IsReady() && useQ.Enabled && Me.HealthPercent < qhpp.Value && Me.ManaPercent > qfuryy.Value && !qr.Enabled)
            {
                Q.Cast();
            }
        }

        private static void LogicW()
        {
            var wtarget = W.GetTarget(W.Range);
            var useW = Config["Wsettings"].GetValue<MenuBool>("UseW");
            if (wtarget == null) return;

            if (W.IsReady() && useW.Enabled && wtarget.IsValidTarget(W.Range) && !wtarget.IsFacing(Me))
            {
                W.Cast(wtarget);
            }
        }

        private static void LogicE()
        {
            var etarget = E.GetTarget(E.Range);
            var useE = Config["Esettings"].GetValue<MenuBool>("UseE");
            var eturret = Config["Esettings"].GetValue<MenuKeyBind>("Etower");
            if (etarget == null) return;

            if (E.IsReady() && useE.Enabled && etarget.IsValidTarget(E.Range) &&
                Me.CountEnemyHeroesInRange(E.Range) > 0 && eturret.Active)
            {
                E.Cast(etarget);
            }
            else if (E.IsReady() && useE.Enabled && etarget.IsValidTarget(E.Range) &&
                     Me.CountEnemyHeroesInRange(E.Range) > 0 && !eturret.Active && etarget.IsUnderEnemyTurret() && EDamage(etarget) >= etarget.Health)
            {
                E.Cast(etarget);
            }
            else if (E.IsReady() && useE.Enabled && etarget.IsValidTarget(E.Range) &&
                     Me.CountEnemyHeroesInRange(E.Range) > 0 && !eturret.Active && etarget.IsUnderEnemyTurret())
            {
                return;
            }
        }
        

        private static void Jungle()
        {
            var JcEe = Config["Clear"].GetValue<MenuBool>("JcE");
            var mobs = GameObjects.Jungle.Where(x => x.IsValidTarget(Q.Range)).OrderBy(x => x.MaxHealth)
                .ToList<AIBaseClient>();
            if (mobs.Count > 0)
            {
                var mob = mobs[0];
                if (JcEe.Enabled && E.IsReady() && Me.Distance(mob) < E.Range) E.Cast(mob.Position);
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
        }

        private static void Killsteal()
        {
            var ksE = Config["Killsteal"].GetValue<MenuBool>("KsE").Enabled;
            var Etarget = E.GetTarget(E.Range);
            
            if (Etarget == null) return;
            if (Etarget.IsInvulnerable) return;

            if (!(Me.Distance(Etarget.Position) <= E.Range) ||
                !(EDamage(Etarget) >= Etarget.Health + OktwCommon.GetIncomingDamage(Etarget))) return;
            if (E.IsReady() && ksE) E.Cast(Etarget);
        }
        
        private static readonly float[] EBaseDamage = {0f, 80f, 110f, 140f, 170f, 200f, 200f};

        private static float EDamage(AIBaseClient Etarget)
        {
            var eLevel = E.Level;
            var eBaseDamage = EBaseDamage[eLevel] + 1.30f * Me.GetBonusPhysicalDamage() + 0.80f * Me.TotalMagicalDamage;
            return (float) Me.CalculateDamage(Etarget, DamageType.Physical, eBaseDamage);
        }
    }
}