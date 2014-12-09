#region
using LeagueSharp;
using SharpDX;
#endregion

namespace WhereDidHeGo
{
	public class SpellData
	{
		public string Name;
		public int MaxRange;
		public float Radius;
		public float Delay;
		public bool Casted;
		public float TimeCasted;
		public Vector3 StartPos;
		public Vector3 EndPos;
		public Obj_AI_Hero CastingHero;
		public string ShortName;
		public bool OutOfBush;
		public bool TargetDead;
		public GameObject Target;
		
		public SpellData(string name,
		                 int maxRange,
		                 float radius,
		                 float delay,
		                 bool casted,
		                 float timeCasted,
		                 Obj_AI_Hero castingHero,
		                 string shortName)
		{
			Name = name;
			MaxRange = maxRange;
			Radius = radius;
			Delay = delay;
			Casted = casted;
			TimeCasted = timeCasted;
			CastingHero = castingHero;
			ShortName = shortName;
		}
	}
}