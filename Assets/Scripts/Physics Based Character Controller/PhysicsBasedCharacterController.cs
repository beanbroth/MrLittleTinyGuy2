using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// A floating-capsule oriented physics based character controller. Based on the approach devised by Toyful Games for Very Very Valet. Modified version of Joe Brinns's Physics Character Controller
/// </summary>
public class PhysicsBasedCharacterController : MonoBehaviour
{
    private Rigidbody _rb;
    private Vector3 _gravitationalForce;
    private Vector3 _rayDir = Vector3.down;
    private Vector3 _previousVelocity = Vector3.zero;
    private Vector2 _moveContext;
    private ParticleSystem.EmissionModule _emission;

    [Header("Other:")]
    [SerializeField] private LayerMask _floorLayerMask;
    [SerializeField] private Transform _cam;
    [SerializeField] private ParticleSystem _dustParticleSystem;

    private bool _shouldMaintainHeight = true;

    [Header("Height Spring:")]
    [SerializeField] private float _rideHeight = 1.75f;
    [SerializeField] private float _rayToGroundLength = 3f;
    [SerializeField] public float _rideSpringStrength = 50f;
    [SerializeField] private float _rideSpringDamper = 5f;


    private enum lookDirectionOptions { velocity, acceleration, moveInput };
    private Quaternion _uprightTargetRot = Quaternion.identity;
    private Quaternion _lastTargetRot;
    private Vector3 _platformInitRot;
    private bool didLastRayHit;

    [Header("Upright Spring:")]
    [SerializeField] private lookDirectionOptions _characterLookDirection = lookDirectionOptions.velocity;
    [SerializeField] private float _uprightSpringStrength = 40f;
    [SerializeField] private float _uprightSpringDamper = 5f;


    private Vector3 _moveInput;
    private float _speedFactor = 1f;
    private float _maxAccelForceFactor = 1f;
    private Vector3 _m_GoalVel = Vector3.zero;

    [Header("Movement:")]
    [SerializeField] private float _maxSpeed = 8f;
    [SerializeField] private float _acceleration = 200f;
    [SerializeField] private float _maxAccelForce = 150f;
    [SerializeField] private float _leanFactor = 0.25f;
    [SerializeField] private AnimationCurve _accelerationFactorFromDot;
    [SerializeField] private AnimationCurve _maxAccelerationForceFactorFromDot;
    [SerializeField] private Vector3 _moveForceScale = new Vector3(1f, 0f, 1f);


    private Vector3 _jumpInput;
    private float _timeSinceJumpPressed = 0f;
    private float _timeSinceUngrounded = 0f;
    private float _timeSinceJump = 0f;
    private bool _jumpReady = true;
    private bool _isJumping = false;

    [Header("Jump:")]
    [SerializeField] private float _jumpForceFactor = 10f;
    [SerializeField] private float _riseGravityFactor = 5f;
    [SerializeField] private float _fallGravityFactor = 10f;
    [SerializeField] private float _lowJumpFactor = 2.5f;
    [SerializeField] private float _jumpBuffer = 0.15f;
    [SerializeField] private float _coyoteTime = 0.25f;


    private void Start()
    {
        _rb = GetComponent<Rigidbody>();
        _gravitationalForce = Physics.gravity * _rb.mass;

        if (_dustParticleSystem)
        {
            _emission = _dustParticleSystem.emission;
            _emission.enabled = false;
        }
    }

    private bool GroundCheck(bool rayHitGround, RaycastHit rayHit)
    {
        return (rayHitGround == true) ? rayHit.distance <= _rideHeight * 1.3f : false;
    }


