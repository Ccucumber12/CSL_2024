using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PlayerControl : MonoBehaviour
{
    [Header("Accelerate")]
    public float maxForwardSpeed;
    public float maxBackwardSpeed;
    public float frontAcceleration;
    public float backAcceleration;
    public float drag;
    public float maxSteerSpeed;
    public float maxDriftSpeed;

    [Header("Steer")]
    public float maxRotateAngle;
    public float wheelRotateSpeed;
    public float carRotateSpeed;
    public float straightenSpeed;
    public Transform hands;
    public Transform frontLeftWheel;
    public Transform frontRightWheel;

    [Header("Drift")]
    public float driftBoostChargeTime;
    public float driftBoostTime;
    public float driftBoostSpeed;
    public float maxDriftBoostSpeed;

    [Header("VFX")]
    public ParticleSystem dropVFXGlobal;
    public ParticleSystem dropVFXLocal;
    public ParticleSystem leftDriftVFXGlow;
    public ParticleSystem leftDriftVFXSpark;
    public ParticleSystem rightDriftVFXGlow;
    public ParticleSystem rightDriftVFXSpark;
    public ParticleSystem boostVFX;

    [Header("Input Actions")]
    public InputAction accelerate;
    public InputAction steer;
    public InputAction drift;
    public InputAction restart;

    private Rigidbody rb;
    private float currentSpeed;
    private float fallDistance = 5;
    private bool isDropping = false;
    private Vector3 respawnPosition;
    private Vector3 respawnRotation;

    private bool isDrifting = false;
    private int driftDirection;
    private float driftCharge = 0;
    private bool isDriftSparking = false;
    private float remainingBoostTime;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        restart.Enable();
        restart.performed += ctx => RestartGame(ctx);
        drift.started += ctx => StartDrift(ctx);
        drift.canceled += ctx => EndDrift(ctx);

        respawnPosition = transform.position;
        respawnRotation = transform.eulerAngles;
    }

    private void Update()
    {
        if (!isDropping && transform.position.y < -fallDistance)
        {
            isDropping = true;
            StartCoroutine(DropAnimation());
        }
        if (isDrifting)
        {
            driftCharge += Time.deltaTime;
            if (!isDriftSparking && driftCharge > driftBoostChargeTime)
            {
                isDriftSparking = true;
                if (driftDirection == 1)
                    leftDriftVFXSpark.Play();
                else
                    rightDriftVFXSpark.Play();
            }
        }

        if (remainingBoostTime > 0)
        {
            remainingBoostTime -= Time.deltaTime;
            if (remainingBoostTime <= 0)
                boostVFX.Stop();
        }

    }

    private void FixedUpdate()
    {
        if (!isDropping)
        {
            Steer();
            Move();
        }
    }

    private IEnumerator DropAnimation()
    {
        dropVFXGlobal.Play();
        dropVFXLocal.Play();
        rb.velocity = new Vector3(rb.velocity.x, rb.velocity.y - 10, rb.velocity.z);

        yield return new WaitForSeconds(1.5f);

        currentSpeed = 0;
        rb.isKinematic = true;
        transform.position = respawnPosition;
        transform.eulerAngles = respawnRotation;
        dropVFXGlobal.Stop();

        yield return new WaitForSeconds(0.5f);

        rb.isKinematic = false;
        rb.velocity = Vector3.zero;
        isDropping = false;
        dropVFXLocal.Stop();
    }

    private void Move()
    {
        float accelerateValue = ReadAccelerateValue();

        if (accelerateValue > 0)
            currentSpeed = Mathf.Lerp(currentSpeed, maxForwardSpeed, Time.fixedDeltaTime * frontAcceleration);
        else if (accelerateValue < 0)
            currentSpeed = Mathf.Lerp(currentSpeed, -maxBackwardSpeed, Time.fixedDeltaTime * backAcceleration);
        else
            currentSpeed = Mathf.Lerp(currentSpeed, 0, Time.fixedDeltaTime * drag);

        float speedLimit = maxForwardSpeed;

        if (remainingBoostTime > 0)
        {
            currentSpeed += driftBoostSpeed;
            speedLimit = maxDriftBoostSpeed;
        }
        else if (isDrifting)
        {
            speedLimit = maxDriftSpeed;
        }
        else if (ReadSteerValue() != 0)
        {
            speedLimit = maxSteerSpeed;
        }
        currentSpeed = Mathf.Min(currentSpeed, speedLimit);

        RotateRigidbody();
        Vector3 velocity = transform.forward * currentSpeed;
        velocity.y = rb.velocity.y;
        rb.velocity = velocity;
    }

    private void Steer()
    {
        float steerValue = ReadSteerValue();
        if (isDrifting)
        {
            float val = driftDirection * 0.7f + (steerValue + driftDirection) * 0.9f;
            RotateVisiual(val * maxRotateAngle, wheelRotateSpeed);
        }
        else
        {
            if (steerValue == 0)
                RotateVisiual(0, straightenSpeed);
            else
                RotateVisiual(steerValue * maxRotateAngle, wheelRotateSpeed);
        }
    }

    private void RotateVisiual(float targetAngle, float rotateSpeed)
    {
        float handAngle = RegularizeAngle(hands.localRotation.eulerAngles.z);
        float wheelAngle = RegularizeAngle(frontLeftWheel.localRotation.eulerAngles.y);
        hands.Rotate(0, 0, (targetAngle - handAngle) * Time.fixedDeltaTime * rotateSpeed, Space.Self);
        frontLeftWheel.Rotate(0, (-targetAngle - wheelAngle) * Time.fixedDeltaTime * rotateSpeed, 0, Space.Self);
        frontRightWheel.Rotate(0, (-targetAngle - wheelAngle) * Time.fixedDeltaTime * rotateSpeed, 0, Space.Self);
    }

    private void RotateRigidbody()
    {
        float turnAngle = RegularizeAngle(frontLeftWheel.eulerAngles.y - transform.eulerAngles.y);
        Vector3 turnVector = new Vector3(0, turnAngle, 0);
        Quaternion deltaRotation = Quaternion.Euler(Sign(currentSpeed) * turnVector * Time.fixedDeltaTime * carRotateSpeed);
        rb.MoveRotation(rb.rotation * deltaRotation);
    }

    private float RegularizeAngle(float angle)
    {
        angle = (angle > 180) ? angle - 360: angle;
        angle = (angle < -180) ? angle + 360 : angle;
        return angle; 
    } 

    private int Sign(float value)
    {
        if (Mathf.Abs(value) == 0)
            return 0;
        else
            return value > 0 ? 1 : -1;
    }

    private float ReadAccelerateValue()
    {
        float value = accelerate.ReadValue<float>();
        return value;
    }

    private float ReadSteerValue()
    {
        float value = steer.ReadValue<float>();
        return value;
    }

    private void StartDrift(InputAction.CallbackContext ctx)
    {
        if (isDrifting)
        {
            Debug.LogWarning("Is drifting when start drift.");
            return;
        }

        driftDirection = Sign(ReadSteerValue());
        if (driftDirection != 0)
        {
            isDrifting = true;
            if (driftDirection == 1)
                leftDriftVFXGlow.Play();
            else
                rightDriftVFXGlow.Play();
        }
    }

    private void EndDrift(InputAction.CallbackContext ctx)
    {
        if (isDrifting == false)
            return;

        leftDriftVFXGlow.Stop();
        leftDriftVFXSpark.Stop();
        rightDriftVFXGlow.Stop();
        rightDriftVFXSpark.Stop();
        
        if (driftCharge > driftBoostChargeTime)
        {
            remainingBoostTime = driftBoostTime;
            boostVFX.Play();
        }

        driftDirection = 0;
        driftCharge = 0;
        isDriftSparking = false;
        isDrifting = false;
    }

    public void UpdateRespawnInformation(Vector3 position, Vector3 rotation)
    {
        respawnPosition = position;
        respawnRotation = rotation;
    }

    public void EnableControls()
    {
        accelerate.Enable();
        steer.Enable();
        drift.Enable();
    }

    public void DisableControls()
    {
        accelerate.Disable();
        steer.Disable();
        drift.Disable();
    }

    private void RestartGame(InputAction.CallbackContext ctx)
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
