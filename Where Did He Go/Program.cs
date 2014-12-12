#region
using System;
using System.Collections.Generic;
using System.Linq;
using Color = System.Drawing.Color;
using SharpDX;
using LeagueSharp;
using LeagueSharp.Common;
#endregion

namespace WhereDidHeGo
{
	internal class Program
	{
		private static List<SpellData> Spells = new List<SpellData>();
		private static Menu Config;
		private static float vayneUltEndTick = 0;
		private static int shacoIndex = -1;
		
		private static void Main(string[] args)
		{
			CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
		}
		
		private static void Game_OnGameLoad(EventArgs args)
		{
			foreach (var hero in ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsValid && hero.IsEnemy))
			{
				if (hero.GetSpellSlot("summonerflash") != SpellSlot.Unknown)
					Spells.Add(new SpellData("summonerflash", 400, 0, 0, false, 0, hero, "Flash"));
				if (hero.ChampionName ==  "Aatrox")
					Spells.Add(new SpellData("AatroxQ", 650, 275, 1, false, 0, hero, "Q"));
				else if (hero.ChampionName ==  "Ahri")
					Spells.Add(new SpellData("AhriTumble", 450, 0, 0.6f, false, 0, hero, "R"));
				else if (hero.ChampionName == "Akali")
					Spells.Add(new SpellData("AkaliShadowDance", 800, 0, 0.5f, false, 0, hero, "R"));
				else if (hero.ChampionName == "Ezreal")
					Spells.Add(new SpellData("EzrealArcaneShift", 475, 0, 0, false, 0, hero, "E"));
				else if (hero.ChampionName == "Fiora")
					Spells.Add(new SpellData("FioraDance", 700, 0, 1, false, 0, hero, "R"));
				else if (hero.ChampionName == "Kassadin")
					Spells.Add(new SpellData("RiftWalk", 700, 0, 0, false, 0, hero, "R"));
				else if (hero.ChampionName == "Katarina")
					Spells.Add(new SpellData("KatarinaE", 700, 0, 0, false, 0, hero, "E"));
				else if (hero.ChampionName == "Khazix")
				{
					Spells.Add(new SpellData("KhazixE", 600, 0, 0.9f, false, 0, hero, "E"));
					Spells.Add(new SpellData("khazixelong", 900, 0, 1, false, 0, hero, "R"));
				}
				else if (hero.ChampionName == "Leblanc")
				{
					Spells.Add(new SpellData("LeblancSlide", 600, 0, 0.5f, false, 0, hero, "W"));
					Spells.Add(new SpellData("leblancslidereturn", 0, 0, 0, false, 0, hero, "W(R)"));
					Spells.Add(new SpellData("LeblancSlideM", 600, 0, 0.5f, false, 0, hero, "W+"));
					Spells.Add(new SpellData("leblancslidereturnm", 0, 0, 0, false, 0, hero, "W+(R)"));
				}
				else if (hero.ChampionName == "Lissandra")
					Spells.Add(new SpellData("LissandraE", 700, 0, 0, false, 0, hero, "E"));
				else if (hero.ChampionName == "MasterYi")
					Spells.Add(new SpellData("AlphaStrike", 600, 0, 0.9f, false, 0, hero, "Q"));
				else if (hero.ChampionName == "Shaco")
					Spells.Add(new SpellData("Deceive", 400, 0, 0, false, 0, hero, "Q"));
				else if (hero.ChampionName == "Talon")
					Spells.Add(new SpellData("TalonCutthroat", 700, 0, 0, false, 0, hero, "E"));
				else if (hero.ChampionName == "Tryndamere")
					Spells.Add(new SpellData("Slash", 600, 0, 0.9f, false, 0, hero, "E"));
				else if (hero.ChampionName == "Tristana")
					Spells.Add(new SpellData("RocketJump", 900, 200, 1.1f, false, 0, hero, "W"));
				else if (hero.ChampionName == "Vayne")
				{
					Spells.Add(new SpellData("VayneTumble", 250, 0, 0, false, 0, hero, "Q"));
					vayneUltEndTick = 1;
				}
				else if (hero.ChampionName == "Zac")
					Spells.Add(new SpellData("ZacE", 1550, 200, 1500, false, 0, hero, "E"));
				else if (hero.ChampionName == "Zed")
					Spells.Add(new SpellData("ZedShadowDash", 999, 0, 0, false, 0, hero, "W"));
			}
			if (Spells.Count > 0)
			{
				for (int i = 0; i < Spells.Count; i++)
				{
					if (Spells[i].Name == "Deceive") 
					{
						shacoIndex = i;
						Spells[i].OutOfBush = false;
					}
					else if (Spells[i].Name == "AlphaStrike" || Spells[i].Name == "FioraDance") Spells[i].TargetDead = false;
				}
				Config = new Menu("Where Did He Go", "Where Did He Go", true);
				Config.AddSubMenu(new Menu("Settings", "Settings"));
				Config.SubMenu("Settings").AddItem(new MenuItem("wallPrediction", "Use Wall Prediction").SetValue(false));
				Config.SubMenu("Settings").AddItem(new MenuItem("displayTime", "Display time (No Vision)").SetValue(new Slider(3,5,0)));
				Config.SubMenu("Settings").AddItem(new MenuItem("displayTimeVisible", "Display time (Vision)").SetValue(new Slider(3,5,0)));
				Config.AddToMainMenu();
				
				Game.PrintChat("Where Did He Go loaded!");
				
				Game.OnGameUpdate += Game_OnGameUpdate;
				Obj_AI_Base.OnProcessSpellCast += Obj_AI_Hero_OnProcessSpellCast;
				GameObject.OnCreate += Game_OnCreate;
				Drawing.OnDraw += Drawing_OnDraw;
			}
		}
		
