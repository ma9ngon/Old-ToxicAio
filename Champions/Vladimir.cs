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
using System.Text.RegularExpressions;
using SebbyLib;

namespace ToxicAio.Champions
{
    public class Vladimir
    {
        private static Spell Q, W, E, R;
        private static Menu Config, menuQ, menuW, menuE, menuR, menuL, menuK, menuM, menuD;
        private static SpellSlot igniteSlot;
        private static AIHeroClient Me = ObjectManager.Player;

        public static void OnGameLoad()
        {
            if (Me.CharacterName != "Vladimir")
            {
                return;
            }

            Q = new Spell(SpellSlot.Q, 600f);
            W = new Spell(SpellSlot.W, 350f);
            E = new Spell(SpellSlot.E, 600f);
            R = new Spell(SpellSlot.R, 625f);
            
            Q.SetTargetted(0.25f, float.MaxValue);
            R.SetSkillshot(0.25f, 375f, float.MaxValue, false, SpellType.Circle);

            igniteSlot = Me.GetSpellSlot("SummonerDot");


            Config = new Menu("Vladimir", "[ToxicAio]: Vladimir", true);

            menuQ = new Menu("Qsettings", "Q settings");
            menuQ.Add(new MenuBool("UseQ", "Use Q in Combo"));
            Config.Add(menuQ);

            menuW = new Menu("Wsettings", "W settings");
            menuW.Add(new MenuBool("WDoge", "Use W to doge Spells/AA when low"));
            menuW.Add(new MenuSlider("WDogeHP", "Hp % to W doge Spells", 25, 0, 100));

            foreach (var ene in ObjectManager.Get<AIHeroClient>().Where(x => x.Team != Me.Team))
            {
                foreach (var lib in KurisuLib.CCList.Where(x => x.HeroName == ene.CharacterName))
                {
                    menuW.Add(new MenuBool(lib.SDataName, lib.SpellMenuName + " from " + ene.CharacterName));
                }
            }
            Config.Add(menuW);

            menuE = new Menu("Esettings", "E settings");
            menuE.Add(new MenuBool("UseE", "use E in Combo"));
            menuE.Add(new MenuList("EMode", "E Mode",
                new string[] {"Fast", "Slow (Fully Charge E To Slow the Enemy)"}, 1));
            Config.Add(menuE);

            menuR = new Menu("Rsettings", "R settings");
            menuR.Add(new MenuBool("UseR", "use R in Combo"));
            menuR.Add(new MenuSlider("Renemy", "Min Targets to cast R", 2, 1, 5));
            menuR.Add(new MenuSlider("RHp", "Hp % to use R", 50, 0, 100));
            menuR.Add(new MenuKeyBind("SemiR", "Semi R", Keys.G, KeyBindType.Press));
            Config.Add(menuR);

            menuL = new Menu("Clear", "Clear settings");
            menuL.Add(new MenuBool("LsQ", "use Q to LastHit"));
            menuL.Add(new MenuBool("LcE", "use E to Laneclear"));
            menuL.Add(new MenuBool("JcQ", "use Q to Jungleclear"));
            menuL.Add(new MenuBool("JcE", "use E to Jungleclear"));
            Config.Add(menuL);

            menuK = new Menu("Killsteal", "Killsteal settings");
            menuK.Add(new MenuBool("KsQ", "use Q to Killsteal"));
            menuK.Add(new MenuBool("KsE", "use E to Killsteal"));
            Config.Add(menuK);

            menuM = new Menu("Misc", "Misc settings");
            menuM.Add(new MenuSliderButton("Skin", "SkindID", 0, 0, 30, false));
            menuM.Add(new MenuKeyBind("aa", "Disable Auto Attacks", Keys.T, KeyBindType.Toggle)).AddPermashow();
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
            AIBaseClient.OnProcessSpellCast += OnProcessSpellCastt;
            AIBaseClient.OnProcessSpellCast += ProcessSpell;
            Orbwalker.OnBeforeAttack += OnBeforeAA;
        }

        static int comb(Menu submenu, string sig)
        {
            return submenu[sig].GetValue<MenuList>().Index;
        }

