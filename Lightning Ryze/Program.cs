#region
using System;
using System.Collections.Generic;
using Color = System.Drawing.Color;
using System.Linq;
using SharpDX;
using LeagueSharp;
using LeagueSharp.Common;
#endregion

namespace LightningRyze
{
	internal class Program
	{
		private static Menu Config;
		private static string LastCast;
		private static float LastFlashTime;
		private static Obj_AI_Hero target;
		private static Obj_AI_Hero Player;
		private static bool UseShield;
		private static Spell Q;
		private static Spell W;
		private static Spell E;
		private static Spell R;
		private static SpellSlot IgniteSlot;
		
		private static void Main(string[] args)
		{
			CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
		}
		
		private static void Game_OnGameLoad(EventArgs args)
		{
			Player = ObjectManager.Player;
			if (Player.ChampionName != "Ryze") return;
			
			Q = new Spell(SpellSlot.Q, 625);
			W = new Spell(SpellSlot.W, 600);
			E = new Spell(SpellSlot.E, 600);
			R = new Spell(SpellSlot.R);
			IgniteSlot = Player.GetSpellSlot("SummonerDot");
			
			Config = new Menu("Lightning Ryze", "Lightning Ryze", true);
			var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
			SimpleTs.AddToMenu(targetSelectorMenu);
			Config.AddSubMenu(targetSelectorMenu);
			
			Config.AddSubMenu(new Menu("Orbwalker", "Orbwalker"));
			var orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalker"));
			
			Config.AddSubMenu(new Menu("Combo", "Combo"));
			Config.SubMenu("Combo").AddItem(new MenuItem("ComboActive", "Combo!").SetValue(new KeyBind(32, KeyBindType.Press)));
			Config.SubMenu("Combo").AddItem(new MenuItem("TypeCombo", "").SetValue(new StringList(new[] {"Mixed mode","Burst combo","Long combo"},0)));
			Config.SubMenu("Combo").AddItem(new MenuItem("UseR", "Use R").SetValue(true));
			Config.SubMenu("Combo").AddItem(new MenuItem("UseIgnite", "Use Ignite").SetValue(true));
			
			Config.AddSubMenu(new Menu("Harass", "Harass"));
			Config.SubMenu("Harass").AddItem(new MenuItem("HarassActive", "Harass!").SetValue(new KeyBind("C".ToCharArray()[0], KeyBindType.Press)));
			Config.SubMenu("Harass").AddItem(new MenuItem("HQ", "Use Q").SetValue(true));
			Config.SubMenu("Harass").AddItem(new MenuItem("HW", "Use W").SetValue(true));
			Config.SubMenu("Harass").AddItem(new MenuItem("HE", "Use E").SetValue(true));
			
			Config.AddSubMenu(new Menu("Farm", "Farm"));
			Config.SubMenu("Farm").AddItem(new MenuItem("FreezeActive", "Freeze!").SetValue(new KeyBind("X".ToCharArray()[0], KeyBindType.Press)));
			Config.SubMenu("Farm").AddItem(new MenuItem("LaneClearActive", "LaneClear!").SetValue(new KeyBind("V".ToCharArray()[0], KeyBindType.Press)));
			Config.SubMenu("Farm").AddItem(new MenuItem("FQ", "Use Q").SetValue(true));
			Config.SubMenu("Farm").AddItem(new MenuItem("FW", "Use W").SetValue(true));
			Config.SubMenu("Farm").AddItem(new MenuItem("FE", "Use E").SetValue(true));
			
			Config.AddSubMenu(new Menu("JungleFarm", "JungleFarm"));
			Config.SubMenu("JungleFarm").AddItem(new MenuItem("JungActive", "Farm!").SetValue(new KeyBind("V".ToCharArray()[0], KeyBindType.Press)));
			Config.SubMenu("JungleFarm").AddItem(new MenuItem("JQ", "Use Q").SetValue(true));
			Config.SubMenu("JungleFarm").AddItem(new MenuItem("JW", "Use W").SetValue(true));
			Config.SubMenu("JungleFarm").AddItem(new MenuItem("JE", "Use E").SetValue(true));
			
			Config.AddSubMenu(new Menu("KillSteal", "KillSteal"));
			Config.SubMenu("KillSteal").AddItem(new MenuItem("KillSteal", "Use Kill Steal").SetValue(true));
			Config.SubMenu("KillSteal").AddItem(new MenuItem("AutoIgnite", "Use Ignite").SetValue(true));
			
			Config.AddSubMenu(new Menu("Extra", "Extra"));
			Config.SubMenu("Extra").AddItem(new MenuItem("UseSera", "Use Seraphs Embrace").SetValue(true));
			Config.SubMenu("Extra").AddItem(new MenuItem("HP", "When % HP").SetValue(new Slider(20,100,0)));
			Config.SubMenu("Extra").AddItem(new MenuItem("UseWGap", "Use W GapCloser").SetValue(true));
			Config.SubMenu("Extra").AddItem(new MenuItem("UsePacket", "Use Packet Cast").SetValue(true));
			
			Config.AddSubMenu(new Menu("Drawings", "Drawings"));
			Config.SubMenu("Drawings").AddItem(new MenuItem("QRange", "Q range").SetValue(new Circle(true, Color.FromArgb(255, 255, 255, 255))));
			Config.SubMenu("Drawings").AddItem(new MenuItem("WERange", "W+E range").SetValue(new Circle(false, Color.FromArgb(255, 255, 255, 255))));
			Config.AddToMainMenu();
			
			Game.PrintChat("Lightning Ryze loaded!");

			Game.OnGameUpdate += Game_OnGameUpdate;
			Orbwalking.BeforeAttack += Orbwalking_BeforeAttack;
			Drawing.OnDraw += Drawing_OnDraw;
			Obj_AI_Base.OnProcessSpellCast += Obj_AI_Hero_OnProcessSpellCast;
			AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
		}
		