    private Vector3 GetLookDirection(lookDirectionOptions lookDirectionOption)
    {
        Vector3 lookDirection = Vector3.zero;
        if (lookDirectionOption == lookDirectionOptions.velocity || lookDirectionOption == lookDirectionOptions.acceleration)
        {
            Vector3 velocity = _rb.velocity;
            velocity.y = 0f;
            if (lookDirectionOption == lookDirectionOptions.velocity)
            {
                lookDirection = velocity;
            }
            else if (lookDirectionOption == lookDirectionOptions.acceleration)
            {
                Vector3 deltaVelocity = velocity - _previousVelocity;
                _previousVelocity = velocity;
                Vector3 acceleration = deltaVelocity / Time.fixedDeltaTime;
                lookDirection = acceleration;
            }
        }
        else if (lookDirectionOption == lookDirectionOptions.moveInput)
        {
            lookDirection = AdjustInputToFaceCamera(_moveInput);
        }
        return lookDirection;
    }

    private void FixedUpdate()
    {
        _moveInput = new Vector3(_moveContext.x, 0, _moveContext.y);

        (bool rayHitGround, RaycastHit rayHit) = RaycastToGround();

        bool grounded = GroundCheck(rayHitGround, rayHit);
        if (grounded == true)
        {
            Vector3 lookDirection = GetLookDirection(_characterLookDirection);
            MaintainUpright(lookDirection, rayHit);
            CharacterMove(_moveInput, rayHit);
            if (_dustParticleSystem)
            {
                if (_emission.enabled == false)
                {
                    _emission.enabled = true; // Applies the new value directly to the Particle System                  
                }
            }

            _timeSinceUngrounded = 0f;

            if (_timeSinceJump > 0.2f)
            {
                _isJumping = false;
            }
        }
        else
        {
            if (_dustParticleSystem)
            {
                if (_emission.enabled == true)
                {
                    _emission.enabled = false; // Applies the new value directly to the Particle System
                }
            }

            _timeSinceUngrounded += Time.fixedDeltaTime;
        }

        CharacterJump(_jumpInput, grounded, rayHit);

        if (rayHitGround && _shouldMaintainHeight)
        {
            MaintainHeight(rayHit);
        }


    }

    /// <summary>
    /// Perfom raycast towards the ground.
    /// </summary>
    /// <returns>Whether the ray hit the ground, and information about the ray.</returns>
    private (bool, RaycastHit) RaycastToGround()
    {
        RaycastHit rayHit;
        Ray rayToGround = new Ray(transform.position, _rayDir);
        bool rayHitGround = Physics.Raycast(rayToGround, out rayHit, _rayToGroundLength, _floorLayerMask);


        return (rayHitGround, rayHit);
    }

    /// <summary>
    /// Determines the relative velocity of the character to the ground beneath,
    /// Calculates and applies the oscillator force to bring the character towards the desired ride height.
    /// Additionally applies the oscillator force to the squash and stretch oscillator, and any object beneath.
    /// </summary>
    /// <param name="rayHit">Information about the RaycastToGround.</param>
    private void MaintainHeight(RaycastHit rayHit)
    {
        Vector3 vel = _rb.velocity;
        Vector3 otherVel = Vector3.zero;
        Rigidbody hitBody = rayHit.rigidbody;
        if (hitBody != null)
        {
            otherVel = hitBody.velocity;
        }
        float rayDirVel = Vector3.Dot(_rayDir, vel);
        float otherDirVel = Vector3.Dot(_rayDir, otherVel);

        float relVel = rayDirVel - otherDirVel;
        float currHeight = rayHit.distance - _rideHeight;
        float springForce = (currHeight * _rideSpringStrength) - (relVel * _rideSpringDamper);
        Vector3 maintainHeightForce = -_gravitationalForce + springForce * Vector3.down;
        Vector3 oscillationForce = springForce * Vector3.down;
        _rb.AddForce(maintainHeightForce);
        //Debug.DrawLine(transform.position, transform.position + (_rayDir * springForce), Color.yellow);

        // Apply force to objects beneath
        if (hitBody != null)
        {
            hitBody.AddForceAtPosition(-maintainHeightForce, rayHit.point);
        }
    }

