using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class PlayerControl : MonoBehaviour
{
    [Header("Accelerate")]
    public float maxForwardSpeed;
    public float maxBackwardSpeed;
    public float frontAcceleration;
    public float backAcceleration;
    public float drag;

    [Header("Steer")]
    public float maxRotateAngle;
    public float wheelRotateSpeed;
    public float carRotateSpeed;
    public float straightenSpeed;
    public Transform hands;
    public Transform frontLeftWheel;
    public Transform frontRightWheel; 

    [Header("Input Actions")]
    public InputAction accelerate;
    public InputAction steer;
    public InputAction drift;
    public InputAction restart;

    [Header("Others")]
    public ParticleSystem dropVFXGlobal;
    public ParticleSystem dropVFXLocal;

    private Rigidbody rb;
    private float currentSpeed;
    private float fallDistance = 5;
    private bool isDropping = false;
    private Vector3 respawnPosition;
    private Vector3 respawnRotation;

    private bool isDrifting = false;
    private int driftDirection;
    private float driftCharge = 0;

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
        }
    }

    private void EndDrift(InputAction.CallbackContext ctx)
    {
        isDrifting = false;
        driftDirection = 0;
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