		private static void Game_OnGameUpdate(EventArgs args)
		{
			target = SimpleTs.GetTarget(Q.Range+25, SimpleTs.DamageType.Magical);
			if (Config.Item("ComboActive").GetValue<KeyBind>().Active)
			{
				if (Config.Item("TypeCombo").GetValue<StringList>().SelectedIndex == 0) ComboMixed();
				else if (Config.Item("TypeCombo").GetValue<StringList>().SelectedIndex == 1) ComboBurst();
				else if (Config.Item("TypeCombo").GetValue<StringList>().SelectedIndex == 2) ComboLong();
			}
			else if (Config.Item("HarassActive").GetValue<KeyBind>().Active) Harass();
			else if (Config.Item("LaneClearActive").GetValue<KeyBind>().Active ||
			         Config.Item("FreezeActive").GetValue<KeyBind>().Active) Farm();
			else if (Config.Item("JungActive").GetValue<KeyBind>().Active) JungleFarm();
			if (Config.Item("UseSera").GetValue<bool>()) UseItems();
		}
		
		private static void Drawing_OnDraw(EventArgs args)
		{
			var drawQ = Config.Item("QRange").GetValue<Circle>();
			if (drawQ.Active && !Player.IsDead)
			{
				Utility.DrawCircle(Player.Position, Q.Range, drawQ.Color);
			}

			var drawWE = Config.Item("WERange").GetValue<Circle>();
			if (drawWE.Active && !Player.IsDead)
			{
				Utility.DrawCircle(Player.Position, W.Range, drawWE.Color);
			}
		}
		
		private static void Orbwalking_BeforeAttack(Orbwalking.BeforeAttackEventArgs args)
		{
			if (Config.Item("ComboActive").GetValue<KeyBind>().Active || Config.Item("HarassActive").GetValue<KeyBind>().Active)
				args.Process = !(Q.IsReady() || W.IsReady() || E.IsReady() || Vector3.Distance(Player.Position, args.Target.Position) >= 600);
		}
		
		private static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
		{
			var UseW = Config.Item("UseWGap").GetValue<bool>();
			var UsePacket = Config.Item("UsePacket").GetValue<bool>();
			if (Player.HasBuff("Recall") || Player.IsWindingUp) return;
			if (UseW && W.IsReady()) W.CastOnUnit(gapcloser.Sender,UsePacket);
		}
		
		private static bool IsOnTheLine(Vector3 point, Vector3 start, Vector3 end)
		{
			var obj = Geometry.ProjectOn(point.To2D(),start.To2D(),end.To2D());
			if (obj.IsOnSegment) return true;
			return false;
		}
				
		private static void Obj_AI_Hero_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
		{
			if (sender.IsMe)
			{
				if (args.SData.Name.ToLower() == "overload")
				{
					LastCast = "Q";
				}
				else if (args.SData.Name.ToLower() == "runeprison")
				{
					LastCast = "W";
				}
				else if (args.SData.Name.ToLower() == "spellflux")
				{
					LastCast = "E";
				}
				else if (args.SData.Name.ToLower() == "desperatepower")
				{
					LastCast = "R";
				}
				else if (args.SData.Name.ToLower() == "summonerflash")
				{
					LastFlashTime = Environment.TickCount;
				}
			}
			if (sender.IsEnemy && (sender.Type == GameObjectType.obj_AI_Hero || sender.Type == GameObjectType.obj_AI_Turret))
			{
				if ( (args.SData.Name != null && IsOnTheLine(Player.Position,args.Start,args.End)) || (args.Target == Player && Vector3.Distance(Player.Position, sender.Position) <= 700))
					UseShield = true;
			}
		}
		
