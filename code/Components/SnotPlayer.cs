using Sandbox;
using Sandbox.Citizen;
using Sandbox.Diagnostics;
using System;

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
	[Category( "Stats" )]
	[Range( 0f, 400f, 1f )]
	public float WalkSpeed { get; set; } = 120f;

	/// <summary>
	/// How fast you can run (units per second)
	/// </summary>
	[Property]
	[Category( "Stats" )]
	[Range( 0f, 1000f, 10f )]
	public float RunSpeed { get; set; } = 250f;

	/// <summary>
	/// How powerful you can jump (units per second)
	/// </summary>
	[Property]
	[Category( "Stats" )]
	[Range( 0f, 1000f, 10f )]
	public float JumpStrength { get; set; } = 400f;

	/// <summary>
	/// How much damage a punch deals
	/// </summary>
	[Property]
	[Category( "Stats" )]
	[Range( 0f, 5f, 0.1f )]
	public float PunchStrength { get; set; } = 1f;

	/// <summary>
	/// How long until you can punch again
	/// </summary>
	[Property]
	[Category( "Stats" )]
	[Range( 0f, 2f, 0.1f )]
	public float PunchCooldown { get; set; } = 0.5f;

	/// <summary>
	/// How far away you can punch in Hammer Units
	/// </summary>
	[Property]
	[Category( "Stats" )]
	[Range( 0f, 200f, 5f )]
	public float PunchRange { get; set; } = 50f;

	/// <summary>
	/// Where the camera rotates around and the aim originates from
	/// </summary>
	[Property]
	public Vector3 EyePosition { get; set; }

	[Property]
	[Category( "Stats" )]
	[Range( 0f, 1000f, 10f )]
	public float TeleportDistance { get; set; } = 200f;

	// woah! C# has lambda expressions to turn Methods into Properties.
	public Vector3 EyeWorldPosition => Transform.Local.PointToWorld( EyePosition );

	public Angles EyeAngles { get; set; }
	Transform _initialCameraTransform;
	TimeSince _lastPunch;

	private bool hasDoubleJumped = false;

	protected override void DrawGizmos()
	{
		if ( !Gizmo.IsSelected ) return;

		var draw = Gizmo.Draw;

		// see where eyes are
		draw.LineSphere( EyePosition, 10f );
		draw.LineCylinder( EyePosition, EyePosition + Transform.Rotation.Forward * PunchRange, 5f, 5f, 10 );
	}

	protected override void OnUpdate()
	{
		EyeAngles += Input.AnalogLook;
		// stop camera from pitching 360 degrees. We could use 90 degrees instead of 80 degrees below, but 80 degrees looks better
		EyeAngles = EyeAngles.WithPitch( MathX.Clamp( EyeAngles.pitch, -80f, 80f ) );
		Transform.Rotation = Rotation.FromYaw( EyeAngles.yaw );

		if ( Camera != null )
		{
			var cameraTransform = _initialCameraTransform.RotateAround( EyePosition, EyeAngles.WithYaw( 0f ) );
			var cameraPosition = Transform.Local.PointToWorld( cameraTransform.Position );
			var cameraTrace = Scene.Trace.Ray( EyeWorldPosition, cameraPosition ) // cast ray from eye to camera
				.Size( 5f ) // make the ray thick-ish to make sure the camera doesn't worm thru cracks
				.IgnoreGameObjectHierarchy( GameObject ) // ignore all game objects with ray
				.WithoutTags( "player" ) // make sure the player isn't hit by the ray
				.Run();

			// set camera location to where this ray ends, so if it hits something like a wall, the camera doesn't phase thru
			Camera.Transform.Position = cameraTrace.EndPosition;
			Camera.Transform.LocalRotation = cameraTransform.Rotation;
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
		Controller.Accelerate( wishVelocity );

		if ( Controller.IsOnGround )
		{
			hasDoubleJumped = false;
			Controller.Acceleration = 10f;
			Controller.ApplyFriction( 5f, 20f );
			if ( Input.Pressed( "Jump" ) )
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
			if ( Input.Pressed( "Jump" ) && !hasDoubleJumped )
			{
				hasDoubleJumped = true;

				// cancel out y component
				//var velocity = Controller.Velocity;
				//Controller.Velocity = velocity.WithY( Math.Max( 0f, velocity.y ) );


				Controller.Punch( Vector3.Up * JumpStrength );
				Animator?.TriggerJump();
			}
			else
			{
				Controller.Velocity += Scene.PhysicsWorld.Gravity * Time.Delta;
			}
		}

		if ( Input.Pressed( "Teleport" ) )
		{
			var initialTrans = Controller.Transform.World;
			// seems like the controller's MoveTo method already handles casting a ray and making sure the user doesn't clip thru walls. Boring!
			Controller.MoveTo( initialTrans.Position + (initialTrans.Forward * TeleportDistance), false );
		}

		// does complicated math to move and collide with the world
		Controller.Move();

		if ( Animator != null )
		{
			// plays animation that better represents user above ground
			Animator.IsGrounded = Controller.IsOnGround;
			// incredible helper method that animates the player walking/running based on speed
			Animator.WithVelocity( Controller.Velocity );

			// lower fists if it's been longer than 2 seconds since last punch
			if ( _lastPunch >= 2f )
				Animator.HoldType = CitizenAnimationHelper.HoldTypes.None;
		}

		// check if player is trying to punch, and make sure it's been longer than the cooldown since last punch
		if (Input.Pressed("Punch") && _lastPunch >= PunchCooldown)
		{
			Punch();
		}
	}

	protected override void OnStart()
	{
		if ( Camera != null )
		{
			_initialCameraTransform = Camera.Transform.Local;
		}

		// apply local clothing to player
		if ( Components.TryGet<SkinnedModelRenderer>( out var model ) )
		{
			var clothing = ClothingContainer.CreateFromLocalUser();
			clothing.Apply( model );
		}
	}

	public void Punch()
	{
		if ( Animator != null )
		{
			Animator.HoldType = CitizenAnimationHelper.HoldTypes.Punch;
			Animator.Target.Set( "b_attack", true );
		}

		// reset timer for last punch time
		_lastPunch = 0f;
	}
}
