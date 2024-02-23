using Sandbox;
using Sandbox.Citizen;
using Sandbox.Diagnostics;

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

	/// <summary>
	/// Where the camera rotates around and the aim originates from
	/// </summary>
	[Property]
	public Vector3 EyePosition { get; set; }

	public Angles EyeAngles { get; set; }
	Transform _initialCameraTransform;

	protected override void DrawGizmos()
	{
		Gizmo.Draw.LineSphere( EyePosition, 10f );
	}

	protected override void OnUpdate()
	{
		EyeAngles += Input.AnalogLook;
		// stop camera from pitching 360 degrees. We could use 90 degrees instead of 80 degrees below, but 80 degrees looks better
		EyeAngles = EyeAngles.WithPitch( MathX.Clamp( EyeAngles.pitch, -80f, 80f ) );
		Transform.Rotation = Rotation.FromYaw( EyeAngles.yaw );

		if (Camera != null )
		{
			Camera.Transform.Local = _initialCameraTransform.RotateAround(EyePosition, EyeAngles.WithYaw(0f));
		}
	}

	// gets called based on frequency set in scene settings
	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();
		Assert.NotNull( Controller );

		var wishSpeed = Input.Down( "Run" ) ? RunSpeed : WalkSpeed;

		// we get the Normal vector of the AnalogMove, since moving diagonally is a vector of length 1.4 otherwise
		var wishVelocity = Input.AnalogMove.Normal * wishSpeed * Transform.Rotation;
		Controller.Accelerate(wishVelocity );

		if (Controller.IsOnGround)
		{
			Controller.Acceleration = 10f;
			Controller.ApplyFriction( 5f );
			if (Input.Pressed("Jump"))
			{
				Controller.Punch( Vector3.Up * JumpStrength );

				Animator?.TriggerJump();
			}
		}
		else // midair
		{
			// halving acceleration when mid-air allows less strafing
			Controller.Acceleration = 5f;
			// we multiply by Time.Delta here since the gravity should be taking place over this amount
			Controller.Velocity += Scene.PhysicsWorld.Gravity * Time.Delta;
		}

		// does complicated math to move and collide with the world
		Controller.Move();

		if (Animator != null)
		{
			// plays animation that better represents user above ground
			Animator.IsGrounded = Controller.IsOnGround;
			// incredible helper method that animates the player walking/running based on speed
			Animator.WithVelocity( Controller.Velocity );
		}
	}

	protected override void OnStart()
	{
		if (Camera != null)
		{
			_initialCameraTransform = Camera.Transform.Local;
		}
	}
}
