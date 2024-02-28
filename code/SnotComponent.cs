using Sandbox;

public sealed class SnotComponent : Component
{
	[Property]
	public UnitInfo Info { get; set; }

	[Property]
	public SkinnedModelRenderer Model { get; set; }

	protected override void OnStart()
	{
		// subscribe to when the UnitInfo component says it takes damage so we can control the animation from that value
		if ( Info != null )
		{
			Info.OnDamage += HurtAnimation;
			Info.OnDeath += DeathAnimation;
		}
	}

	protected override void OnUpdate()
	{
		if (Info != null && Model != null)
		{
			// don't continue to shrink it during death animation
			if ( !Info.Alive ) return;

			// I guess this is a way to ask the Animgraph for the current float value of its "health" property
			var currentHealth = Model.GetFloat( "health" );
			// health is scaled in Animgraph from 0-100
			var scaledHealth = Info.Health / Info.MaxHealth * 100f;

			// we want to do some interpolation between the animation's current health value and the actual health
			// of the snot. we do this by a fraction of time, which is however long it took since last frame divided by 0.1 (essentially multiplying by 10)
			var lerpedHealth = MathX.Lerp( currentHealth, scaledHealth, Time.Delta / 0.1f );
			Model.Set( "health", lerpedHealth );
		}
	}

	public void HurtAnimation( float damage )
	{
		// snot animation values in Animgraph are 0-100, we wanna scale how much damage it took as a percentage of
		// the snot's max health
		var scaledDamage = damage / Info.MaxHealth * 100f;

		if (Model != null)
		{
			Model.Set( "damage", scaledDamage );
			Model.Set( "hit", true );
		}
	}

	public void DeathAnimation()
	{
		if (Model != null)
		{
			Model.Set( "dead", true );
		}
	}
}
