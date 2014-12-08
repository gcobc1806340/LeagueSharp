#region License

/*
 Copyright 2014 - 2014 Nikita Bernthaler
 JungleTimer.cs is part of SFXUtility.
 
 SFXUtility is free software: you can redistribute it and/or modify
 it under the terms of the GNU General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.
 
 SFXUtility is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 GNU General Public License for more details.
 
 You should have received a copy of the GNU General Public License
 along with SFXUtility. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

namespace SFXUtility.Feature
{
    #region

    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using Class;
    using IoCContainer;
    using LeagueSharp;
    using LeagueSharp.Common;
    using SharpDX;
    using Color = System.Drawing.Color;
    using Utilities = Class.Utilities;

    #endregion

    internal class JungleTimer : Base
    {
        #region Fields

        private const float CheckInterval = 25f;

        private readonly List<Camp> _camps = new List<Camp>();
        private readonly IList<DrawText> _DrawText = new List<DrawText>();
        private float _lastCheck = Environment.TickCount;
        private Timers _timers;

        #endregion

        #region Constructors

        public JungleTimer(IContainer container)
            : base(container)
        {
            CustomEvents.Game.OnGameLoad += OnGameLoad;
        }

        #endregion

        #region Properties

        public override bool Enabled
        {
            get
            {
                return _timers != null && _timers.Menu != null &&
                       _timers.Menu.Item(_timers.Name + "Enabled").GetValue<bool>() && Menu != null &&
                       Menu.Item(Name + "Enabled").GetValue<bool>();
            }
        }

        public override string Name
        {
            get { return "Jungle"; }
        }

        #endregion

        #region Methods

//        private void OnDraw(EventArgs args)
//        {
//            try
//            {
//                if (!Enabled)
//                    return;
//
//                foreach (Camp camp in _camps.Where(camp => !(camp.NextRespawnTime <= 0f)))
//                {
//                    Utilities.DrawTextCentered(Drawing.WorldToMinimap(camp.Position),
//                        Menu.Item(Name + "DrawingColor").GetValue<Color>(),
//                        ((int) (camp.NextRespawnTime - Game.Time)).ToString(CultureInfo.InvariantCulture));
//                }
//            }
//            catch (Exception ex)
//            {
//                Logger.WriteBlock(ex.Message, ex.ToString());
//            }
//        }

		private void Drawing_OnEndScene(EventArgs args)
		{
			try
			{
				if (!Enabled) return;                   
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
			catch (Exception ex)
            {
                Logger.WriteBlock(ex.Message, ex.ToString());
            }
		}

        private void OnGameLoad(EventArgs args)
        {
            try
            {
                Logger.Prefix = string.Format("{0} - {1}", BaseName, Name);

                if (IoC.IsRegistered<Timers>() && IoC.Resolve<Timers>().Initialized)
                {
                    TimersLoaded(IoC.Resolve<Timers>());
                }
                else
                {
                    if (IoC.IsRegistered<Mediator>())
                    {
                        IoC.Resolve<Mediator>().Register("Timers_initialized", TimersLoaded);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteBlock(ex.Message, ex.ToString());
            }
        }

        private void OnGameProcessPacket(GamePacketEventArgs args)
        {
            try
            {
                if (!Enabled)
                    return;

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
            catch (Exception ex)
            {
                Logger.WriteBlock(ex.Message, ex.ToString());
            }
        }

        private void OnGameUpdate(EventArgs args)
        {
            try
            {
                if (!Enabled || _lastCheck + CheckInterval > Environment.TickCount)
                    return;

                _lastCheck = Environment.TickCount;

                foreach (Camp camp in _camps.Where(camp => (camp.NextRespawnTime - Game.Time) < 0f))
                {
                    camp.NextRespawnTime = 0f;
                }             
            }
            catch (Exception ex)
            {
                Logger.WriteBlock(ex.Message, ex.ToString());
            }
        }

        private void TimersLoaded(object o)
        {
            try
            {
                if (o is Timers && (o as Timers).Menu != null)
                {
                    _timers = (o as Timers);

                    Menu = new Menu(Name, Name);

                    Menu.AddItem(new MenuItem(Name + "Enabled", "Enabled").SetValue(true));

                    _timers.Menu.AddSubMenu(Menu);

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

                    if (Utility.Map.GetMap()._MapType == Utility.Map.MapType.TwistedTreeline)
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
                        Game.OnGameUpdate += OnGameUpdate;
                        Game.OnGameProcessPacket += OnGameProcessPacket;
//                      Drawing.OnDraw += OnDraw;
                        Drawing.OnEndScene += Drawing_OnEndScene;
                    }

                    Initialized = true;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteBlock(ex.Message, ex.ToString());
            }
        }

        #endregion

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