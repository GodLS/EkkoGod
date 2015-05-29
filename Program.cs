using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using System.Drawing;
using Color = System.Drawing.Color;

namespace EkkoGod
{
    class Program
    {
        private static Obj_AI_Hero Player;
        private static Menu Config;
        private static Spell Q, W, E, R;
        private static Orbwalking.Orbwalker Orbwalker;
        private static SpellSlot ignite;

        static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += GameOnOnGameLoad;
        }

        private static void GameOnOnGameLoad(EventArgs args)
        {
            Player = ObjectManager.Player;

            if (Player.ChampionName != "Ekko")
            {
                return;
            }

            Q = new Spell(SpellSlot.Q, 850);
            Q.SetSkillshot(0.25f, 60f, 1650, false, SkillshotType.SkillshotLine);

            W = new Spell(SpellSlot.W, 1650);
            W.SetSkillshot(3f, 500f, int.MaxValue, false, SkillshotType.SkillshotCircle);

            E = new Spell(SpellSlot.E, 450);

            R = new Spell(SpellSlot.R, 400);

            ignite = Player.GetSpellSlot("summonerdot");

            Config = new Menu("Ekko God", "EkkoGod", true);

            Menu orbwalkerMenu = new Menu("Orbwalker", "Orbwalker");
            Orbwalker = new Orbwalking.Orbwalker(orbwalkerMenu);
            Config.AddSubMenu(orbwalkerMenu);

            var TargetSelectorMenu = new Menu("Target Selector", "Target Selector");
            TargetSelector.AddToMenu(TargetSelectorMenu);
            Config.AddSubMenu(TargetSelectorMenu);

            var comboMenu = new Menu("Combo", "Combo");
            comboMenu.AddItem(new MenuItem("UseQCombo", "Use Q in Combo").SetValue(true));
            comboMenu.AddItem(new MenuItem("UseWCombo", "Cast W before R in AoE").SetValue(true));
            comboMenu.AddItem(new MenuItem("UseWCombo2", "Cast W before R in Combo Killable").SetValue(true));
            comboMenu.AddItem(new MenuItem("UseECombo", "Use E in Combo").SetValue(true));
            comboMenu.AddItem(new MenuItem("UseRKillable", "Use R if Combo Killable").SetValue(true));
            comboMenu.AddItem(new MenuItem("UseRatHP", "Use R at %HP").SetValue(true));
            comboMenu.AddItem(new MenuItem("HP", "HP").SetValue(new Slider(30, 0, 100)));
            comboMenu.AddItem(new MenuItem("UseRAoE", "Use R AoE").SetValue(true));
            comboMenu.AddItem(new MenuItem("AoECount", "Minimum targets to R").SetValue(new Slider(3, 1, 5)));
            Config.AddSubMenu(comboMenu);

            var harassMenu = new Menu("Harass", "Harass");
            harassMenu.AddItem(new MenuItem("UseQHarass", "Use Q in Harass").SetValue(true));
            harassMenu.AddItem(new MenuItem("UseEHarass", "Use E in Harass").SetValue(true));
            harassMenu.AddItem(new MenuItem("harassMana", "Mana Manager (%)").SetValue(new Slider(40, 1, 100)));
            Config.AddSubMenu(harassMenu);

            var drawingsMenu = new Menu("Drawings", "Drawings");
            drawingsMenu.AddItem(new MenuItem("drawQ", "Q range (also is dash+leap range)").SetValue(new Circle(true, Color.FromArgb(100, 255, 255, 255))));
            drawingsMenu.AddItem(new MenuItem("drawW", "W range").SetValue(new Circle(true, Color.FromArgb(100, 255, 255, 255))));
            drawingsMenu.AddItem(new MenuItem("drawE", "E (leap) range").SetValue(new Circle(true, Color.FromArgb(100, 255, 255, 255))));
            drawingsMenu.AddItem(new MenuItem("drawGhost", "R range (around ghost)").SetValue(new Circle(true, Color.FromArgb(100, 255, 255, 255))));
            drawingsMenu.AddItem(new MenuItem("drawPassiveStacks", "Passive stacks").SetValue(new Circle(true, Color.FromArgb(100, 255, 255, 255))));
            var dmgAfterCombo = new MenuItem("DamageAfterCombo", "Draw damage after combo").SetValue(true);
            Utility.HpBarDamageIndicator.DamageToUnit = ComboDamage;
            Utility.HpBarDamageIndicator.Enabled = dmgAfterCombo.GetValue<bool>();
            drawingsMenu.AddItem(dmgAfterCombo);
            Config.AddSubMenu(drawingsMenu);

            var miscMenu = new Menu("Misc", "Misc");
            miscMenu.AddItem(new MenuItem("Killsteal", "KS with Q").SetValue(true));
            miscMenu.AddItem(new MenuItem("WSelf", "W Self on Gapclose").SetValue(true));
            miscMenu.AddItem(new MenuItem("WCC", "Cast W on Immobile").SetValue(true));
            miscMenu.AddItem(new MenuItem("UseIgnite", "Ignite if Combo Killable").SetValue(true));
            miscMenu.AddItem(new MenuItem("---", "--underneath not functional--").SetValue(false));
            miscMenu.AddItem(new MenuItem("123", "E Minion After Manual E if Target Far").SetValue(false));
            miscMenu.AddItem(new MenuItem("1234", "R dangerous spells").SetValue(false));
            Config.AddSubMenu(miscMenu);

            //Config.AddItem(new MenuItem("eToMinion", "E Minion After Manual E if Target Far").SetValue(true));


            Config.AddToMainMenu();

            Game.OnUpdate += OnUpdate;
            Drawing.OnDraw += OnDraw;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Hero_OnProcessSpellCast;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;


        }

