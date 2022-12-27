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
using SharpDX.Direct3D9;

namespace ToxicAio.Champions
{
    public class Ziggs
    {
        private static Spell Q, W, E, R;
        private static Menu Config, menuQ, menuW, menuE, menuR, menuL, menuK, menuM, menuD, menuP;
        private static SpellSlot igniteSlot;
        private static HitChance hitchance;
        private static AIHeroClient Me = ObjectManager.Player;
        private static Font thm;

        public static void OnGameLoad()
        {
            if (Me.CharacterName != "Ziggs")
            {
                return;
            }

            Q = new Spell(SpellSlot.Q, 850f);
            W = new Spell(SpellSlot.W, 1000f);
            E = new Spell(SpellSlot.E, 900f);
            R = new Spell(SpellSlot.R, 5000f);

            Q.SetSkillshot(0.25f, 150f, 1700f, false, SpellType.Line);
            W.SetSkillshot(0.25f, 325f, 1750f, false, SpellType.Circle);
            E.SetSkillshot(0.25f, 325f, 1550, false, SpellType.Circle);
            R.SetSkillshot(0.375f, 525f, 2250f, false, SpellType.Circle);

            igniteSlot = Me.GetSpellSlot("SummonerDot");
            thm = new Font(Drawing.Direct3DDevice9, new FontDescription { FaceName = "Tahoma", Height = 22, Weight = FontWeight.Bold, OutputPrecision = FontPrecision.Default, Quality = FontQuality.ClearType });


            Config = new Menu("Ziggs", "[ToxicAio]: Ziggs", true);

            menuQ = new Menu("Qsettings", "Q settings");
            menuQ.Add(new MenuBool("UseQ", "Use Q in Combo"));
            Config.Add(menuQ);

            menuW = new Menu("Wsettings", "W settings");
            menuW.Add(new MenuBool("UseW", "use W in Combo", false));
            Config.Add(menuW);

            menuE = new Menu("Esettings", "E settings");
            menuE.Add(new MenuBool("UseE", "use E in Combo"));
            Config.Add(menuE);

            menuR = new Menu("Rsettings", "R settings");
            menuR.Add(new MenuBool("UseR", "use R in Combo"));
            menuR.Add(new MenuKeyBind("SemiR", "Semi R", Keys.T, KeyBindType.Press));
            Config.Add(menuR);
            
            menuP = new Menu("Psettings", "Pred settings");
            menuP.Add(new MenuBool("QPred", "Enable Q Prediction"));
            menuP.Add(new MenuBool("WPred", "Enable W Prediction"));
            menuP.Add(new MenuBool("EPred", "Enable E Prediction"));
            menuP.Add(new MenuBool("RPred", "Enable R Prediction"));
            menuP.Add(new MenuList("Pred", "Q Prediction hitchance",
                new string[] {"Low", "Medium", "High", " Very High"}, 2));
            Config.Add(menuP);

            menuL = new Menu("Clear", "Clear settings");
            menuL.Add(new MenuBool("LcQ", "use Q to Laneclear"));
            menuL.Add(new MenuBool("LcE", "use E to Laneclear"));
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
            menuM.Add(new MenuBool("Wag", "AntiGapCloser"));
            menuM.Add(new MenuBool("WInt", "Interrupter"));
            menuM.Add(new MenuSliderButton("Skin", "SkindID", 0, 0, 30, false));
            Config.Add(menuM);

            menuD = new Menu("Draw", "Draw settings");
            menuD.Add(new MenuBool("drawQ", "Q Range  (White)", true));
            menuD.Add(new MenuBool("drawW", "W Range  (White)", true));
            menuD.Add(new MenuBool("drawE", "E Range (White)", true));
            menuD.Add(new MenuBool("drawR", "R Range  (Red)", true));
            menuD.Add(new MenuBool("drawD", "Draw Combo Damage", true));
            menuD.Add(new MenuBool("DrawKill", "Draw R Killable"));
            Config.Add(menuD);

            Config.Attach();

            GameEvent.OnGameTick += OnGameUpdate;
            Drawing.OnDraw += OnDraw;
            AntiGapcloser.OnGapcloser += OnGapCloser;
            Interrupter.OnInterrupterSpell += Interrupterr;
        }

        static int comb(Menu submenu, string sig)
        {
            return submenu[sig].GetValue<MenuList>().Index;
        }

