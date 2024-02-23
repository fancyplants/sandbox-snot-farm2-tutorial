using Sandbox;
using Sandbox.Citizen;

public sealed class SnotPlayer : Component
{
	[Property]
	[Category( "Components" )]
	public GameObject Camera { get; set; }

	[Property]
	[Category( "Components" )]
	public CharacterController Controller { get; set; }

	[Property]
	[Category( "Components" )]
	public CitizenAnimationHelper Animator { get; set; }

	/// <summary>
	/// How fast you can walk (units per second)
	/// </summary>
	[Property]
	[Category("Stats")]
	[Range(0f, 400f, 1f)]
	public float WalkSpeed { get; set; } = 120f;

	/// <summary>
	/// How fast you can run (units per second)
	/// </summary>
	[Property]
	[Category( "Stats" )]
	[Range(0f, 1000f, 10f)]
	public float RunSpeed { get; set; } = 250f;

	/// <summary>
	/// How powerful you can jump (units per second)
	/// </summary>
	[Property]
	[Category( "Stats" )]
	[Range(0f, 1000f, 10f)]
	public float JumpStrength { get; set; } = 400f;

	protected override void OnUpdate()
	{

	}

	// gets called based on frequency set in scene settings
	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();
	}

	protected override void OnStart()
	{
		base.OnStart();
	}
}