		private static bool IgniteKillable(Obj_AI_Base target)
		{
			return Damage.GetSummonerSpellDamage(Player, target,Damage.SummonerSpell.Ignite) > target.Health;
		}
		
		private static float GetComboDamage(Obj_AI_Base enemy)
		{
			var damage = 0d;
			if (Q.IsReady())
				damage += Player.GetSpellDamage(enemy, SpellSlot.Q)*2;
			if (W.IsReady())
				damage += Player.GetSpellDamage(enemy, SpellSlot.W);
			if (E.IsReady())
				damage += Player.GetSpellDamage(enemy, SpellSlot.E);
			if (IgniteSlot != SpellSlot.Unknown && Player.SummonerSpellbook.CanUseSpell(IgniteSlot) == SpellState.Ready)
				damage += Player.GetSummonerSpellDamage(enemy, Damage.SummonerSpell.Ignite);
			return (float)damage;
		}
		
		private static void UseItems()
		{
			var myHP = Player.Health/Player.MaxHealth*100;
			var ConfigHP = Config.Item("HP").GetValue<Slider>().Value;
			if (myHP <= ConfigHP && Items.HasItem(3040) && Items.CanUseItem(3040) && UseShield)
			{
				Items.UseItem(3040);
				UseShield = false;
			}
		}
		
		private static void ComboMixed()
		{
			var UseR = Config.Item("UseR").GetValue<bool>();
			var UseIgnite = Config.Item("UseIgnite").GetValue<bool>();
			var UsePacket = Config.Item("UsePacket").GetValue<bool>();
			if (target == null) return;
			
			if (UseIgnite && (IgniteKillable(target) || GetComboDamage(target) > target.Health))
				if (IgniteSlot != SpellSlot.Unknown && Player.SummonerSpellbook.CanUseSpell(IgniteSlot) == SpellState.Ready && Vector3.Distance(Player.Position, target.Position) <= 600)
					Player.SummonerSpellbook.CastSpell(IgniteSlot, target);
			
			if (Environment.TickCount - LastFlashTime < 1 && W.IsReady()) W.CastOnUnit(target,UsePacket);
			else
			{
				if (Q.IsKillable(target) && Q.IsReady()) Q.CastOnUnit(target,UsePacket);
				else if (E.IsKillable(target) && E.IsReady()) E.CastOnUnit(target,UsePacket);
				else if (W.IsKillable(target) && W.IsReady()) W.CastOnUnit(target,UsePacket);
				else if (Vector3.Distance(Player.Position, target.Position) >= 575 && !target.IsFacing(Player) && W.IsReady()) W.CastOnUnit(target,UsePacket);
				else
				{
					if (Q.IsReady() && W.IsReady() && E.IsReady() && GetComboDamage(target) > target.Health)
					{
						if (Q.IsReady()) Q.CastOnUnit(target,UsePacket);
						else if (R.IsReady() && UseR) R.Cast(UsePacket);
						else if (W.IsReady()) W.CastOnUnit(target,UsePacket);
						else if (E.IsReady()) E.CastOnUnit(target,UsePacket);
					}
					else if (Math.Abs(Player.PercentCooldownMod) >= 0.2)
					{
						if (Player.CountEnemysInRange(300) > 1)
						{
							if (LastCast == "Q")
							{
								if (Q.IsReady()) Q.CastOnUnit(target ,UsePacket);
								if (R.IsReady() && UseR) R.Cast(UsePacket);
								if (!R.IsReady()) W.CastOnUnit(target ,UsePacket);
								if (!R.IsReady() && !W.IsReady()) E.CastOnUnit(target ,UsePacket);
							}
							else Q.CastOnUnit(target,UsePacket);
						}
						else
						{
							if (LastCast == "Q")
							{
								if (Q.IsReady()) Q.CastOnUnit(target ,UsePacket);
								if (W.IsReady()) W.CastOnUnit(target ,UsePacket);
								if (!W.IsReady()) E.CastOnUnit(target ,UsePacket);
								if (!W.IsReady() && !E.IsReady() && UseR) R.Cast(UsePacket);
							}
							else
								if (Q.IsReady()) Q.CastOnUnit(target ,UsePacket);
						}
					}
					else
					{
						if (Q.IsReady()) Q.CastOnUnit(target ,UsePacket);
						else if (R.IsReady() && UseR) R.Cast(UsePacket);
						else if (E.IsReady()) E.CastOnUnit(target ,UsePacket);
						else if (W.IsReady()) W.CastOnUnit(target ,UsePacket);
					}
				}
			}
		}
		
