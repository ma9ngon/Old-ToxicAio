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
    public class Rengar
    {
        private static Spell Q, W, E, R;
        private static Menu Config, menuQ, menuW, menuE, menuL, menuK, menuM, menuD, menuP;
        private static SpellSlot igniteSlot;
        private static HitChance hitchance;
        private static AIHeroClient Me = ObjectManager.Player;

        public static void OnGameLoad()
        {
            if (Me.CharacterName != "Rengar")
            {
                return;
            }

            Q = new Spell(SpellSlot.Q, Me.GetRealAutoAttackRange());
            W = new Spell(SpellSlot.W, 450f);
            E = new Spell(SpellSlot.E, 1000f);
            R = new Spell(SpellSlot.R, 2000f);
            
            E.SetSkillshot(0.25f, 140f, 1500f, true, SpellType.Line);

            igniteSlot = Me.GetSpellSlot("SummonerDot");


            Config = new Menu("Rengar", "[ToxicAio]: Rengar", true);

            menuQ = new Menu("Qsettings", "Q settings");
            menuQ.Add(new MenuBool("UseQ", "Use Q in Combo"));
            Config.Add(menuQ);

            menuW = new Menu("Wsettings", "W settings");
            menuW.Add(new MenuBool("UseW", "use W in Combo"));
            menuW.Add(new MenuBool("Wh", "use W to remove cc"));
            menuW.Add(new MenuBool("Wheal", "use W to heal"));
            menuW.Add(new MenuSlider("Whp", "W HP %", 50, 0, 100));
            Config.Add(menuW);

            menuE = new Menu("Esettings", "E settings");
            menuE.Add(new MenuBool("UseE", "use E in Combo"));
            Config.Add(menuE);
            
            menuP = new Menu("Psettings", "Pred settings");
            menuP.Add(new MenuBool("EPred", "Enable E Prediction"));
            menuP.Add(new MenuList("Pred", "Prediction hitchance",
                new string[] {"Low", "Medium", "High", " Very High"}, 2));
            Config.Add(menuP);

            menuL = new Menu("Clear", "Clear settings");
            menuL.Add(new MenuBool("LcQ", "use Q to Laneclear"));
            menuL.Add(new MenuBool("LcW", "use W to Laneclear"));
            menuL.Add(new MenuBool("LcE", "use E to Laneclear"));
            menuL.Add(new MenuBool("JcQ", "use Q to Jungleclear"));
            menuL.Add(new MenuBool("JcW", "use W to Jungleclear"));
            menuL.Add(new MenuBool("JcE", "use E to Jungleclear"));
            Config.Add(menuL);

            menuK = new Menu("Killsteal", "Killsteal settings");
            menuK.Add(new MenuBool("KsQ", "use Q to Killsteal"));
            menuK.Add(new MenuBool("KsW", "use W to Killsteal"));
            menuK.Add(new MenuBool("KsE", "use E to Killsteal"));
            Config.Add(menuK);

            menuM = new Menu("Misc", "Misc settings");
            menuM.Add(new MenuBool("Eag", "AntiGapCloser"));
            menuM.Add(new MenuBool("Eint", "Interrupter"));
            menuM.Add(new MenuList("Prio", "Empowered Prio",
                new string[] {"Q", "W", "E",}, 0));
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
            Interrupter.OnInterrupterSpell += Interrupterr;
        }

        static int comb(Menu submenu, string sig)
        {
            return submenu[sig].GetValue<MenuList>().Index;
        }

        public static void OnGameUpdate(EventArgs args)
        {

            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
            {
                switch (comb(menuM, "Prio"))
                {
                    case 0:
                        LogicQ();
                        LogicE();
                        LogicW();

                        break;
                    
                    case 1:
                        LogicW();
                        LogicQ();
                        LogicE();

                        break;
                    
                    case 2:
                        LogicE();
                        LogicQ();
                        LogicW();

                        break;
                }
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
            wheal();
            Wremove();
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
        
        private static void Interrupterr(AIHeroClient sender, Interrupter.InterruptSpellArgs args)
        {
            if (Config["Misc"].GetValue<MenuBool>("Eint").Enabled && E.IsReady() && sender.IsValidTarget(E.Range) && Me.Mana > 3)
            {
                E.Cast(sender);
            }
        }

        private static void Wremove()
        {
            if (Config["Wsettings"].GetValue<MenuBool>("Wh").Enabled && Me.Mana > 3)
            {
                BuffType[] buffList =
                {
                    BuffType.Asleep,
                    BuffType.Charm,
                    BuffType.Fear,
                    BuffType.Knockback,
                    BuffType.Taunt,
                    BuffType.Snare,
                    BuffType.Slow
                };

                foreach (var b in buffList.Where(b => Me.HasBuffOfType(b)))
                {
                    W.Cast();
                }
            }
        }

        private static void wheal()
        {
            var wh = Config["Wsettings"].GetValue<MenuBool>("Wheal");
            var whp = Config["Wsettings"].GetValue<MenuSlider>("Whp");
            if (W.IsReady() && wh.Enabled && Me.HealthPercent < whp.Value && Me.CountEnemyHeroesInRange(Q.Range) > 0)
            {
                W.Cast();
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
            
            if (Q.IsReady() && qtarget.IsValidTarget(Q.Range) && useQ.Enabled)
            {
                Q.Cast();
            }
            
            switch (comb(menuM, "Prio"))
            {
                case 0:
                    
                    if (Q.IsReady() && qtarget.IsValidTarget(Q.Range) && Me.HasBuff("rengarpassivebuff") &&
                        Me.Mana > 3 )
                    {
                        if (Me.CountEnemyHeroesInRange(725) > 0 && Me.HasBuff("rengarpassivebuff"))
                        {
                            Q.Cast();
                        }
                    }
                    else if (Q.IsReady() && qtarget.IsValidTarget(Q.Range) &&
                             Me.Mana > 3)
                    {
                        Q.Cast();
                    }

                    break;
            }
        }

        private static void LogicW()
        {
            var wtarget = W.GetTarget(W.Range);
            var useW = Config["Wsettings"].GetValue<MenuBool>("UseW");
            if (wtarget == null) return;
            
            if (useW.Enabled && W.IsReady() && wtarget.IsValidTarget(W.Range) && Me.Mana < 3)
            {
                W.Cast(wtarget);
            }
            
            switch (comb(menuM, "Prio"))
            {
                case 1:
                    if (useW.Enabled && W.IsReady() && wtarget.IsValidTarget(W.Range) && Me.Mana > 3 && !Me.HasBuff("rengarpassivebuff"))
                    {
                        W.Cast();
                    }

                    break;
                    
            }
        }

        private static void LogicE()
        {
            var etarget = E.GetTarget(E.Range);
            var useE = Config["Esettings"].GetValue<MenuBool>("UseE");
            var input = E.GetPrediction(etarget);
            var epred = Config["Psettings"].GetValue<MenuBool>("EPred");
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
            
            if (E.IsReady() && etarget.IsValidTarget(E.Range) && input.Hitchance >= hitchance && useE.Enabled && Me.Mana < 3 && epred.Enabled)
            {
                E.Cast(input.CastPosition);
            }
            else if (E.IsReady() && etarget.IsValidTarget(E.Range) && useE.Enabled && Me.Mana < 3 && !epred.Enabled)
            {
                E.Cast(etarget);
            }
            
            switch (comb(menuM, "Prio"))
            {
                case 2:
                    
                    if (E.IsReady() && etarget.IsValidTarget(E.Range) && Me.Mana > 3 &&
                        !Me.HasBuff("rengarpassivebuff") && useE.Enabled && input.Hitchance >= hitchance && epred.Enabled)
                    {
                        E.Cast(input.CastPosition);
                    }
                    else if (E.IsReady() && etarget.IsValidTarget(E.Range) && Me.Mana > 3 &&
                             !Me.HasBuff("rengarpassivebuff") && useE.Enabled && !epred.Enabled)
                    {
                        E.Cast(etarget);
                    }

                    break;
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
                if (JcQq.Enabled && Q.IsReady() && Me.Distance(mob) < Q.Range) Q.Cast();
                if (JcWw.Enabled && W.IsReady() && Me.Distance(mob.Position) < W.Range) W.Cast();
                if (JcEe.Enabled && E.IsReady() && Me.Distance(mob.Position) < E.Range) E.Cast(mob.Position);
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
                    var wFarmLoaction = W.GetCircularFarmLocation(minions);
                    if (wFarmLoaction.Position.IsValid())
                    {
                        W.Cast();
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
                        Q.Cast();
                        return;
                    }
                }
            }
            
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
                !(Q.GetDamage(Qtarget)>= Qtarget.Health + OktwCommon.GetIncomingDamage(Qtarget))) return;
            if (Q.IsReady() && ksQ) Q.Cast(Qtarget);

            if (!(Me.Distance(Wtarget.Position) <= W.Range) ||
                !(WDamage(Wtarget) >= Wtarget.Health + OktwCommon.GetIncomingDamage(Wtarget))) return;
            if (W.IsReady() && ksW) W.Cast(Wtarget);

            if (!(Me.Distance(Etarget.Position) <= E.Range) ||
                !(EDamage(Etarget) >= Etarget.Health + OktwCommon.GetIncomingDamage(Etarget))) return;
            if (E.IsReady() && ksE) E.Cast(Etarget);
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

            var baseDamage = new[] {0, 50, 80, 110, 140, 170}[wLevel];
            var adDamage = new[] {0, 50, 80, 110, 140, 170}[wLevel] + 0.80 * Me.TotalMagicalDamage;
            var wResult = Me.CalculateDamage(Wtarget, DamageType.Magical, baseDamage + adDamage);
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

            var baseDamage = new[] {0, 55, 100, 145, 190, 235}[eLevel];
            var adDamage = new[] {0, 55, 100, 145, 190, 235}[eLevel] + 0.80 * Me.GetBonusPhysicalDamage();
            var eResult = Me.CalculateDamage(Etarget, DamageType.Physical, baseDamage + adDamage);
            return eResult;
        }
    }
}