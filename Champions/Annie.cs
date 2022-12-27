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
    public static class Annie
    {
        private static Spell Q, W, E, R;
        private static Menu Config, menuQ, menuW, menuE, menuR, menuL, menuK, menuM, menuD;
        private static SpellSlot igniteSlot;
        private static AIHeroClient Me = ObjectManager.Player;
        private static bool gotAggro;

        public static void OnGameLoad()
        {
            if (Me.CharacterName != "Annie")
            {
                return;
            }

            Q = new Spell(SpellSlot.Q, 625f);
            W = new Spell(SpellSlot.W, 625f);
            E = new Spell(SpellSlot.E, 800f);
            R = new Spell(SpellSlot.R, 600f);
            
            Q.SetTargetted(0.25f, float.MaxValue);
            W.SetSkillshot(0.25f, 49f, float.MaxValue, false, SpellType.Cone);
            R.SetSkillshot(0.25f, 250f, float.MaxValue, false, SpellType.Circle);

            igniteSlot = Me.GetSpellSlot("SummonerDot");


            Config = new Menu("Annie", "[ToxicAio]: Annie", true);

            menuQ = new Menu("Qsettings", "Q settings");
            menuQ.Add(new MenuBool("UseQ", "Use Q in Combo"));
            Config.Add(menuQ);

            menuW = new Menu("Wsettings", "W settings");
            menuW.Add(new MenuBool("UseW", "use W in Combo"));
            Config.Add(menuW);

            menuE = new Menu("Esettings", "E settings");
            menuE.Add(new MenuBool("UseE", "use E in Combo"));
            menuE.Add(new MenuBool("aes", "Auto Cast E To Get Last Passive Stack"));
            Config.Add(menuE);

            menuR = new Menu("Rsettings", "R settings");
            menuR.Add(new MenuBool("UseR", "use R in Combo"));
            menuR.Add(new MenuBool("UseRS", "use R only with stun"));
            Config.Add(menuR);

            menuL = new Menu("Clear", "Clear settings");
            menuL.Add(new MenuBool("LhQ", "Q Last Hit"));
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
            menuM.Add(new MenuBool("Wag", "AntiGapCloser"));
            menuM.Add(new MenuBool("Int", "W Interrupter"));
            menuM.Add(new MenuBool("asf", "Auto Stack Passive In fountain"));
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
            AIHeroClient.OnAggro += OnAggro;
            Orbwalker.OnBeforeAttack += OnBeforeAA;
            AntiGapcloser.OnGapcloser += OnGapCloser;
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
                LogicR();
                LogicW();
                LogicE();
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
            basestack();
            EStack();
            Shield();
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
        
        private static void OnAggro(AIBaseClient sender, AIBaseClientAggroEventArgs args)
        {
            if (!ObjectManager.Player.IsDead && sender.IsEnemy && !sender.IsMinion() &&
                args.NetworkId == ObjectManager.Player.NetworkId) gotAggro = true;
        }

        private static bool InAARangeOf(this AIHeroClient player, AIHeroClient target)
        {
            if (player.Distance(target.Position) < target.AttackRange) return true;
            return false;
        }

        private static void OnBeforeAA(object sender, BeforeAttackEventArgs args)
        {
            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
            {
                LogicQ(args.Target);
            }
        }


        private static void LogicQ(AttackableUnit target)
        {
            var tt = target as AIBaseClient;
            var qtarget = Q.GetTarget(Q.Range);
            var useQ = Config["Qsettings"].GetValue<MenuBool>("UseQ");
            if (qtarget == null) return;

            if (Q.IsReady() && useQ.Enabled && qtarget.IsValidTarget(Q.Range) && Q.IsInRange(qtarget))
            {
                Q.Cast(tt);
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

            if (E.IsReady() && useE.Enabled)
            {
                var close = GameObjects.EnemyHeroes.Where(x =>
                    Me.InAARangeOf(x) && x.IsFacing(Me) || x.GetWaypoints().LastOrDefault().DistanceToPlayer() < 100f);

                if (gotAggro && !close.Any())
                {
                    gotAggro = false;
                }
                else if (gotAggro && close.Any())
                {
                    E.Cast();
                }
            }
        }

        private static void LogicR()
        {
            var rtarget = R.GetTarget();
            var useR = Config["Rsettings"].GetValue<MenuBool>("UseR");
            var useRS = Config["Rsettings"].GetValue<MenuBool>("UseRS");
            if (rtarget == null) return;

            if (R.IsReady() && useR.Enabled && rtarget.IsValidTarget(R.Range) && !useRS.Enabled && R.GetDamage(rtarget) + W.GetDamage(rtarget) + Q.GetDamage(rtarget) >= rtarget.Health)
            {
                R.Cast(rtarget.Position);
            }
            else if (R.IsReady() && useR.Enabled && useRS.Enabled && Me.HasBuff("anniepassiveprimed") &&
                     R.GetDamage(rtarget) + W.GetDamage(rtarget) + Q.GetDamage(rtarget) >= rtarget.Health)
            {
                R.Cast(rtarget.Position);
            }
        }
        
        private static void Shield()
        {
            var target = E.GetTarget(E.Range);
            var useE = Config["Esettings"].GetValue<MenuBool>("UseE");

            foreach (var allies in GameObjects.AllyHeroes.Where(y => y.HealthPercent < 35 && useE.Enabled && y.DistanceToPlayer() < E.Range && !ObjectManager.Player.IsMe))
            {
                E.Cast(allies);
            }
        }
        
        private static void OnGapCloser(AIHeroClient sender, AntiGapcloser.GapcloserArgs args)
        {
            if (Config["Misc"].GetValue<MenuBool>("Wag").Enabled && Me.HasBuff("anniepassiveprimed"))
            {
                var target = sender;
                if (target.IsValidTarget(W.Range))
                {
                    W.Cast(target, true);
                }
            }

            return;
        }
        
        private static void Interrupterr(AIHeroClient sender, Interrupter.InterruptSpellArgs args)
        {
            if (Config["Misc"].GetValue<MenuBool>("Int").Enabled && W.IsReady() && sender.IsValidTarget(2500) && Me.HasBuff("anniepassiveprimed"))
            {
                W.Cast(sender);
            }
        }
        
        private static void EStack()
        {
            var aes = Config["Esettings"].GetValue<MenuBool>("aes");
            if (aes.Enabled && ObjectManager.Player.GetBuffCount("anniepassivestack") == 3 && E.IsReady())
            {
                E.Cast();
            }
        }
        
        private static void basestack()
        {
            var basestack = Config["Misc"].GetValue<MenuBool>("asf");
            if (basestack.Enabled && ObjectManager.Player.InFountain() &&
                ObjectManager.Player.GetBuffCount("anniepassivestack") < 4 && !ObjectManager.Player.HasBuff("anniepassiveprimed"))
            {
                W.Cast(Game.CursorPos);
                E.Cast();
            }
        }

        private static void Jungle()
        {
            var JcWw = Config["Clear"].GetValue<MenuBool>("JcW");
            var JcQq = Config["Clear"].GetValue<MenuBool>("JcQ");
            var mobs = GameObjects.Jungle.Where(x => x.IsValidTarget(Q.Range)).OrderBy(x => x.MaxHealth)
                .ToList<AIBaseClient>();
            if (mobs.Count > 0)
            {
                var mob = mobs[0];
                if (JcWw.Enabled && W.IsReady() && Me.Distance(mob.Position) < W.Range) W.Cast(mob.Position);
                if (JcQq.Enabled && Q.IsReady() && Me.Distance(mob.Position) < Q.Range) Q.Cast(mob);
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
            var ksR = Config["Killsteal"].GetValue<MenuBool>("KsR").Enabled;
            var Qtarget = Q.GetTarget(Q.Range);
            var Wtarget = W.GetTarget(W.Range);
            var Rtarget = R.GetTarget(R.Range);
            
            if (Qtarget == null) return;
            if (Qtarget.IsInvulnerable) return;
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
            if (R.IsReady() && ksR) R.Cast(Rtarget.Position);
        }

        private static readonly float[] QBaseDamage = {0f, 80f, 115f, 150f, 185f, 220f, 220f};
        
        private static readonly float[] WBaseDamage = {0f, 70f, 115f, 160f, 205f, 250f, 250f};
        
        private static readonly float[] RBaseDamage = {0f, 150f, 275f, 400f, 400f,};

        private static float QDamage(AIBaseClient Qtarget)
        {
            var qLevel = Q.Level;
            var qBaseDamage = QBaseDamage[qLevel] + .75f * Me.TotalMagicalDamage;
            return (float) Me.CalculateDamage(Qtarget, DamageType.Magical, qBaseDamage);
        }
        
        private static float WDamage(AIBaseClient Wtarget)
        {
            var wLevel = W.Level;
            var wBaseDamage = WBaseDamage[wLevel] + .85f * Me.TotalMagicalDamage;
            return (float) Me.CalculateDamage(Wtarget, DamageType.Magical, wBaseDamage);
        }
        
        private static float RDamage(AIBaseClient Rtarget)
        {
            var rLevel = R.Level;
            var rBaseDamage = RBaseDamage[rLevel] + .75f * Me.TotalMagicalDamage;
            return (float) Me.CalculateDamage(Rtarget, DamageType.Magical, rBaseDamage);
        }
    }
}