    /// <summary>
    /// Determines the desired y rotation for the character, with account for platform rotation.
    /// </summary>
    /// <param name="yLookAt">The input look rotation.</param>
    /// <param name="rayHit">The rayHit towards the platform.</param>
    private void CalculateTargetRotation(Vector3 yLookAt, RaycastHit rayHit = new RaycastHit())
    {
        if (didLastRayHit)
        {
            _lastTargetRot = _uprightTargetRot;
            try
            {
                _platformInitRot = transform.parent.rotation.eulerAngles;
            }
            catch
            {
                _platformInitRot = Vector3.zero;
            }
        }
        if (rayHit.rigidbody == null)
        {
            didLastRayHit = true;
        }
        else
        {
            didLastRayHit = false;
        }

        if (yLookAt != Vector3.zero)
        {
            _uprightTargetRot = Quaternion.LookRotation(yLookAt, Vector3.up);
            _lastTargetRot = _uprightTargetRot;
            try
            {
                _platformInitRot = transform.parent.rotation.eulerAngles;
            }
            catch
            {
                _platformInitRot = Vector3.zero;
            }
        }
        else
        {
            try
            {
                Vector3 platformRot = transform.parent.rotation.eulerAngles;
                Vector3 deltaPlatformRot = platformRot - _platformInitRot;
                float yAngle = _lastTargetRot.eulerAngles.y + deltaPlatformRot.y;
                _uprightTargetRot = Quaternion.Euler(new Vector3(0f, yAngle, 0f));
            }
            catch
            {

            }
        }
    }

    /// <summary>
    /// Adds torque to the character to keep the character upright, acting as a torsional oscillator (i.e. vertically flipped pendulum).
    /// </summary>
    /// <param name="yLookAt">The input look rotation.</param>
    /// <param name="rayHit">The rayHit towards the platform.</param>
    private void MaintainUpright(Vector3 yLookAt, RaycastHit rayHit = new RaycastHit())
    {
        CalculateTargetRotation(yLookAt, rayHit);

        Quaternion currentRot = transform.rotation;
        Quaternion toGoal = MathsUtils.ShortestRotation(_uprightTargetRot, currentRot);

        Vector3 rotAxis;
        float rotDegrees;

        toGoal.ToAngleAxis(out rotDegrees, out rotAxis);
        rotAxis.Normalize();

        float rotRadians = rotDegrees * Mathf.Deg2Rad;

        _rb.AddTorque((rotAxis * (rotRadians * _uprightSpringStrength)) - (_rb.angularVelocity * _uprightSpringDamper));
    }

    /// <summary>
    /// Reads the player movement input.
    /// </summary>
    /// <param name="context">The move input's context.</param>
    public void MoveInputAction(InputAction.CallbackContext context)
    {
        _moveContext = context.ReadValue<Vector2>();
    }

    /// <summary>
    /// Reads the player jump input.
    /// </summary>
    /// <param name="context">The jump input's context.</param>
    public void JumpInputAction(InputAction.CallbackContext context)
    {
        float jumpContext = context.ReadValue<float>();
        _jumpInput = new Vector3(0, jumpContext, 0);

        if (context.started) // button down
        {
            _timeSinceJumpPressed = 0f;
        }
    }