		private static void ComboBurst()
		{
			var UseR = Config.Item("UseR").GetValue<bool>();
			var UseIgnite = Config.Item("UseIgnite").GetValue<bool>();
			var UsePacket = Config.Item("UsePacket").GetValue<bool>();
			if (target == null) return;
			
			if (UseIgnite && IgniteKillable(target))
				if (IgniteSlot != SpellSlot.Unknown && Player.SummonerSpellbook.CanUseSpell(IgniteSlot) == SpellState.Ready && Vector3.Distance(Player.Position, target.Position) <= 600)
					Player.SummonerSpellbook.CastSpell(IgniteSlot, target);
			
			if (Environment.TickCount - LastFlashTime < 1 && W.IsReady()) W.CastOnUnit(target ,UsePacket);
			else
			{
				if (Q.IsKillable(target) && Q.IsReady()) Q.CastOnUnit(target,UsePacket);
				else if (E.IsKillable(target) && E.IsReady()) E.CastOnUnit(target,UsePacket);
				else if (W.IsKillable(target) && W.IsReady()) W.CastOnUnit(target,UsePacket);
				else if (Vector3.Distance(Player.Position, target.Position) >= 575 && !target.IsFacing(Player) && W.IsReady()) W.CastOnUnit(target,UsePacket);
				else
				{
					if (Q.IsReady()) Q.CastOnUnit(target ,UsePacket);
					else if (R.IsReady() && UseR) R.Cast(UsePacket);
					else if (E.IsReady()) E.CastOnUnit(target ,UsePacket);
					else if (W.IsReady()) W.CastOnUnit(target ,UsePacket);
				}
			}
		}
		
		private static void ComboLong()
		{
			var UseR = Config.Item("UseR").GetValue<bool>();
			var UseIgnite = Config.Item("UseIgnite").GetValue<bool>();
			var UsePacket = Config.Item("UsePacket").GetValue<bool>();
			if (target == null) return;
			
			if (UseIgnite && IgniteKillable(target))
				if (IgniteSlot != SpellSlot.Unknown && Player.SummonerSpellbook.CanUseSpell(IgniteSlot) == SpellState.Ready && Vector3.Distance(Player.Position, target.Position) <= 600)
					Player.SummonerSpellbook.CastSpell(IgniteSlot, target);
			
			if (Environment.TickCount - LastFlashTime < 1 && W.IsReady()) W.CastOnUnit(target ,UsePacket);
			else
			{
				if (Q.IsKillable(target) && Q.IsReady()) Q.CastOnUnit(target,UsePacket);
				else if (E.IsKillable(target) && E.IsReady()) E.CastOnUnit(target,UsePacket);
				else if (W.IsKillable(target) && W.IsReady()) W.CastOnUnit(target,UsePacket);
				else if (Vector3.Distance(Player.Position, target.Position) >= 575 && !target.IsFacing(Player) && W.IsReady()) W.CastOnUnit(target,UsePacket);
				else
				{
					if (Player.CountEnemysInRange(300) > 1)
					{
						if (LastCast == "Q")
						{
							if (Q.IsReady()) Q.CastOnUnit(target ,UsePacket);
							if (R.IsReady() && UseR) R.Cast(UsePacket);
							if (!R.IsReady()) W.CastOnUnit(target ,UsePacket);
							if (!R.IsReady() && !W.IsReady()) E.CastOnUnit(target ,UsePacket);
						}
						else Q.CastOnUnit(target,UsePacket);
					}
					else
					{
						if (LastCast == "Q")
						{
							if (Q.IsReady()) Q.CastOnUnit(target ,UsePacket);
							if (W.IsReady()) W.CastOnUnit(target ,UsePacket);
							if (!W.IsReady()) E.CastOnUnit(target ,UsePacket);
							if (!W.IsReady() && !E.IsReady() && R.IsReady() && UseR) R.Cast(UsePacket);
						}
						else
							if (Q.IsReady()) Q.CastOnUnit(target ,UsePacket);
					}
				}
			}
		}
		
