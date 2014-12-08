using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace RecallTracker_ProFlash 
{
    class Program 
    {
        static void Main(string[] args) 
        {
            Game.OnGameProcessPacket += Game_OnGameProcessPacket;
            Drawing.OnDraw += Drawing_OnDraw;
            RecallList = new List<Recall>();
            Game.PrintChat("Recall Tracker + ProFlash loaded!!!");
        }
        
        static void Drawing_OnDraw(EventArgs args) 
        {
           	var index = -1;
            foreach (Recall r in RecallList)
            {
                if (!r.update()) 
                {
                	index++;
                    if (r.LastAction == Recall.RecallState.Recalling) 
                        Drawing.DrawText(Drawing.Width * 0.683f, Drawing.Height * 0.88f + (index * 15f), System.Drawing.Color.Beige, r.ToString());
                    else if (r.LastAction == Recall.RecallState.Ported)
                        Drawing.DrawText(Drawing.Width * 0.683f, Drawing.Height * 0.88f + (index * 15f), System.Drawing.Color.GreenYellow, r.ToString());
                    else if (r.LastAction == Recall.RecallState.Cancelled)
                        Drawing.DrawText(Drawing.Width * 0.683f, Drawing.Height * 0.88f + (index * 15f), System.Drawing.Color.Red, r.ToString());               
                }
            }
        }

        static List<Recall> RecallList;
        
        static readonly List<byte> SummonerByte = new List<byte> { 0xE9, 0xEF, 0x8B, 0xED, 0x63 };

        static void Game_OnGameProcessPacket(GamePacketEventArgs args) 
        {
            if (args.PacketData[0] == 0xD8 || args.PacketData[0] == 0xD7) 
            {
                var stream = new System.IO.MemoryStream(args.PacketData);
                var byteRead = new System.IO.BinaryReader(stream);

                byteRead.ReadByte();
                byteRead.ReadBytes(4);
                var netidbytes = byteRead.ReadBytes(4);
                var networkId = System.BitConverter.ToInt32(netidbytes, 0);

                byteRead.ReadBytes(0x42);
                string s = System.BitConverter.ToString(byteRead.ReadBytes(6));
                var state = Recall.RecallState.Recalling;
                if (string.Equals("00-00-00-00-00-00", s)) state = Recall.RecallState.Unknown;

                byteRead.Close();

                var unit = ObjectManager.GetUnitByNetworkId<GameObject>(networkId);
                if (unit == null || !unit.IsValid) return;
                if (unit.Team == ObjectManager.Player.Team) return;
                
                HandleRecall((Obj_AI_Hero)unit, state);
            }
            if (Packet.C2S.Cast.Header != args.PacketData[0]) return;
                    

                var packet = new GamePacket(args.PacketData);
                var summoner = SummonerByte.Contains(packet.ReadByte(5));
                var slot = (SpellSlot)packet.ReadByte();
                var flash = ObjectManager.Player.SummonerSpellbook.Spells.SingleOrDefault(s => s.Name == "summonerflash");

                if (flash != null)
                {
                    var flashSlot = flash.Slot;
                    var flashPacket = Packet.C2S.Cast.Decoded(args.PacketData);

                    if (summoner && slot == flashSlot && flashPacket.SourceNetworkId == ObjectManager.Player.NetworkId)
                    {
                        var to = new Vector2(flashPacket.ToX, flashPacket.ToY);
                        if (ObjectManager.Player.ServerPosition.To2D().Distance(to) < 390)
                        {
                            args.Process = false;

                            var maxRange = ObjectManager.Player.ServerPosition.To2D().Extend(to, 400);
                            flashPacket.FromX = maxRange.X;
                            flashPacket.FromY = maxRange.Y;
                            flashPacket.ToX = maxRange.X;
                            flashPacket.ToY = maxRange.Y;

                            Game.PrintChat("- ProFlash -");
                            Packet.C2S.Cast.Encoded(flashPacket).Send(args.Channel, args.ProtocolFlag);
                        }
                    }
                }
        }
        
        public static void HandleRecall(Obj_AI_Hero hero, Recall.RecallState recallType) {
            Recall recall = null;
            recall = RecallList.Where(x => x.Player.NetworkId == hero.NetworkId).FirstOrDefault();
            if (recall == null) 
            {
                recall = new Recall(hero);
                recall.LastAction = recallType;
                RecallList.Add(recall);
            } 
            else recall.LastAction = recallType;
        }
    }
}