    /// <summary>
    /// Apply forces to move the character up to a maximum acceleration, with consideration to acceleration graphs.
    /// </summary>
    /// <param name="moveInput">The player movement input.</param>
    /// <param name="rayHit">The rayHit towards the platform.</param>
    private void CharacterMove(Vector3 moveInput, RaycastHit rayHit)
    {


        Vector3 m_UnitGoal = AdjustInputToFaceCamera(moveInput);
        Vector3 unitVel = _m_GoalVel.normalized;
        float velDot = Vector3.Dot(m_UnitGoal, unitVel);
        float accel = _acceleration * _accelerationFactorFromDot.Evaluate(velDot);
        Vector3 goalVel = m_UnitGoal * _maxSpeed * _speedFactor;
        Vector3 otherVel = Vector3.zero;
        Rigidbody hitBody = rayHit.rigidbody;
        _m_GoalVel = Vector3.MoveTowards(_m_GoalVel,
                                        goalVel,
                                        accel * Time.fixedDeltaTime);
        Vector3 neededAccel = (_m_GoalVel - _rb.velocity) / Time.fixedDeltaTime;
        float maxAccel = _maxAccelForce * _maxAccelerationForceFactorFromDot.Evaluate(velDot) * _maxAccelForceFactor;
        neededAccel = Vector3.ClampMagnitude(neededAccel, maxAccel);
        _rb.AddForceAtPosition(Vector3.Scale(neededAccel * _rb.mass, _moveForceScale), transform.position + new Vector3(0f, transform.localScale.y * _leanFactor, 0f)); // Using AddForceAtPosition in order to both move the player and cause the play to lean in the direction of input.
    }

    /// <summary>
    /// Adjusts the input, so that the movement matches input regardless of camera rotation.
    /// </summary>
    /// <param name="moveInput">The player movement input.</param>
    /// <returns>The camera corrected movement input.</returns>
    private Vector3 AdjustInputToFaceCamera(Vector3 moveInput)
    {
        float facing = _cam.transform.eulerAngles.y;
        return (Quaternion.Euler(0, facing, 0) * moveInput);
    }


    /// <summary>
    /// Apply force to cause the character to perform a single jump, including coyote time and a jump input buffer.
    /// </summary>
    /// <param name="jumpInput">The player jump input.</param>
    /// <param name="grounded">Whether or not the player is considered grounded.</param>
    /// <param name="rayHit">The rayHit towards the platform.</param>
    private void CharacterJump(Vector3 jumpInput, bool grounded, RaycastHit rayHit)
    {
        _timeSinceJumpPressed += Time.fixedDeltaTime;
        _timeSinceJump += Time.fixedDeltaTime;
        if (_rb.velocity.y < 0)
        {
            _shouldMaintainHeight = true;
            _jumpReady = true;
            if (!grounded)
            {
                // Increase downforce for a sudden plummet.
                _rb.AddForce(_gravitationalForce * (_fallGravityFactor - 1f)); // Hmm... this feels a bit weird. I want a reactive jump, but I don't want it to dive all the time...
            }
        }
        else if (_rb.velocity.y > 0)
        {
            if (!grounded)
            {
                if (_isJumping)
                {
                    _rb.AddForce(_gravitationalForce * (_riseGravityFactor - 1f));
                }
                if (jumpInput == Vector3.zero)
                {
                    // Impede the jump height to achieve a low jump.
                    _rb.AddForce(_gravitationalForce * (_lowJumpFactor - 1f));
                }
            }
        }

        if (_timeSinceJumpPressed < _jumpBuffer)
        {
            if (_timeSinceUngrounded < _coyoteTime)
            {
                if (_jumpReady)
                {
                    _jumpReady = false;
                    _shouldMaintainHeight = false;
                    _isJumping = true;
                    _rb.velocity = new Vector3(_rb.velocity.x, 0f, _rb.velocity.z); // Cheat fix... (see comment below when adding force to rigidbody).
                    if (rayHit.distance != 0) // i.e. if the ray has hit
                    {
                        _rb.position = new Vector3(_rb.position.x, _rb.position.y - (rayHit.distance - _rideHeight), _rb.position.z);
                    }
                    _rb.AddForce(Vector3.up * _jumpForceFactor, ForceMode.Impulse); // This does not work very consistently... Jump height is affected by initial y velocity and y position relative to RideHeight... Want to adopt a fancier approach (more like PlayerMovement). A cheat fix to ensure consistency has been issued above...
                    _timeSinceJumpPressed = _jumpBuffer; // So as to not activate further jumps, in the case that the player lands before the jump timer surpasses the buffer.
                    _timeSinceJump = 0f;
                }
            }
        }
    }
}
