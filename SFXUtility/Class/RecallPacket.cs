namespace SFXUtility.Class
{
	using System;
	using System.Collections.Generic;
	using LeagueSharp.Common;
	using LeagueSharp;
	
	public static class RecallPacket
	{
		public enum ObjectType
		{
			Player,
			Turret,
			Minion,
			Ward,
			Object
		}
		public enum RecallStatus
		{
			RecallStarted,
			RecallAborted,
			RecallFinished,
			Unknown,
			TeleportStart,
			TeleportAbort,
			TeleportEnd,
		}
		public static byte Header = 0xD8;
		public static readonly Dictionary<int, int> RecallT = new Dictionary<int, int>();
		public static readonly Dictionary<int, int> TPT = new Dictionary<int, int>();
		public static GamePacket Encoded(Struct packetStruct)
		{
			//TODO when the packet is fully decoded.
			return new GamePacket(Header);
		}
		public static Struct Decoded(byte[] data)
		{
			var packet = new GamePacket(data);
			var result = new Struct();
			result.UnitNetworkId = packet.ReadInteger(5);
			var type = packet.ReadString(75);
			result.Status = RecallStatus.Unknown;
			var gObject = ObjectManager.GetUnitByNetworkId<GameObject>(result.UnitNetworkId);
			if (gObject == null || !gObject.IsValid)
			{
				return result;
			}
			if (gObject is Obj_AI_Hero)
			{
				var unit = (Obj_AI_Hero) gObject;
				if (!unit.IsValid || unit.Spellbook.GetSpell(SpellSlot.Recall) == null)
				{
					return result;
				}
				result.Type = ObjectType.Player;
				var duration = Utility.GetRecallTime(unit);
				result.Duration = duration;
				if (!RecallT.ContainsKey(result.UnitNetworkId))
				{
					RecallT.Add(result.UnitNetworkId, 0);
				}
				if (!TPT.ContainsKey(result.UnitNetworkId))
				{
					TPT.Add(result.UnitNetworkId, 0);
				}
				if (type == "Teleport")
				{
					TPT[result.UnitNetworkId] = Environment.TickCount;
					result.Status = RecallStatus.TeleportStart;
				}
				else if (type == "Recall")
				{
					result.Status = RecallStatus.RecallStarted;
					RecallT[result.UnitNetworkId] = Environment.TickCount;
				}
				else if (string.IsNullOrEmpty(type))
				{
					if (Environment.TickCount - RecallT[result.UnitNetworkId] < duration - 200)
					{
						result.Status = RecallStatus.RecallAborted;
					}
					else if (Environment.TickCount - RecallT[result.UnitNetworkId] < duration + 200)
					{
						result.Status = RecallStatus.RecallFinished;
					}
					if (Environment.TickCount - TPT[result.UnitNetworkId] < 3500)
					{
						result.Status = RecallStatus.TeleportAbort;
					}
					else if (Environment.TickCount - TPT[result.UnitNetworkId] < 4500)
					{
						result.Status = RecallStatus.TeleportEnd;
					}
				}
			}
			else if (gObject is Obj_AI_Turret)
			{
				result.Type = ObjectType.Turret;
				result.Status = string.IsNullOrEmpty(type)
					? RecallStatus.TeleportEnd
					: RecallStatus.TeleportStart;
			}
			else if (gObject is Obj_AI_Minion)
			{
				result.Type = ObjectType.Object;
				if (gObject.Name.Contains("Minion"))
				{
					result.Type = ObjectType.Minion;
				}
				if (gObject.Name.Contains("Ward"))
				{
					result.Type = ObjectType.Ward;
				}
				result.Status = string.IsNullOrEmpty(type)
					? RecallStatus.TeleportEnd
					: RecallStatus.TeleportStart;
			}
			else
			{
				result.Type = ObjectType.Object;
			}
			return result;
		}
		public struct Struct
		{
			public int Duration;
			public RecallStatus Status;
			public ObjectType Type;
			public int UnitNetworkId;
			public Struct(int unitNetworkId, RecallStatus status, ObjectType type, int duration)
			{
				UnitNetworkId = unitNetworkId;
				Status = status;
				Type = type;
				Duration = duration;
			}
		}
	}
}