        #region damagecalcscreditto1337/worstping
        // WorstPing
        public static double GetDamageQ(Obj_AI_Base target) // fixed
        {
            return Q.IsReady()
                       ? Player.CalcDamage(
                           target,
                           Damage.DamageType.Magical,
                           new double[] { 120, 160, 200, 240, 280 }[Q.Level - 1]
                           + Player.TotalMagicalDamage * .8f)
                       : 0d;
        }

        // 1337
        public static double GetDamageE(Obj_AI_Base target)
        {
            return E.IsReady()
                       ? Player.CalcDamage(
                           target,
                           Damage.DamageType.Magical,
                           new double[] { 50, 80, 110, 140, 170 }[E.Level - 1]
                           + Player.TotalMagicalDamage * .2f)
                       : 0d;
        }


        // WorstPing
        public static double GetDamageR(Obj_AI_Base target)
        {
            return R.IsReady()
                       ? Player.CalcDamage(
                           target,
                           Damage.DamageType.Magical,
                           new double[] { 200, 350, 500 }[R.Level - 1]
                           + Player.TotalMagicalDamage * 1.3f)
                       : 0d;
        }

        #endregion


        private static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {

            var WSelf = Config.Item("WSelf").GetValue<bool>();

            if (WSelf && W.IsReady() && Player.Distance(gapcloser.Sender.ServerPosition) < E.Range)
            {
                W.Cast(Player.Position);
            }
        }

        private static float ComboDamage(Obj_AI_Hero hero)
        {

            var dmg = 0d;

            dmg += GetDamageQ(hero);
            dmg += GetDamageE(hero);
            dmg += GetDamageR(hero);
            dmg += 15 + (12 * Player.Level) + Player.FlatMagicDamageMod; // passive damage
            if (Player.Spellbook.CanUseSpell(Player.GetSpellSlot("summonerdot")) == SpellState.Ready)
            {
                dmg += Player.GetSummonerSpellDamage(hero, Damage.SummonerSpell.Ignite);
            }
            return (float)dmg;
        }

        private static void OnUpdate(EventArgs args)
        {

            if (Player.IsDead)
            {
                return;
            }

            switch (Orbwalker.ActiveMode)
            {
                case Orbwalking.OrbwalkingMode.Combo:
                    Combo();
                    break;
                case Orbwalking.OrbwalkingMode.Mixed:
                    Harass();
                    break;
            }

            WCC();
            Killsteal();
            
            //RSafe();
        }

