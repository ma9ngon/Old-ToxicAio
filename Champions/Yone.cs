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
    public class Yone
    {
        private static Spell Q, Q3, W, E, R;
        private static Menu Config, menuQ, menuW, menuE, menuR, menuL, menuK, menuM, menuD;
        private static SpellSlot igniteSlot;
        private static AIHeroClient Me = ObjectManager.Player;
        private static bool YoneQ3;

        public static void OnGameLoad()
        {
            if (Me.CharacterName != "Yone")
            {
                return;
            }

            Q = new Spell(SpellSlot.Q, 475);
            Q.SetSkillshot(0.33f, 15f, 5000f, false, SpellType.Line);
            Q3 = new Spell(SpellSlot.Q, 950);
            Q3.SetSkillshot(0.25f, 160f, 1500f, false, SpellType.Line);
            W = new Spell(SpellSlot.W, 600f);
            W.SetSkillshot(0.46f, 0f, 500f, false, SpellType.Cone);
            E = new Spell(SpellSlot.E, 300f);
            R = new Spell(SpellSlot.R, 1000);
            R.SetSkillshot(0.75f, 255f, 1500f, false, SpellType.Line);
            
            foreach (var item in GameObjects.EnemyHeroes)
            {
                var target = item;
                ListDmg.Add(new DmgOnTarget(target.NetworkId, 0));
            }

            igniteSlot = Me.GetSpellSlot("SummonerDot");


            Config = new Menu("Yone", "[ToxicAio]: Yone", true);

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
            Config.Add(menuR);

            menuL = new Menu("Clear", "Clear settings");
            menuL.Add(new MenuBool("LhQ", "use Q to Last Hit"));
            menuL.Add(new MenuBool("LcQ", "use Q to Laneclear"));
            menuL.Add(new MenuBool("LcW", "use W to Laneclear"));
            menuL.Add(new MenuBool("JcQ", "use Q to Jungleclear"));
            menuL.Add(new MenuBool("JcW", "use W to Jungleclear"));
            Config.Add(menuL);

            menuK = new Menu("Killsteal", "Killsteal settings");
            menuK.Add(new MenuBool("KsQ", "use Q to Killsteal"));
            menuK.Add(new MenuBool("KsW", "use W to Killsteal"));
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
            AIBaseClient.OnBuffAdd += AIBaseClient_OnBuffAdd;
        }

        static int comb(Menu submenu, string sig)
        {
            return submenu[sig].GetValue<MenuList>().Index;
        }
        
        private static List<DmgOnTarget> ListDmg = new List<DmgOnTarget>();
        class DmgOnTarget

        {
            public int UID;
            public double dmg;
            public DmgOnTarget(int ID, double Dmg)
            {
                UID = ID;
                dmg = Dmg;
            }
        }
        
        private static void AIBaseClient_OnBuffAdd(AIBaseClient sender, AIBaseClientBuffAddEventArgs args)
        {
            if (args.Buff.Name != "")
                return;


            var FindinList = ListDmg.Where(i => i.UID == sender.NetworkId);
            if (FindinList.Count() >= 1)
            {
                var target = FindinList.FirstOrDefault();

                //start dmg
                target.dmg = 0;
            }
        }
        
        private static double GetEDmg(AIBaseClient target)
        {
            if (!target.HasBuff(""))
                return 0;
            var findinlist = ListDmg.Where(i => i.UID == target.NetworkId);
            if (findinlist.Count() < 1)
                return 0;

            double dmg = 0;
            var list = new double[]
            {
                0.25, 0.275, 0.3, 0.325, 0.35
            };
            var xtarget = findinlist.FirstOrDefault();
            dmg += list[E.Level] * xtarget.dmg;

            return dmg;
        }
        
        private static bool isE2()
        {
            if (ObjectManager.Player.Mana > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static void OnGameUpdate(EventArgs args)
        {

            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
            {
                LogicE();
                LogicR();
                LogicW();
                LogicQ();
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
            YoneQ33();
            Killsteal();
            skind();
        }
        
        private static void YoneQ33()
        {
            if (!YoneQ3 && ObjectManager.Player.HasBuff("yoneq3ready"))
            {
                Q.Range = 950;
                YoneQ3 = true;
            }
            else if (YoneQ3 && !ObjectManager.Player.HasBuff("yoneq3ready"))
            {
                Q.Range = 450;
                YoneQ3 = false;
            }
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

            if (W.IsReady() && W.IsInRange(wtarget) && useW.Enabled && wtarget.IsValidTarget(W.Range))
            {
                W.Cast(wtarget);
            }
        }

        private static void LogicE()
        {
            var etarget = E.GetTarget(E.Range);
            var useE = Config["Esettings"].GetValue<MenuBool>("UseE");
            if (etarget == null) return;

            if (E.IsReady() && useE.Enabled && !isE2() && etarget != null)
            {
                E.Cast(etarget.Position);
            }
        }

        private static void LogicR()
        {
            var rtarget = R.GetTarget();
            var useR = Config["Rsettings"].GetValue<MenuBool>("UseR");
            if (rtarget == null) return;

            if (R.IsReady() && useR.Enabled && R.GetDamage(rtarget) + Q.GetDamage(rtarget) * 2 + W.GetDamage(rtarget) >= rtarget.Health && rtarget.IsValidTarget())
            {
                R.Cast(rtarget);
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
                if (JcQq.Enabled && Q.IsReady() && Me.Distance(mob.Position) < Q.Range) Q.Cast(mob.Position);
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
                var minions = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(Q.Range) && x.IsMinion())
                    .Cast<AIBaseClient>().ToList();
                if (minions.Any())
                {
                    var qFarmLoaction = Q.GetLineFarmLocation(minions);
                    if (qFarmLoaction.Position.IsValid())
                    {
                        Q.Cast(qFarmLoaction.Position);
                        return;
                    }
                }
            }
        }
        
        private static void LastHit()
        {
            var allMinions = GameObjects.EnemyMinions.Where(x => x.IsMinion() && !x.IsDead)
                .OrderBy(x => x.Distance(ObjectManager.Player.Position));
            var qlh = Config["Clear"].GetValue<MenuBool>("LhQ");

            foreach (var min in allMinions.Where(x => x.IsValidTarget(Q.Range) && x.Health < Q.GetDamage(x) && qlh.Enabled))
            {
                Orbwalker.ForceTarget = min;
                Q.Cast(min);
            }
        }

        private static void Killsteal()
        {
            var ksQ = Config["Killsteal"].GetValue<MenuBool>("KsQ").Enabled;
            var ksW = Config["Killsteal"].GetValue<MenuBool>("KsW").Enabled;
            var ksR = Config["Killsteal"].GetValue<MenuBool>("KsR").Enabled;
            var Qtarget = Q.GetTarget(Q.Range);
            var Wtarget = W.GetTarget(W.Range);
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

            if (!(Me.Distance(Rtarget.Position) <= R.Range) ||
                !(RDamage(Rtarget) >= Rtarget.Health + OktwCommon.GetIncomingDamage(Rtarget))) return;
            if (R.IsReady() && ksR) R.Cast(Rtarget);
        }
        
        private static readonly float[] QBaseDamage = {0f, 20f, 40f, 60f, 80f, 100f, 100f};
        private static readonly float[] WBaseDamage = {0f, 10f, 20f, 30f, 40f, 50f, 50f};
        private static readonly float[] whealDamage = {0f, .11f, .12f, .13f, .14f, .15f, .15f};
        private static readonly float[] RBaseDamage = {0f, 200f, 400f, 600f, 600f};

        private static float QDamage(AIBaseClient Qtarget)
        {
            var qlevel = Q.Level;
            var qbaseDamage = QBaseDamage[qlevel] + 1.0f * Me.TotalAttackDamage;
            return (float) Me.CalculateDamage(Qtarget, DamageType.Physical, qbaseDamage);
        }

        private static float WDamage(AIBaseClient Wtarget)
        {
            var wlevel = W.Level;
            var wBaseDamage = WBaseDamage[wlevel] + 100 - Wtarget.MaxHealth * whealDamage[wlevel];
            return (float) Me.CalculateDamage(Wtarget, DamageType.Physical, wBaseDamage);
        }
        
        private static float RDamage(AIBaseClient Rtarget)
        {
            var rlevel = R.Level;
            var rBaseDamage = RBaseDamage[rlevel] + .80 * Me.TotalAttackDamage;
            return (float) Me.CalculateDamage(Rtarget, DamageType.Physical, rBaseDamage);
        }
        
    }
}