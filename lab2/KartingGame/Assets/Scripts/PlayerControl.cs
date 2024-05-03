using System.Collections;
using System.Collections.Generic;
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
    public AudioSource driftSFX;
    public AudioSource boostSFX;

    [Header("VFX")]
    public ParticleSystem dropVFXGlobal;
    public ParticleSystem dropVFXLocal;
    public ParticleSystem leftDriftVFXGlow;
    public ParticleSystem leftDriftVFXSpark;
    public ParticleSystem rightDriftVFXGlow;
    public ParticleSystem rightDriftVFXSpark;
    public ParticleSystem boostVFX;

    [Header("Keyboard Inputs")]
    public InputAction accelerate;
    public InputAction steer;
    public InputAction drift;
    public InputAction restart;
    public InputAction changeInputScheme;

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

    private bool isMovementLocked;

    [Header("Controller")]
    public ControllerScheme currentControllerScheme;

    [Header("Wireless Controller")]
    public bool useWirelessController;
    public string hostIP = "192.168.43.121";
    public int port = 80;

    private List<Joycon> joycons;
    private Joycon joyconInput = null;

    private SocketClient socketClient = null;
    private bool driftButtonValue;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        restart.Enable();
        changeInputScheme.Enable();
        accelerate.Enable();
        steer.Enable();
        drift.Enable();

        changeInputScheme.performed += ctx => OnChangeInputScheme(ctx);
        restart.performed += ctx => RestartGame(ctx);
        drift.started += ctx => StartDriftInput(ctx);
        drift.canceled += ctx => EndDriftInput(ctx);

        if (useWirelessController)
        {
            socketClient = new SocketClient(hostIP, port);
            currentControllerScheme = ControllerScheme.Wireless;
        }

        joycons = JoyconManager.Instance.j;
        if (joycons.Count > 0)
            joyconInput = joycons[0];

        respawnPosition = transform.position;
        respawnRotation = transform.eulerAngles;

        DisableMovement();
    }

    private void OnDestroy()
    {
        socketClient.Close();
        changeInputScheme.performed -= ctx => OnChangeInputScheme(ctx);
        restart.performed -= ctx => RestartGame(ctx);
        drift.started -= ctx => StartDriftInput(ctx);
        drift.canceled -= ctx => EndDriftInput(ctx);
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

        if (currentControllerScheme != ControllerScheme.Keyboard)
            PollDriftInput();
    }

    private void PollDriftInput()
    {
        bool value = ReadDriftValue();
        if (driftButtonValue == false && value == true)
            StartDrift();
        if (driftButtonValue == true && value == false)
            EndDrift();
        driftButtonValue = value;
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
            float val = driftDirection * 0.4f + (steerValue + driftDirection) * 1.2f;
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
        if (isMovementLocked)
            return 0;
        switch (currentControllerScheme)
        {
            case ControllerScheme.Keyboard:
                return accelerate.ReadValue<float>();
            case ControllerScheme.Joycon:
                if (joyconInput.GetButton(Joycon.Button.DPAD_UP) || joyconInput.GetButton(Joycon.Button.DPAD_DOWN))
                    return 1;
                if (joyconInput.GetButton(Joycon.Button.DPAD_LEFT) || joyconInput.GetButton(Joycon.Button.DPAD_RIGHT))
                    return -1;
                return 0;
            case ControllerScheme.Wireless:
                float value = socketClient.accelerationInput;
                return Mathf.Abs(value) < 0.2f ? 0 : value;
            default:
                return 0;
        }
    }

    private float ReadSteerValue()
    {
        if (isMovementLocked)
            return 0;
        switch (currentControllerScheme)
        {
            case ControllerScheme.Keyboard:
                return steer.ReadValue<float>();
            case ControllerScheme.Joycon:
                return joyconInput.GetStick()[1];
            case ControllerScheme.Wireless:
                float value = socketClient.rotationInput;
                return Mathf.Abs(value) < 0.5f ? 0 : value;
            default:
                return 0;
        }
    }

    private bool ReadDriftValue()
    {
        if (isMovementLocked)
            return false;
        switch(currentControllerScheme)
        {
            case ControllerScheme.Keyboard:
                return drift.IsPressed();
            case ControllerScheme.Joycon:
                return joyconInput.GetButton(Joycon.Button.SR);
            case ControllerScheme.Wireless:
                return socketClient.buttonInput;
            default:
                return false;
        }
    }

    private void StartDriftInput(InputAction.CallbackContext ctx)
    {
        StartDrift();
    }

    private void EndDriftInput(InputAction.CallbackContext ctx)
    {
        EndDrift();
    }

    private void StartDrift()
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
            driftSFX.Play();
            if (driftDirection == 1)
                leftDriftVFXGlow.Play();
            else
                rightDriftVFXGlow.Play();

            if (currentControllerScheme == ControllerScheme.Joycon)
                joyconInput.SetRumble(100, 200, 0.5f);
        }
    }

    private void EndDrift()
    {
        if (isDrifting == false)
            return;

        leftDriftVFXGlow.Stop();
        leftDriftVFXSpark.Stop();
        rightDriftVFXGlow.Stop();
        rightDriftVFXSpark.Stop();
        driftSFX.Stop();

        if (currentControllerScheme == ControllerScheme.Joycon)
            joyconInput.SetRumble(0, 0, 0);

        if (driftCharge > driftBoostChargeTime)
        {
            remainingBoostTime = driftBoostTime;
            boostVFX.Play();
            boostSFX.Play();
            if (currentControllerScheme == ControllerScheme.Joycon)
                joyconInput.SetRumble(150, 300, 0.8f, Mathf.FloorToInt(driftBoostTime * 1000));
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

    public void EnableMovement()
    {
        isMovementLocked = false;
    }

    public void DisableMovement()
    {
        isMovementLocked = true;
    }

    private void RestartGame(InputAction.CallbackContext ctx)
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void OnChangeInputScheme(InputAction.CallbackContext ctx)
    {
        if (currentControllerScheme == ControllerScheme.Keyboard)
            currentControllerScheme = ControllerScheme.Joycon;
        else
            currentControllerScheme = ControllerScheme.Keyboard;
    }
}

public enum ControllerScheme
{
    Keyboard,
    Joycon,
    Wireless,
}