        public static void OnGameUpdate(EventArgs args)
        {

            if (Me.HasBuff("VladimirE"))
            {
                Me.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
            }

            if (Me.HasBuff("VladimirSanguinePool"))
            {
                Me.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
            }

            if (Config["Rsettings"].GetValue<MenuKeyBind>("SemiR").Active)
            {
                semiR();
            }

            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
            {
                LogicR();
                LogicW();
                LogicQ();
                LogicE();
            }

            if (Orbwalker.ActiveMode == OrbwalkerMode.LaneClear)
            {
                LastHit();
                Lanceclear();
                Jungle();
            }

            if (Orbwalker.ActiveMode == OrbwalkerMode.LastHit)
            {
                LastHit();
            }

            if (Orbwalker.ActiveMode == OrbwalkerMode.Harass)
            {
                LogicQ();
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

        private static void OnBeforeAA(object sender, BeforeAttackEventArgs args)
        {
            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo && Config["Misc"].GetValue<MenuKeyBind>("aa").Active)
            {
                args.Process = false;
            }
        }

        private static float lastE = 0;

        private static void OnProcessSpellCastt(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
        {
            if (sender.IsMe && args.Slot == SpellSlot.E)
            {
                lastE = Variables.GameTimeTickCount;
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
                        if (Config["Wsettings"].GetValue<MenuBool>("WDoge").Enabled &&
                            Config["Wsettings"].GetValue<MenuSlider>("WDogeHP").Value >= Me.HealthPercent)
                        {
                            if (W.Cast())
                            {
                                return;
                            }
                        }
                    }
                }
            }
        }

        private static void semiR()
        {
            var target = R.GetTarget(R.Range);
            if (target == null) return;

            if (R.IsReady() && target.IsValidTarget(R.Range))
            {
                R.Cast(target);
            }
        }