		private static bool CheckP(Vector3 pos, float x0, float z0, float x, float y)
		{
			pos.X = x0 + x * 50;
			pos.Z = z0 + y * 50;
			return !pos.IsWall();
		}
		
		private static Vector3 FindNearestNonWall(Vector3 pos, float maxRadius)
		{
			if (!pos.IsWall()) return pos;
			maxRadius = (float)Math.Floor(maxRadius/50);
			float x0 = (float)Math.Round(pos.X/50) * 50;
			float z0 = (float)Math.Round(pos.Z/50) * 50;
			float radius = 1;
			while (radius <= maxRadius)
			{
				if (CheckP(pos,x0,z0,0,radius) || CheckP(pos,x0,z0,radius,0) || CheckP(pos,x0,z0,0,-radius) || CheckP(pos,x0,z0,-radius,0))
					return pos;
				float f = 1 - radius;
				float x = 0;
				float y = radius;
				while (x < y - 1)
				{
					x = x + 1;
					if (f < 0) f = f + 1 + 2 * x;
					else
					{
						y = y - 1;
						f = f + 1 + 2 * (x - y);
					}
					if (CheckP(pos,x0,z0,x,y) || CheckP(pos,x0,z0,-x,y) || CheckP(pos,x0,z0,x,-y) || CheckP(pos,x0,z0,-x,-y) ||
					    CheckP(pos,x0,z0,y,x) || CheckP(pos,x0,z0,-y,x) || CheckP(pos,x0,z0,y,-x) || CheckP(pos,x0,z0,-y,-x))
						return pos;
				}
				radius = radius + 1;
			}
			return new Vector3(0,0,0);
		}
		
		private static void SetNormalEndPosition(int i, GameObjectProcessSpellCastEventArgs args)
		{
			if (Vector3.Distance(args.Start,args.End) <= Spells[i].MaxRange)
				Spells[i].EndPos = args.End;
			else
			{
				Vector3 tEndPos = args.Start - Vector3.Normalize(args.Start - args.End) * Spells[i].MaxRange;
				Vector3 PosCh = FindNearestNonWall(tEndPos,1000);
				if (Config.Item("wallPrediction").GetValue<bool>() && PosCh.X != 0 && PosCh.Y != 0 && PosCh.Z != 0)
				{
					tEndPos = PosCh;
				}
				Spells[i].EndPos = tEndPos;
			}
		}
		
		private static void Obj_AI_Hero_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
		{
			if (sender.Type == GameObjectType.obj_AI_Hero && sender.IsEnemy && sender.IsValid)
			{
				if (vayneUltEndTick > 0 && args.SData.Name == "vayneinquisition")
				{
					vayneUltEndTick = ((float)Environment.TickCount/1000) + 6 + 2*args.Level;
					return;
				}				
				
				for (int i = 0; i < Spells.Count; i++)
				{
					if (args.SData.Name == Spells[i].Name && ((Obj_AI_Hero)sender).ChampionName == Spells[i].CastingHero.ChampionName)
					{
						if (Spells[i].Name == "VayneTumble" && ((float)Environment.TickCount/1000) >= vayneUltEndTick) return;
						if (Spells[i].Name == "Deceive") Spells[i].OutOfBush = false;
						if (Spells[i].Name == "LeblancSlideM")
						{
							Spells[i-2].Casted = false;
							Spells[i].StartPos = Spells[i-2].StartPos;
							SetNormalEndPosition(i,args);
						}
						else if (Spells[i].Name == "leblancslidereturn" || Spells[i].Name == "leblancslidereturnm")
						{
							if (Spells[i].Name == "leblancslidereturn")
							{
								Spells[i-1].Casted = false;
								Spells[i+1].Casted = false;
								Spells[i+2].Casted = false;
							}
							else
							{
								Spells[i-3].Casted = false;
								Spells[i-2].Casted = false;
								Spells[i-1].Casted = false;
							}
							Spells[i].StartPos = args.Start;
							Spells[i].EndPos = Spells[i-1].StartPos;
						}
						else if (Spells[i].Name == "FioraDance" || Spells[i].Name == "AlphaStrike")
						{
							Spells[i].Target = args.Target;
							Spells[i].TargetDead = false;
							Spells[i].StartPos = args.Start;
							Spells[i].EndPos = Spells[i].Target.Position;
						}
						else
						{
							Spells[i].StartPos = args.Start;
							SetNormalEndPosition(i,args);
						}
						Spells[i].Casted = true;
						Spells[i].TimeCasted = ((float)Environment.TickCount/1000);
						break;
					}
				}
			}			
		}
		
