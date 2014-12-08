#region
using System;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using Color = System.Drawing.Color;
using SharpDX;
using LeagueSharp;
using LeagueSharp.Common;
#endregion

namespace JungleTimer
{
	internal class Program
	{
		private const float CheckInterval = 25f;

		private static readonly List<Camp> _camps = new List<Camp>();
		private static readonly IList<DrawText> _DrawText = new List<DrawText>();
		private static float _lastCheck = Environment.TickCount;

		private static void Main(string[] args)
		{
			CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
		}
		
		private static bool Initialize()
		{
			if (Utility.Map.GetMap()._MapType == Utility.Map.MapType.SummonersRift)
			{
				// Blue: Blue Buff
				_camps.Add(new Camp(new Vector3(3388.2f, 8400f, 55.2f), 1, 300f));

				// Blue: Wolves
				_camps.Add(new Camp(new Vector3(3415.8f, 6950f, 55.6f), 2, 100f));

				// Blue: Chicken
				_camps.Add(new Camp(new Vector3(6500f, 5900f, 60f), 3, 100f));

				// Blue: Red Buff
				_camps.Add(new Camp(new Vector3(7300.4f, 4600.1f, 56.9f), 4, 300f));

				// Blue: Krug
				_camps.Add(new Camp(new Vector3(7700.2f, 3200f, 54.3f), 5, 100f));

				// Blue: Gromp
				_camps.Add(new Camp(new Vector3(1900.1f, 9200f, 54.9f), 13, 100f));

				// Red: Blue Buff
				_camps.Add(new Camp(new Vector3(10440f, 7500f, 54.9f), 7, 300f));

				// Red: Wolves
				_camps.Add(new Camp(new Vector3(10350f, 9000f, 65.5f), 8, 100f));

				// Red: Chicken
				_camps.Add(new Camp(new Vector3(7100f, 10000f, 55.5f), 9, 100f));

				// Red: Red Buff
				_camps.Add(new Camp(new Vector3(6450.2f, 11400f, 54.6f), 10, 300f));

				// Red: Krug
				_camps.Add(new Camp(new Vector3(6005f, 13000f, 39.6f), 11, 100f));

				// Red: Gromp
				_camps.Add(new Camp(new Vector3(12000f, 7000f, 54.8f), 14, 100f));

				// Neutral: Dragon
				_camps.Add(new Camp(new Vector3(9300.8f, 4200.5f, -60.3f), 6, 360f));

				// Neutral: Baron
				_camps.Add(new Camp(new Vector3(4300.1f, 11600.7f, -63.1f), 12, 420f));
				
				// Dragon: Crab
				_camps.Add(new Camp(new Vector3(10600f, 5600.5f, -60.3f), 15, 180f));
				
				// Baron: Crab
				_camps.Add(new Camp(new Vector3(4200.1f, 9900.7f, -63.1f), 16, 180f));
			}
			else if (Utility.Map.GetMap()._MapType == Utility.Map.MapType.TwistedTreeline)
			{
				// Blue: Wraiths
				_camps.Add(new Camp(new Vector3(3550f, 6250f, 60f), 1, 50f));

				// Blue: Golems
				_camps.Add(new Camp(new Vector3(4500f, 8550f, 60f), 2, 50f));

				// Blue: Wolves
				_camps.Add(new Camp(new Vector3(5600f, 6400f, 60f), 3, 50f));

				// Red: Wraiths
				_camps.Add(new Camp(new Vector3(10300f, 6250f, 60f), 4, 50f));

				// Red: Golems
				_camps.Add(new Camp(new Vector3(9800f, 8550f, 60f), 5, 50f));

				// Red: Wolves
				_camps.Add(new Camp(new Vector3(8600f, 6400f, 60f), 6, 50f));

				// Neutral: Vilemaw
				_camps.Add(new Camp(new Vector3(7150f, 11100f, 60f), 8, 300f));
			}

			if (_camps.Count > 0)
			{
				foreach (var camp in _camps)
				{
					DrawText pos = new DrawText(camp);
					_DrawText.Add(pos);
				}
				return true;
			}
			else return false;
		}
		
		private static void Game_OnGameLoad(EventArgs args)
		{
			if (!Initialize()) return;
						
			Game.PrintChat("JungleTimer loaded!");

			Game.OnGameUpdate += Game_OnGameUpdate;
			Game.OnGameProcessPacket += Game_OnGameProcessPacket;
			Drawing.OnEndScene += Drawing_OnEndScene;
		}
				
		private static void Game_OnGameUpdate(EventArgs args)
		{
			if (_lastCheck + CheckInterval > Environment.TickCount) return;
			
			_lastCheck = Environment.TickCount;

			foreach (Camp camp in _camps.Where(camp => (camp.NextRespawnTime - Game.Time) < 0f))
			{
				camp.NextRespawnTime = 0f;
			}
		}
		
		private static void Game_OnGameProcessPacket(GamePacketEventArgs args)
		{
			if (args.PacketData[0] == Packet.S2C.EmptyJungleCamp.Header)
			{
				var packet = Packet.S2C.EmptyJungleCamp.Decoded(args.PacketData);
				var camp = _camps.FirstOrDefault(c => c.Id == packet.CampId);
				if (packet.UnitNetworkId != 0 && !Equals(camp, default(Camp)))
				{
					if (packet.EmptyType != 3)
					{
						camp.NextRespawnTime = Game.Time + camp.RespawnTime;
					}
				}
			}
		}
		
		private static void Drawing_OnEndScene(EventArgs args)
		{
			foreach (var Texts in _DrawText)
			{
				foreach (var camp in _camps)
				{
					if (camp.NextRespawnTime - Game.Time > 0 && !(camp.NextRespawnTime <= 0f) && Texts.Camps.Id == camp.Id)
					{
						Texts.Text.OnEndScene();
					}
				}
			}
		}
		
		private class Camp
		{

			public readonly int Id;
			public readonly Vector3 Position;
			public readonly float RespawnTime;
			public float NextRespawnTime;

			public Camp(Vector3 position, int id, float respawnTime)
			{
				Position = position;
				Id = id;
				RespawnTime = respawnTime;
			}
		}
		
		private class DrawText
		{
			private static int _layer;
			public Render.Text Text { get; set; }
			public Camp Camps;
			public DrawText(Camp pos)
			{
				Text = new Render.Text(Drawing.WorldToMinimap(pos.Position),"",15,SharpDX.Color.White)
				{
					VisibleCondition = sender => ((int) (pos.NextRespawnTime - Game.Time)) > 0 && !(pos.NextRespawnTime <= 0f),
					TextUpdate = () => ((int) (pos.NextRespawnTime - Game.Time)).ToString(CultureInfo.InvariantCulture),
				};
				Camps = pos;
				Text.Add(_layer);
				_layer++;
			}
		}
		

	}
	
}