        public static void OnGameUpdate(EventArgs args)
        {

            if (Config["Rsettings"].GetValue<MenuKeyBind>("SemiR").Active)
            {
                semiR();
            }

            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
            {
                LogicE();
                LogicQ();
                LogicR();
                LogicW();
            }

            if (Orbwalker.ActiveMode == OrbwalkerMode.LaneClear)
            {
                Lanceclear();
                Jungle();
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

        private static void OnGapCloser(AIHeroClient sender, AntiGapcloser.GapcloserArgs args)
        {
            if (Config["Misc"].GetValue<MenuBool>("Wag").Enabled)
            {
                var target = sender;
                if (target.IsValidTarget(W.Range))
                {
                    W.Cast(target, true);
                }
            }
        }

        private static void Interrupterr(AIHeroClient sender, Interrupter.InterruptSpellArgs args)
        {
            if (Config["Misc"].GetValue<MenuBool>("WInt").Enabled && W.IsReady() && sender.IsValidTarget(W.Range))
            {
                W.Cast(sender);
            }
        }

        private static void semiR()
        {
            var target = R.GetTarget(R.Range);
            if (target == null) return;

            foreach (var targets in GameObjects.EnemyHeroes.Where(x => x.IsValidTarget(R.Range) && RDamage(x) >= x.Health && !x.IsInvulnerable))
            {
                var input = R.GetPrediction(targets);
                if (input.Hitchance >= HitChance.VeryHigh && R.IsInRange(input.CastPosition))
                {
                    R.Cast(input.CastPosition);
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

            if (Config["Draw"].GetValue<MenuBool>("DrawKill").Enabled && R.IsReady())
            {
                var target = TargetSelector.GetTarget(R.Range, DamageType.Magical);
                if (target.IsValidTarget(R.Range) && R.IsInRange(target))
                {
                    if (RDamage(target) > target.Health)
                    {
                        Vector2 ft = Drawing.WorldToScreen(Me.Position);
                        DrawFont(thm, " Press T to Kill: " + target.CharacterName , (float)(ft[0] - 20), (float)(ft[1] + 50), SharpDX.Color.Orange);
                    }
                }
            }
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
        
        private static void DrawFont(Font vFont, string vText, float jx, float jy, ColorBGRA jc)
        {
            vFont.DrawText(null, vText, (int)jx, (int)jy, jc);
        }


        private static void LogicQ()
        {
            var qtarget = Q.GetTarget(Q.Range);
            var useQ = Config["Qsettings"].GetValue<MenuBool>("UseQ");
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

            if (Q.IsReady() && useQ.Enabled && qtarget.IsValidTarget(Q.Range) && input.Hitchance >= hitchance && qpred.Enabled)
            {
                Q.Cast(input.CastPosition);
            }
            else if (Q.IsReady() && useQ.Enabled && qtarget.IsValidTarget(Q.Range) && !qpred.Enabled)
            {
                Q.Cast(qtarget);
            }
        }

        private static void LogicW()
        {
            var wtarget = W.GetTarget(W.Range);
            var useW = Config["Wsettings"].GetValue<MenuBool>("UseW");
            var wpred = Config["Psettings"].GetValue<MenuBool>("WPred");
            var input = W.GetPrediction(wtarget);
            if (wtarget == null) return;

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

            if (W.IsReady() && useW.Enabled && input.Hitchance >= hitchance &&
                wtarget.IsValidTarget(W.Range) && wpred.Enabled)
            {
                W.Cast(input.CastPosition);
            }
            else if (W.IsReady() && useW.Enabled && wtarget.IsValidTarget(W.Range) && !wpred.Enabled)
            {
                W.Cast(wtarget);
            }
        }

        private static void LogicE()
        {
            var etarget = E.GetTarget(E.Range);
            var useE = Config["Esettings"].GetValue<MenuBool>("UseE");
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

            if (E.IsReady() && useE.Enabled && input.Hitchance >= hitchance && etarget.IsValidTarget(E.Range) && epred.Enabled)
            {
                E.Cast(input.CastPosition);
            }
            else if (E.IsReady() && useE.Enabled && etarget.IsValidTarget(E.Range) && !epred.Enabled)
            {
                E.Cast(etarget);
            }
        }

        private static void LogicR()
        {
            var rtarget = R.GetTarget(R.Range);
            var useR = Config["Rsettings"].GetValue<MenuBool>("UseR");
            var rpred = Config["Psettings"].GetValue<MenuBool>("RPred");
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

            if (R.IsReady() && useR.Enabled && input.Hitchance >= hitchance && RDamage(rtarget) >= rtarget.Health && rpred.Enabled)
            {
                R.Cast(input.CastPosition);
            }
            else if (R.IsReady() && useR.Enabled && RDamage(rtarget) >= rtarget.Health && !rpred.Enabled)
            {
                R.Cast(input.CastPosition);
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
                if (JcQq.Enabled && Q.IsReady() && Me.Distance(mob) < Q.Range) Q.Cast(mob.Position);
                if (JcWw.Enabled && W.IsReady() && Me.Distance(mob.Position) < W.Range) W.Cast(mob.Position);
                if (JcEe.Enabled && E.IsReady() && Me.Distance(mob.Position) < E.Range) E.Cast(mob.Position);
            }
        }

        private static void Lanceclear()
        {
            var lce = Config["Clear"].GetValue<MenuBool>("LcE");
            if (lce.Enabled && E.IsReady())
            {
                var minions = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(W.Range) && x.IsMinion())
                    .Cast<AIBaseClient>().ToList();
                if (minions.Any())
                {
                    var eFarmLoaction = E.GetCircularFarmLocation(minions);
                    if (eFarmLoaction.Position.IsValid())
                    {
                        E.Cast(eFarmLoaction.Position);
                        return;
                    }
                }
            }
            
            var lcq = Config["Clear"].GetValue<MenuBool>("LcQ");
            if (lcq.Enabled && Q.IsReady())
            {
                var minions = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(W.Range) && x.IsMinion())
                    .Cast<AIBaseClient>().ToList();
                if (minions.Any())
                {
                    var qFarmLoaction = Q.GetCircularFarmLocation(minions);
                    if (qFarmLoaction.Position.IsValid())
                    {
                        Q.Cast(qFarmLoaction.Position);
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
            var ksR = Config["Killsteal"].GetValue<MenuBool>("KsR").Enabled;
            var Qtarget = Q.GetTarget(Q.Range);
            var Wtarget = W.GetTarget(W.Range);
            var Etarget = E.GetTarget(E.Range);
            var Rtarget = R.GetTarget(E.Range);

            if (Qtarget == null) return;
            if (Qtarget.IsInvulnerable) return;
            if (Wtarget == null) return;
            if (Wtarget.IsInvulnerable) return;
            if (Etarget == null) return;
            if (Etarget.IsInvulnerable) return;
            if (Rtarget == null) return;
            if (Rtarget.IsInvulnerable) return;

            if (!(Me.Distance(Qtarget) <= Q.Range) ||
                !(QDamage(Qtarget)>= Qtarget.Health + OktwCommon.GetIncomingDamage(Qtarget))) return;
            if (Q.IsReady() && ksQ) Q.Cast(Qtarget);

            if (!(Me.Distance(Wtarget.Position) <= W.Range) ||
                !(WDamage(Wtarget) >= Wtarget.Health + OktwCommon.GetIncomingDamage(Wtarget))) return;
            if (W.IsReady() && ksW) W.Cast(Wtarget);

            if (!(Me.Distance(Etarget.Position) <= E.Range) ||
                !(EDamage(Etarget) >= Etarget.Health + OktwCommon.GetIncomingDamage(Etarget))) return;
            if (E.IsReady() && ksE) E.Cast(Etarget);
            
            if (!(Me.Distance(Rtarget.Position) <= R.Range) ||
                !(RDamage(Rtarget) >= Rtarget.Health + OktwCommon.GetIncomingDamage(Rtarget))) return;
            if (R.IsReady() && ksR) R.Cast(Etarget);
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

            var baseDamage = new[] {0, 85, 135, 185, 235, 285}[qLevel];
            var adDamage = new[] {0, 85, 135, 185, 235, 285}[qLevel] + 0.65 * Me.TotalMagicalDamage;
            var qResult = Me.CalculateDamage(Qtarget, DamageType.Magical, baseDamage + adDamage);
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

            var baseDamage = new[] {0, 70, 105, 140, 175, 210}[wLevel];
            var adDamage = new[] {0, 70, 105, 140, 175, 210}[wLevel] + 0.50 * Me.TotalMagicalDamage;
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

            var baseDamage = new[] {0, 40, 75, 110, 145, 180}[eLevel];
            var adDamage = new[] {0, 40, 75, 110, 145, 180}[eLevel] + 0.30 * Me.TotalMagicalDamage;
            var eResult = Me.CalculateDamage(Etarget, DamageType.Magical, baseDamage + adDamage);
            return eResult;
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

            var baseDamage = new[] {0, 200, 300, 400}[rLevel];
            var adDamage = new[] {0, 200, 300, 400}[rLevel] + 0.73 * Me.TotalMagicalDamage;
            var rResult = Me.CalculateDamage(Rtarget, DamageType.Magical, baseDamage + adDamage);
            return rResult;
        }
    }
}