        private static void OnProcessSpellCast(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
        {
            if (sender.Type != Me.Type || !W.IsReady() || !sender.IsEnemy)
                return;

            var attacker = ObjectManager.Get<AIHeroClient>().First(x => x.NetworkId == sender.NetworkId);
            foreach (var ally in GameObjects.AllyHeroes.Where(x => x.IsValidTarget(W.Range, false)))
            {
                var detectRange = ally.Position + (args.End - ally.Position).Normalized() * ally.Distance(args.End);
                if (detectRange.Distance(ally.Position) > ally.AttackRange - ally.BoundingRadius)
                    continue;

                foreach (var lib in KurisuLib.CCList.Where(x => x.HeroName == attacker.CharacterName && x.Slot == attacker.GetSpellSlot(args.SData.Name)))
                {
                    if (lib.Type == Skilltype.Unit && args.Target.NetworkId != ally.NetworkId)
                        return;
                    try
                    {
                        if (Config["Wsettings"].GetValue<MenuBool>(lib.SDataName).Enabled)
                        {
                            Console.WriteLine(Config["Wsettings"].GetValue<MenuBool>(lib.SDataName).Enabled.ToString());
                            Console.WriteLine(lib.SDataName);
                            W.Cast();
                        }
                    }
                    catch { }
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
            var qtarget = Q.GetTarget(Q.Range);
            var useQ = Config["Qsettings"].GetValue<MenuBool>("UseQ");
            if (qtarget == null) return;

            if (Q.IsReady() && useQ.Enabled && qtarget.IsValidTarget(Q.Range) && !Me.HasBuff("VladimirE"))
            {
                Q.Cast(qtarget);
            }
        }

        private static void LogicE()
        {
            var etarget = E.GetTarget(E.Range);
            var useE = Config["Esettings"].GetValue<MenuBool>("UseE");
            if (etarget == null) return;

            switch (comb(menuE, "EMode"))
            {
                case 0:
                    if (E.IsReady() && useE.Enabled && Variables.GameTimeTickCount - lastE > 1000 && etarget.IsValidTarget(E.Range))
                    {
                        if (!E.IsCharging)
                        {
                            E.StartCharging();
                        }

                        if (E.IsChargedSpell && etarget.IsValidTarget(E.Range))
                        {
                            E.Cast();
                        }
                    }

                    break;
                
                case 1:
                    if (E.IsReady() && useE.Enabled && Variables.GameTimeTickCount - lastE > 1600 && etarget.IsValidTarget(E.Range))
                    {
                        if (!E.IsCharging && etarget.IsValidTarget(E.Range))
                        {
                            E.StartCharging();
                        }

                        if (E.IsChargedSpell)
                        {
                            E.Cast();
                        }
                    }

                    break;
            }
        }

        private static void LogicW()
        {
            var wtarget = W.GetTarget(W.Range);
            if (wtarget == null) return;

            if (W.IsReady() && wtarget.IsValidTarget(W.Range) && W.IsInRange(wtarget) &&
                W.GetDamage(wtarget) + Q.GetDamage(wtarget) + E.GetDamage(wtarget) >= wtarget.Health)
            {
                W.Cast();
            }
        }

        private static void LogicR()
        {
            var target = TargetSelector.GetTarget(R.Range, DamageType.Magical);
            var useR = Config["Rsettings"].GetValue<MenuBool>("UseR");
            var countR = Config["Rsettings"].GetValue<MenuSlider>("Renemy");
            var Rhp = Config["Rsettings"].GetValue<MenuSlider>("RHp");
            if (target == null) return;

            if (R.IsReady() && useR.Enabled && GetHitByR(target) >= countR.Value && target.IsValidTarget(R.Range))
            {
                R.Cast(target);
            }
            else if (R.IsReady() && useR.Enabled && target.IsValidTarget(R.Range) && target.HealthPercent <= Rhp.Value)
            {
                R.Cast(target);
            }
        }

        private static int GetHitByR(AIBaseClient target) // Credits to Trelli For helping me with this one!
        {
            int totalHit = 0;
            foreach (AIHeroClient current in ObjectManager.Get<AIHeroClient>())
            {
                if (current.IsEnemy && Vector3.Distance(Me.Position, current.Position) <= R.Range)
                {
                    totalHit = totalHit + 1;
                }
            }
            return totalHit;
        }

        private static void Jungle()
        {
            var JcQq = Config["Clear"].GetValue<MenuBool>("JcQ");
            var JcEe = Config["Clear"].GetValue<MenuBool>("JcE");
            var mobs = GameObjects.Jungle.Where(x => x.IsValidTarget(Q.Range)).OrderBy(x => x.MaxHealth)
                .ToList<AIBaseClient>();
            if (mobs.Count > 0)
            {
                var mob = mobs[0];
                if (JcQq.Enabled && Q.IsReady() && Me.Distance(mob.Position) < Q.Range) Q.Cast(mob);
                if (JcEe.Enabled && E.IsReady() && Variables.GameTimeTickCount - lastE > 1000 && Me.Distance(mob.Position) < E.Range) E.Cast(mob);
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
                    var eFarmLoaction = E.GetCircularFarmLocation(minions);
                    if (eFarmLoaction.Position.IsValid() && Variables.GameTimeTickCount - lastE > 1000)
                    {
                        E.Cast(eFarmLoaction.Position);
                        return;
                    }
                }
            }
        }
        
        private static void LastHit()
        {
            if (Config["Clear"].GetValue<MenuBool>("LsQ").Enabled)
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
            var ksE = Config["Killsteal"].GetValue<MenuBool>("KsE").Enabled;
            var Qtarget = W.GetTarget(Q.Range);
            var Etarget = R.GetTarget(E.Range);
            
            if (Qtarget == null) return;
            if (Qtarget.IsInvulnerable) return;
            if (Etarget == null) return;
            if (Etarget.IsInvulnerable) return;
            
            if (!(Me.Distance(Qtarget) <= Q.Range) ||
                !(QDamage(Qtarget)>= Qtarget.Health + OktwCommon.GetIncomingDamage(Qtarget))) return;
            if (Q.IsReady() && ksQ) Q.Cast(Qtarget);
            
            if (!(Me.Distance(Etarget.Position) <= E.Range) ||
                !(EDamage(Etarget) >= Etarget.Health + OktwCommon.GetIncomingDamage(Etarget))) return;
            if (E.IsReady() && ksE && Variables.GameTimeTickCount - lastE > 1000) E.Cast(Etarget);
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

            var basedamage = new[] {0, 80, 100, 120, 140, 160}[qLevel];
            var apdamage = new[] {0, 80, 100, 120, 140, 160}[qLevel] + 0.60 * Me.TotalMagicalDamage;
            var qresult = Me.CalculateDamage(Qtarget, DamageType.Magical, basedamage + apdamage);
            return qresult;
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

            var basedamage = new[] {0, 30, 45, 60, 75, 90}[eLevel];
            var apdamage = new[] {0, 30, 45, 60, 75, 90}[eLevel] + 0.35 * Me.TotalMagicalDamage + 1.5 * Me.MaxHealth;
            var eresult = Me.CalculateDamage(Etarget, DamageType.Magical, basedamage + apdamage);
            return eresult;
        }
    }
}