       private static void WCC()
       {
           var WCC = Config.Item("WCC").GetValue<bool>();
           if (WCC)
           {
               foreach (var target in HeroManager.Enemies.Where(enemy => enemy.IsVisible && !enemy.IsDead && Player.Distance(enemy.Position) <= W.Range && W.IsReady() && Player.Distance(enemy.Position) < W.Range))
               {
                   if (target.HasBuffOfType(BuffType.Taunt) || target.HasBuffOfType(BuffType.Suppression) || target.HasBuffOfType(BuffType.Snare) || target.HasBuffOfType(BuffType.Knockup) || target.HasBuffOfType(BuffType.Fear) || target.HasBuffOfType(BuffType.Charm) || target.HasBuffOfType(BuffType.Stun))
                   {
                       W.Cast(target.ServerPosition);
                   }
               }
           }
       }

       private static void Killsteal()
       {
           var KS = Config.Item("Killsteal").GetValue<bool>();
           if (KS)
           {
               foreach (var target in HeroManager.Enemies.Where(enemy => enemy.IsVisible && !enemy.IsDead && GetDamageQ(enemy) > enemy.Health && Player.Distance(enemy.Position) <= Q.Range && Q.IsReady()))
               {
                   Q.Cast(target);
               }
           }
       }

        //private static void RSafe()
        //{
        //    if (R.IsReady() && Player.HasBuff("zedulttargetmark")) //stupid idea idk what to do tho
        //    {
        //        Utility.DelayAction.Add(0, () => R.Cast());
        //    }
        //}

        private static void Combo()
        {
            var useQ = Config.Item("UseQCombo").GetValue<bool>();
            var useW = Config.Item("UseWCombo").GetValue<bool>();
            var useE = Config.Item("UseECombo").GetValue<bool>();
            var useRKillable = Config.Item("UseRKillable").GetValue<bool>();
            var useRatHP = Config.Item("UseRatHP").GetValue<bool>();
            var HP = Config.Item("HP").GetValue<Slider>();
            var useRAoE = Config.Item("UseRAoE").GetValue<bool>();
            var AoECount = Config.Item("AoECount").GetValue<Slider>();
            var alone = HeroManager.Enemies.Count(scared => scared.Distance(Player.Position) <= 1000);
            var enemyCount = 0;
            var useW2 = Config.Item("UseWCombo2").GetValue<bool>();
            var UseIgnite = Config.Item("UseIgnite").GetValue<bool>();
            if (ghost != null)
            {
                enemyCount += HeroManager.Enemies.Count(enemy => enemy.Distance(ghost.Position) <= 375);
            }

            var target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);
            if (target == null)
                return;

            if (useQ && Q.IsReady())
            {
                Q.CastIfHitchanceEquals(target, HitChance.High);
            }

            if (useE && E.IsReady())
            {
                E.Cast(Game.CursorPos);
            }

            if (useRKillable && R.IsReady() && useW2)
            {
                if (target.Distance(ghost.Position) <= 375)
                {
                    if (ComboDamage(target) >= target.Health && Player.Distance(ghost.Position) < W.Range)
                    {
                        W.Cast(ghost.Position);
                        R.Cast();
                    }
                }
            }

            else if (useRKillable && R.IsReady())
            {
                if (target.Distance(ghost.Position) <= 375)
                {
                    if (ComboDamage(target) >= target.Health)
                    {
                        R.Cast();
                    }
                }
            }

            if (useRatHP && R.IsReady())
            {
                if (Player.HealthPercent <= HP.Value && alone >= 1)
                {
                    R.Cast();
                }
            }

            if (useRAoE && R.IsReady() && enemyCount >= AoECount.Value && useW && W.IsReady() && Player.Distance(ghost.Position) < W.Range)
            {
                W.Cast(ghost.Position);
                R.Cast();
            }

            else if (useRAoE && R.IsReady() && enemyCount >= AoECount.Value)
            {
                R.Cast();
            }

