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
using System.Runtime.Remoting.Messaging;
using System.Text.RegularExpressions;
using SebbyLib;

namespace ToxicAio.Champions
{
    public class Morgana
    {
        private static Spell Q, W, E, R;
        private static Menu Config, menuQ, menuW, menuE, menuR, menuL, menuK, menuM, menuD, menuP;
        private static SpellSlot igniteSlot;
        private static HitChance hitchance;
        private static AIHeroClient Me = ObjectManager.Player;

        public static void OnGameLoad()
        {
            if (Me.CharacterName != "Morgana")
            {
                return;
            }

            Q = new Spell(SpellSlot.Q, 1300f);
            W = new Spell(SpellSlot.W, 900f);
            E = new Spell(SpellSlot.E, 800f);
            R = new Spell(SpellSlot.R, 625f);

            Q.SetSkillshot(0.25f, 140f, 1200f, true, SpellType.Line);

            igniteSlot = Me.GetSpellSlot("SummonerDot");


            Config = new Menu("Morgana", "[ToxicAio]: Morgana", true);

            menuQ = new Menu("Qsettings", "Q settings");
            menuQ.Add(new MenuBool("UseQ", "Use Q in Combo"));
            menuQ.Add(new MenuBool("AQ", "Auto cast Q on cc/dashing targets")); ;
            Config.Add(menuQ);

            menuW = new Menu("Wsettings", "W settings");
            menuW.Add(new MenuBool("UseW", "use W in Combo"));
            Config.Add(menuW);

            menuE = new Menu("Esettings", "E settings");

            foreach (var ene in ObjectManager.Get<AIHeroClient>().Where(x => x.Team != Me.Team))
            {
                foreach (var lib in KurisuLib.CCList.Where(x => x.HeroName == ene.CharacterName))
                {
                    menuE.Add(new MenuBool(lib.SDataName, lib.SpellMenuName + " from " + ene.CharacterName));
                }
            }

            Config.Add(menuE);

            menuR = new Menu("Rsettings", "R settings");
            menuR.Add(new MenuBool("UseR", "use R in Combo"));
            menuR.Add(new MenuSlider("Rtargets", "Min targets to cast R", 2, 1, 5));
            Config.Add(menuR);

            menuP = new Menu("Psettings", "Pred settings");
            menuP.Add(new MenuBool("QPred", "Enable Q Prediction"));
            menuP.Add(new MenuList("Pred", "Prediction hitchance",
                new string[] {"Low", "Medium", "High", " Very High"}, 2));
            Config.Add(menuP);

            menuL = new Menu("Clear", "Clear settings");
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
            menuM.Add(new MenuBool("Qag", "AntiGapCloser"));
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
            AIBaseClient.OnProcessSpellCast += OnProcessSpelLCast;
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
            AutoQ();
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
            if (Config["Misc"].GetValue<MenuBool>("Qag").Enabled)
            {
                var target = sender;
                if (target.IsValidTarget(Q.Range))
                {
                    Q.Cast(target, true);
                }
            }

            return;
        }