		private static void Harass()
		{
			var UseQ = Config.Item("HQ").GetValue<bool>();
			var UseW = Config.Item("HW").GetValue<bool>();
			var UseE = Config.Item("HE").GetValue<bool>();
			var UsePacket = Config.Item("UsePacket").GetValue<bool>();
			if (Vector3.Distance(Player.Position, target.Position) <= 625 )
			{
				if (UseQ && Q.IsReady()) Q.CastOnUnit(target,UsePacket);
				if (UseW && W.IsReady()) W.CastOnUnit(target,UsePacket);
				if (UseE && E.IsReady()) E.CastOnUnit(target,UsePacket);
			}
		}
		
		private static void Farm()
		{
			var UseQ = Config.Item("FQ").GetValue<bool>();
			var UseW = Config.Item("FW").GetValue<bool>();
			var UseE = Config.Item("FE").GetValue<bool>();
			var UsePacket = Config.Item("UsePacket").GetValue<bool>();
			var allMinions = MinionManager.GetMinions(Player.Position, Q.Range,MinionTypes.All,MinionTeam.All, MinionOrderTypes.MaxHealth);
			if (Config.Item("FreezeActive").GetValue<KeyBind>().Active)
			{
				if (UseQ && Q.IsReady())
				{
					foreach (var minion in allMinions)
					{
						if (minion.IsValidTarget() && Q.IsKillable(minion))
						{
							Q.CastOnUnit(minion,UsePacket);
							return;
						}
					}
				}
				else if (UseW && W.IsReady())
				{
					foreach (var minion in allMinions)
					{
						if (minion.IsValidTarget(W.Range) && W.IsKillable(minion))
						{
							W.CastOnUnit(minion,UsePacket);
							return;
						}
					}
				}
				else if (UseE && E.IsReady())
				{
					foreach (var minion in allMinions)
					{
						if (minion.IsValidTarget(E.Range) && E.IsKillable(minion))
						{
							E.CastOnUnit(minion,UsePacket);
							return;
						}
					}
				}
			}
			else if (Config.Item("LaneClearActive").GetValue<KeyBind>().Active)
			{
				foreach (var minion in allMinions)
				{
					if (UseQ && Q.IsReady()) Q.CastOnUnit(minion,UsePacket);
					if (UseW && W.IsReady()) W.CastOnUnit(minion,UsePacket);
					if (UseE && E.IsReady()) E.CastOnUnit(minion,UsePacket);
				}
			}
		}
		
		private static void JungleFarm()
		{
			var UseQ = Config.Item("JQ").GetValue<bool>();
			var UseW = Config.Item("JW").GetValue<bool>();
			var UseE = Config.Item("JE").GetValue<bool>();
			var UsePacket = Config.Item("UsePacket").GetValue<bool>();
			var jungminions = MinionManager.GetMinions(Player.Position, Q.Range,MinionTypes.All,MinionTeam.Neutral, MinionOrderTypes.MaxHealth);
			if (jungminions.Count > 0)
			{
				var minion = jungminions[0];
				if (UseQ && Q.IsReady()) Q.CastOnUnit(minion,UsePacket);
				if (UseW && W.IsReady()) W.CastOnUnit(minion,UsePacket);
				if (UseE && E.IsReady()) E.CastOnUnit(minion,UsePacket);
			}
		}
		
		private static void KillSteal()
		{
			var AutoIgnite = Config.Item("AutoIgnite").GetValue<bool>();
			var KillSteal = Config.Item("KillSteal").GetValue<bool>();
			var UsePacket = Config.Item("UsePacket").GetValue<bool>();
			if (AutoIgnite && IgniteSlot != SpellSlot.Unknown && Player.SummonerSpellbook.CanUseSpell(IgniteSlot) == SpellState.Ready)
			{
				foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => Vector3.Distance(Player.Position, target.Position) <= 600 && enemy.IsEnemy && enemy.IsVisible && !enemy.IsDead && IgniteKillable(enemy)))
					Player.SummonerSpellbook.CastSpell(IgniteSlot, enemy);
			}
			if (KillSteal & (Q.IsReady() || W.IsReady() || E.IsReady()))
			{
				foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => Vector3.Distance(Player.Position, target.Position) <= Q.Range && enemy.IsEnemy && enemy.IsVisible && !enemy.IsDead))
				{
					if (Q.IsReady() && Q.IsKillable(target)) Q.CastOnUnit(enemy,UsePacket);
					if (W.IsReady() && W.IsKillable(target)) W.CastOnUnit(enemy,UsePacket);
					if (E.IsReady() && E.IsKillable(target)) E.CastOnUnit(enemy,UsePacket);
				}
				
			}
		}
	}
}