            if (Player.Distance(target.ServerPosition) <= 600 && ComboDamage(target) >= target.Health && UseIgnite)
            {
                Player.Spellbook.CastSpell(ignite, target);
            }
        }

        private static void Harass()
        {

            var mana = Config.Item("harassMana").GetValue<Slider>().Value;
            var useQ = Config.Item("UseQHarass").GetValue<bool>();
            var useE = Config.Item("UseEHarass").GetValue<bool>();

            if (Player.ManaPercent < mana)
                return;

            var target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);
            if (target == null)
                return;

            if (useQ && Q.IsReady())
            {
                Q.Cast(target);
            }

            if (useE && E.IsReady())
            {
                E.Cast(Game.CursorPos);
            }
        }

        private static Obj_AI_Base ghost
        {
            get
            {
                return
                ObjectManager.Get<Obj_AI_Base>()
                                .FirstOrDefault(ghost => !ghost.IsEnemy && ghost.Name.Contains("Ekko"));
            }
        }


        private static void Obj_AI_Hero_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsMe && args.SData.Name == "EkkoE")
            {
                // make sure orbwalker doesnt mess up after casting E
                Utility.DelayAction.Add(250, Orbwalking.ResetAutoAttackTimer);

                //var eToMinion = Config.Item("eToMinion").GetValue<bool>();

                //Obj_AI_Base cloesestminiontotarget = ObjectManager.Get<Obj_AI_Base>().Where(x => x.IsMinion && x.IsEnemy).MinOrDefault(x => x.Distance(args.Target.Position));

                //if (args.Target != null && Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo && eToMinion)
                //{
                //    ObjectManager.Player.IssueOrder(GameObjectOrder.AttackUnit, cloesestminiontotarget);
                //}
            }

            //if (R.IsReady() && args.SData.Name == "ViR" && howthefuckdoiknowifimthetarget)
            //{
            //    Utility.DelayAction.Add(100, () => R.Cast());

            //}


        }

        private static void OnDraw(EventArgs args)
        {
            if (Player.IsDead)
                return;

            var drawQ = Config.Item("drawQ").GetValue<Circle>();
            var drawW = Config.Item("drawW").GetValue<Circle>();
            var drawE = Config.Item("drawE").GetValue<Circle>();
            var drawPassive = Config.Item("drawPassiveStacks").GetValue<Circle>();
            var drawGhost = Config.Item("drawGhost").GetValue<Circle>();


            if (drawQ.Active && Q.IsReady())
            {
                Render.Circle.DrawCircle(Player.Position, Q.Range, drawQ.Color);
            }
            else if (drawQ.Active && !Q.IsReady())
            {
                Render.Circle.DrawCircle(Player.Position, Q.Range, Color.Maroon);
            }

            if (drawW.Active && W.IsReady())
            {
                Render.Circle.DrawCircle(Player.Position, W.Range, drawW.Color);
            }

            if (drawE.Active && E.IsReady() || Player.Spellbook.GetSpell(SpellSlot.E).State == SpellState.Surpressed)
            {
                Render.Circle.DrawCircle(Player.Position, E.Range, drawE.Color);
            }

            else if (drawE.Active && !E.IsReady())
            {
                Render.Circle.DrawCircle(Player.Position, E.Range, Color.Maroon);
            }

            if (drawPassive.Active)
            {
                foreach (var enemy in HeroManager.Enemies)
                {
                    if (enemy.Buffs.Any(buff1 => buff1.Name == "EkkoStacks" && buff1.Count == 2) && enemy.IsVisible)
                    {
                        var enemypos = Drawing.WorldToScreen(enemy.Position);
                        Render.Circle.DrawCircle(enemy.Position, 150, Color.Red);
                        Drawing.DrawText(enemypos.X, enemypos.Y + 15, Color.Red, "2 Stacks");
                    }

                    else if (enemy.Buffs.Any(buff1 => buff1.Name == "EkkoStacks" && buff1.Count == 1) && enemy.IsVisible)
                    {
                        var enemypos = Drawing.WorldToScreen(enemy.Position);
                        Render.Circle.DrawCircle(enemy.Position, 150, drawPassive.Color);
                        Drawing.DrawText(enemypos.X, enemypos.Y + 15, drawPassive.Color, "1 Stack");
                    }
                }
            }

            if (drawGhost.Active && ghost != null)
            {
                if (R.IsReady())
                {
                    Render.Circle.DrawCircle(ghost.Position, R.Range, drawGhost.Color);
                }
            }
        }
    }
}