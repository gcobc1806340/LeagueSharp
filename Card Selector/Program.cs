#region
using System;
using System.Collections.Generic;
using Color = System.Drawing.Color;
using System.Linq;
using SharpDX;
using LeagueSharp;
using LeagueSharp.Common;
#endregion

namespace CardSelector
{
    internal class Program
    {
        private static Menu Config;
        private static Obj_AI_Hero myHero;
        private static Vector2 PingLocation;
        private static int LastPingT;
		
        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }
                       
        private static void Game_OnGameLoad(EventArgs args)
        {
        	myHero = ObjectManager.Player;
        	
           	if (myHero.ChampionName != "TwistedFate") return;
			
			LastPingT = 0;
          			
			Config = new Menu("Card Selector", "Card Selector", true);
			
			Config.AddSubMenu(new Menu("Card Selector", "CardSelector"));
			Config.SubMenu("CardSelector").AddItem(new MenuItem("Yellow", "Yellow!").SetValue(new KeyBind("W".ToCharArray()[0], KeyBindType.Press)));
			Config.SubMenu("CardSelector").AddItem(new MenuItem("Blue", "Blue!").SetValue(new KeyBind("E".ToCharArray()[0], KeyBindType.Press)));
			Config.SubMenu("CardSelector").AddItem(new MenuItem("Red", "Red!").SetValue(new KeyBind("T".ToCharArray()[0], KeyBindType.Press)));
			Config.SubMenu("CardSelector").AddItem(new MenuItem("Ping", "Ping low health enemies").SetValue(true));
			
			Config.AddSubMenu(new Menu("Drawings", "Drawings"));
			Config.SubMenu("Drawings").AddItem(new MenuItem("QRange", "Q range").SetValue(new Circle(true, Color.FromArgb(255, 255, 255, 255))));
			Config.SubMenu("Drawings").AddItem(new MenuItem("WRange", "W range").SetValue(new Circle(false, Color.FromArgb(255, 255, 255, 255))));
			Config.SubMenu("Drawings").AddItem(new MenuItem("RRange", "R range").SetValue(new Circle(false, Color.FromArgb(255, 255, 255, 255))));				
			Config.AddToMainMenu();       
			
			Game.PrintChat("Card Selector loaded!");

			Game.OnGameUpdate += Game_OnGameUpdate;
			Drawing.OnEndScene += DrawingOnOnEndScene;
			Drawing.OnDraw += Drawing_OnDraw;	
        }
        
        private static void Ping(Vector2 position)
        {
            if (Environment.TickCount - LastPingT < 30 * 1000) return;
            LastPingT = Environment.TickCount;
            PingLocation = position;
            SimplePing();
            Utility.DelayAction.Add(150, SimplePing);
            Utility.DelayAction.Add(300, SimplePing);
			Utility.DelayAction.Add(450, SimplePing);
        }

        private static void SimplePing()
        {
 			Packet.S2C.Ping.Encoded(new Packet.S2C.Ping.Struct(PingLocation.X, PingLocation.Y, 0, 0, Packet.PingType.Fallback)).Process();
        }
        
        private static bool Killable(Obj_AI_Hero hero)
        {   
            var dmg = 0d;
            dmg += myHero.GetSpellDamage(hero, SpellSlot.Q) * 2;
            dmg += myHero.GetSpellDamage(hero, SpellSlot.W);

            if (Items.HasItem("ItemBlackfireTorch"))
            {
                dmg += myHero.GetItemDamage(hero, Damage.DamageItems.Dfg);
                dmg = dmg * 1.2;
            }

            if(myHero.GetSpellSlot("SummonerIgnite") != SpellSlot.Unknown)
            {
                dmg += myHero.GetSummonerSpellDamage(hero, Damage.SummonerSpell.Ignite);
            }
            
            if (dmg > hero.Health) return true;
            else return false;
        }
                          
        private static void Game_OnGameUpdate(EventArgs args)
        {     
        	if (Config.Item("Ping").GetValue<bool>())
        		foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(h => myHero.Spellbook.CanUseSpell(SpellSlot.R) == SpellState.Ready && h.IsValidTarget() && Killable(h)))
                {
                    Ping(enemy.Position.To2D());
                }
        	
        	if (Config.Item("Yellow").GetValue<KeyBind>().Active)
            {
                CardSelector.StartSelecting(Cards.Yellow);
            }

            if (Config.Item("Blue").GetValue<KeyBind>().Active)
            {
                CardSelector.StartSelecting(Cards.Blue);
            }

            if (Config.Item("Red").GetValue<KeyBind>().Active)
            {
                CardSelector.StartSelecting(Cards.Red);
            }			
        }
        
        private static void DrawingOnOnEndScene(EventArgs args)
        {
            var drawR = Config.Item("RRange").GetValue<Circle>();
            if (drawR.Active && !myHero.IsDead)
            {
                Utility.DrawCircle(ObjectManager.Player.Position, 5500, drawR.Color, 1, 23, true);
            }
        }
                    		
 		private static void Drawing_OnDraw(EventArgs args)
        {
        	var drawQ = Config.Item("QRange").GetValue<Circle>();
            if (drawQ.Active && !myHero.IsDead)
            {
                Utility.DrawCircle(myHero.Position, 1450, drawQ.Color);
            }

            var drawW = Config.Item("WRange").GetValue<Circle>();
            if (drawW.Active && !myHero.IsDead)
            {
                Utility.DrawCircle(myHero.Position, 700, drawW.Color);
            }                       
        } 		
    }
}