        private static void OnProcessSpelLCast(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
        {
            if (sender.Type != Me.Type || !E.IsReady() || !sender.IsEnemy)
                return;

            var attacker = ObjectManager.Get<AIHeroClient>().First(x => x.NetworkId == sender.NetworkId);
            foreach (var ally in GameObjects.AllyHeroes.Where(x => x.IsValidTarget(E.Range, false)))
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
                        if (Config["Esettings"].GetValue<MenuBool>(lib.SDataName).Enabled)
                        {
                            Console.WriteLine(Config["Esettings"].GetValue<MenuBool>(lib.SDataName).Enabled.ToString());
                            Console.WriteLine(lib.SDataName);
                            Console.WriteLine(ally.CharacterName);
                            E.Cast(Me);
                        }
                    }
                    catch { }
                }
            }
        }

        private static void AutoQ()
        {
            var Qtarg = Q.GetTarget(Q.Range);
            var aq = Config["Qsettings"].GetValue<MenuBool>("AQ");
            var pred = Q.GetPrediction(Qtarg);
            if (Qtarg == null) return;
            
            if (!Q.IsReady() || !Q.IsInRange(Qtarg) || !aq.Enabled || !Qtarg.IsValidTarget(Q.Range)) return;
            if (Qtarg.HasBuffOfType(BuffType.Stun) ||
                Qtarg.HasBuffOfType(BuffType.Snare) ||
                Qtarg.HasBuffOfType(BuffType.Knockup) ||
                Qtarg.HasBuffOfType(BuffType.Suppression) ||
                Qtarg.HasBuffOfType(BuffType.Charm) ||
                Qtarg.IsRecalling())
            {
                Q.Cast(Qtarg);
            }
            else if (pred.Hitchance >= HitChance.Dash)
            {
                Q.Cast(pred.CastPosition);
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
            var Qinput = Q.GetPrediction(qtarget);
            var qpred = Config["Psettings"].GetValue<MenuBool>("QPred");
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

            if (Q.IsReady() && useQ.Enabled && qtarget.IsValidTarget(Q.Range) && Q.IsInRange(qtarget) &&
                Qinput.Hitchance >= hitchance && qpred.Enabled)
            {
                Q.Cast(Qinput.CastPosition);
            }
            else if (Q.IsReady() && useQ.Enabled && qtarget.IsValidTarget(Q.Range) && Q.IsInRange(qtarget) && !qpred.Enabled)
            {
                Q.Cast(qtarget);
            }
        }

        private static void LogicW()
        {
            var wtarget = W.GetTarget(W.Range);
            var useW = Config["Wsettings"].GetValue<MenuBool>("UseW");
            if (wtarget == null) return;

            if (W.IsReady() && useW.Enabled && wtarget.IsValidTarget(W.Range))
            {
                W.Cast(wtarget);
            }
        }

        private static void LogicR()
        {
            var rtarget = R.GetTarget(Q.Range);
            var useR = Config["Rsettings"].GetValue<MenuBool>("UseR");
            var rtag = Config["Rsettings"].GetValue<MenuSlider>("Rtargets");
            if (rtarget == null) return;

            if (R.IsReady() && Me.CountEnemyHeroesInRange(R.Range) >= rtag.Value && rtarget.IsValidTarget(R.Range) &&
                !rtarget.HaveSpellShield())
            {
                R.Cast();
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
                if (JcQq.Enabled && Q.IsReady() && Me.Distance(mob) < Q.Range) Q.Cast(mob.Position);
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
                    var wFarmLoaction = W.GetCircularFarmLocation(minions);
                    if (wFarmLoaction.Position.IsValid())
                    {
                        W.Cast(wFarmLoaction.Position);
                        return;
                    }
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

            if (!(Me.Distance(Qtarget) <= Q.Range) ||
                !(QDamage(Qtarget) >= Qtarget.Health + OktwCommon.GetIncomingDamage(Qtarget))) return;
            if (Q.IsReady() && ksQ) Q.Cast(Qtarget);

            if (!(Me.Distance(Wtarget.Position) <= W.Range) ||
                !(WDamage(Wtarget) >= Wtarget.Health + OktwCommon.GetIncomingDamage(Wtarget))) return;
            if (W.IsReady() && ksW) W.Cast(Wtarget);

            if (!(Me.Distance(Rtarget.Position) <= R.Range) ||
                !(RDamage(Rtarget) >= Rtarget.Health + OktwCommon.GetIncomingDamage(Rtarget))) return;
            if (R.IsReady() && ksR) R.Cast(Rtarget);
        }

        private static readonly float[] QBaseDamage = {0f, 80f, 135f, 190f, 245f, 300f, 300f};
        private static readonly float[] WBaseDamage = {0f, 60f, 110f, 160f, 210f, 260f, 260f};
        private static readonly float[] RBaseDamage = {0f, 150f, 225f, 300f, 300f};

        private static float QDamage(AIBaseClient Qtarget)
        {
            var qLevel = Q.Level;
            var qBasedamage = QBaseDamage[qLevel] + 0.90 * Me.TotalMagicalDamage;
            return (float) GameObjects.Player.CalculateDamage(Qtarget, DamageType.Magical, qBasedamage);
        }

        private static float WDamage(AIBaseClient Wtarget)
        {
            var wLevel = W.Level;
            var wBasedamage = WBaseDamage[wLevel] + 0.70 * Me.TotalMagicalDamage;
            return (float) GameObjects.Player.CalculateDamage(Wtarget, DamageType.Magical, wBasedamage);
        }

        private static float RDamage(AIBaseClient Rtarget)
        {
            var rLevel = R.Level;
            var rBasedamage = RBaseDamage[rLevel] + 0.70 * Me.TotalMagicalDamage;
            return (float) GameObjects.Player.CalculateDamage(Rtarget, DamageType.Magical, rBasedamage);
        }

    }
}