		private static void Game_OnCreate(GameObject sender, EventArgs args)
		{
			if (shacoIndex != -1 && sender.IsValid && sender.Name == "JackintheboxPoof2.troy" && !Spells[shacoIndex].Casted && sender.Type != GameObjectType.obj_LampBulb)
			{
				Spells[shacoIndex].StartPos = sender.Position;
				Spells[shacoIndex].EndPos = sender.Position;
				Spells[shacoIndex].Casted = true;
				Spells[shacoIndex].TimeCasted = ((float)Environment.TickCount/1000);
				Spells[shacoIndex].OutOfBush = true;
			}
		}
		
		private static void Game_OnGameUpdate(EventArgs args)
		{
			for (int i = 0; i < Spells.Count; i++)
			{
				if (Spells[i].Casted)
				{
					int timenovis = Config.Item("displayTime").GetValue<Slider>().Value;
					int timevis = Config.Item("displayTimeVisible").GetValue<Slider>().Value;
					if ((Spells[i].Name == "FioraDance" || Spells[i].Name == "AlphaStrike") && !Spells[i].TargetDead)
					{
						if (((float)Environment.TickCount/1000) > Spells[i].TimeCasted + Spells[i].Delay + 0.2f) Spells[i].Casted = false;
						else if (Spells[i].Target.IsDead)
						{
							Vector3 tempPos = Spells[i].EndPos;
							Spells[i].EndPos = Spells[i].StartPos;
							Spells[i].StartPos = tempPos;
							Spells[i].TargetDead = true;
						}
						else Spells[i].EndPos = Spells[i].Target.Position;
					}
					else if (Spells[i].CastingHero.IsDead || (!Spells[i].CastingHero.IsVisible && ((float)Environment.TickCount/1000) > Spells[i].TimeCasted + timenovis + Spells[i].Delay) ||
					         (Spells[i].CastingHero.IsVisible && ((float)Environment.TickCount/1000) > Spells[i].TimeCasted + timevis + Spells[i].Delay))
						Spells[i].Casted = false;
					else if (!Spells[i].OutOfBush && Spells[i].CastingHero.IsVisible && ((float)Environment.TickCount/1000) > Spells[i].TimeCasted + Spells[i].Delay)
						Spells[i].EndPos = Spells[i].CastingHero.Position;
				}
			}
		}
		
		private static void Drawing_OnDraw(EventArgs args)
		{
			for (int i = 0; i < Spells.Count; i++)
			{
				if (Spells[i].Casted)
				{
					Vector2 lineStartPos = Drawing.WorldToScreen(Spells[i].StartPos);
					Vector2 lineEndPos = Drawing.WorldToScreen(Spells[i].EndPos);
					float size = 100;
					if (Spells[i].Radius > 0 && ((float)Environment.TickCount/1000) < Spells[i].TimeCasted + Spells[i].Delay) size = Spells[i].Radius;
					if (Spells[i].OutOfBush) Utility.DrawCircle(Spells[i].EndPos, Spells[i].MaxRange, Color.Red);
					else
					{
						Utility.DrawCircle(Spells[i].EndPos, size, Color.White);
						Drawing.DrawLine(lineStartPos,lineEndPos, 2.0f, Color.Blue);
					}
					int offset = 30;
					string infoText = Spells[i].CastingHero.ChampionName + " " + Spells[i].ShortName;
					Drawing.DrawLine(lineEndPos.X, lineEndPos.Y, lineEndPos.X + offset, lineEndPos.Y - offset, 1.0f, Color.Red);
					Drawing.DrawLine(lineEndPos.X + offset, lineEndPos.Y - offset, lineEndPos.X + offset + 6 * infoText.Length, lineEndPos.Y - offset, 1.0f, Color.Red);
					Drawing.DrawText(lineEndPos.X + offset + 1, lineEndPos.Y - offset, Color.Bisque,infoText);
				}
			}
		}
	}
}