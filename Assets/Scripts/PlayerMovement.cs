using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PlayerMovement : MonoBehaviour
{
    [Header("Mouse variables")]

    // mouse vars
    public float sensitivity;
    public bool hideCursor = false;
    
    private float xRot = 0f;
    private float yRot = 0f;
    private float zRot = 0f;
    private Camera cam;
    private Quaternion startingRot;

    [Header("Position variables")]

    // position vars
    public float standHeight;
    public float crouchHeight;
    private float height;
    public float walkSpeed;
    public float crouchSpeed;
    public float runSpeed;
    public float runFOVOffset = 10;

    private float speed;
    private float speedSmoothVel;
    private float heightSmoothVel;
    private float startFOV, fovSmoothTime;

    [Space(10)]

    public float jumpStrength;
    public float gravity;
    public float friction;
    public float collisionDistance;

    [Header("Flight variables")]

    // flight vars
    public float flightStartBoost;       
    public float exitFlightCooldown;  // while in flight
    public float flightSpeed;
    public float drag;
    private Vector3 flightAcceleration;
    private float lastTap;
    private bool isFlying = false;

    private Vector3 velocity;
    private Vector3 lastMovement;
    private Vector3 velSmoothTime;

    [Header("Health")]

    // meta

    public int health = 4;
    public float healthIncreaseCooldown;
    private float lastHealthIncrease; 
    public Texture healthHUD1;
    public Texture healthHUD2;
    public Texture healthHUD3;
    public Texture clear;
    public RawImage bloodHUD;
    public float flinchAngle = 10;
    public float flinchTime = 0.5f;

    [Header("Audio")]

    public AudioSource jetpack;
    public AudioSource hit;

    [HideInInspector]
    public bool isDead = false;

    private float zRotSmoothVel;
    private float startJetpackVol, jetpackVolSmoothTime;

    // Start is called before the first frame update
    void Start()
    {
        lastHealthIncrease = Time.time;
        cam = Camera.main;
        startingRot = cam.transform.rotation;
        startFOV = cam.fieldOfView;

        height = standHeight;

        if (hideCursor)
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        startJetpackVol = jetpack.volume;
        jetpack.volume = 0;
    }

    // Update is called once per frame
    void Update()
    { 
        if (!isDead)
        {
            UpdatePosition();

            UpdateHealth();
        }

        // if(Input.GetKeyDown(KeyCode.Escape))
        // {
        //     Cursor.visible = true;
        //     Cursor.lockState = CursorLockMode.None;
        //     SceneManager.LoadScene("MainMenu");
        // }
    }

    void LateUpdate()
    {
        if (!isDead)
            UpdateLookRotation();
    }

    private void UpdatePosition()
    {
        if(!isFlying)
        {
            jetpack.volume = Mathf.SmoothDamp(jetpack.volume, 0, ref jetpackVolSmoothTime, 0.15f);
            OnGround();            
        }
        else
        {
            jetpack.volume = Mathf.SmoothDamp(jetpack.volume, startJetpackVol, ref jetpackVolSmoothTime, 0.15f);;
            InFlight();
        }
    }

    public void TakeDamage()
    {
        lastHealthIncrease = Time.time;
        health--;
        zRot = flinchAngle;
        hit.PlayOneShot(hit.clip);
    }

    private void UpdateHealth()
    {
        if(lastHealthIncrease + healthIncreaseCooldown < Time.time)
        {
            health++;
            lastHealthIncrease = Time.time;
        }

        health = Mathf.Clamp(health, 0, 4);

        if(health == 4)
        {
            bloodHUD.texture = clear;
        }
        else if(health == 3)
        {
            bloodHUD.texture = healthHUD1;
        }
        else if(health == 2)
        {
            bloodHUD.texture = healthHUD2;
        }
        else if(health == 1)
        {
            bloodHUD.texture = healthHUD3;
        }

        if(health < 1)
        {
            isDead = true;
            FindObjectOfType<GameManager>().BeingDeathSequence();
        }

    }

    private void OnGround()
    {
        velocity += new Vector3(0, gravity, 0); // ACCELERATION
        this.transform.Translate(velocity * Time.deltaTime);

        // Handle special movement cases
        if(Input.GetKey(KeyCode.LeftControl))
        {
            // Smooth to crouch speed and height
            speed = Mathf.SmoothDamp(speed, crouchSpeed, ref speedSmoothVel, 0.1f);
            height = Mathf.SmoothDamp(height, crouchHeight, ref heightSmoothVel, 0.1f);;
        }
        else
        {
            // Change speed based on if sprinting
            if(Input.GetKey(KeyCode.LeftShift))
            {
                speed = Mathf.SmoothDamp(speed, runSpeed, ref speedSmoothVel, 0.1f);
                cam.fieldOfView = Mathf.SmoothDamp(cam.fieldOfView, startFOV + runFOVOffset, ref fovSmoothTime, 0.15f);
            }
            else
            {
                speed = Mathf.SmoothDamp(speed, walkSpeed, ref speedSmoothVel, 0.1f);
                cam.fieldOfView = Mathf.SmoothDamp(cam.fieldOfView, startFOV, ref fovSmoothTime, 0.15f);
            }

            // Stand if not crouching
            height = Mathf.SmoothDamp(height, standHeight, ref heightSmoothVel, 0.1f);
        }

        RaycastHit hit;
        Physics.Raycast(this.transform.position, Vector3.down, out hit);

        // On ground OR if on ground and crouching
        if(this.transform.position.y - hit.point.y <= height || (this.transform.position.y - hit.point.y > crouchHeight && Input.GetKey(KeyCode.LeftControl)))
        {
            this.transform.position = new Vector3(hit.point.x, hit.point.y + height, hit.point.z);
            
            velocity = new Vector3(velocity.x, 0, velocity.z);
            velocity = Vector3.SmoothDamp(velocity, new Vector3(0, 0, 0), ref velSmoothTime, friction);
        }
        else
        {
            this.transform.position += new Vector3(0, velocity.y, 0) * Time.deltaTime;
        }
        
        Vector3 forwardVector = new Vector3(cam.transform.forward.x, 0f, cam.transform.forward.z).normalized;
        Vector3 rightVector = new Vector3(cam.transform.right.x, 0f, cam.transform.right.z).normalized;

        // Handle input for movement
        if(Input.GetKeyDown(KeyCode.Space) && this.transform.position.y - hit.point.y <= height + 0.1f)
        {
            velocity += new Vector3(0, jumpStrength, 0);
        }
        else if(Input.GetKeyDown(KeyCode.Space) && this.transform.position.y - hit.point.y > height + 0.1f)
        {
            isFlying = true;

            velocity = new Vector3(lastMovement.x, jumpStrength, lastMovement.z) * flightStartBoost;
        }

        // Checks all potential movement
        lastMovement = Vector3.zero;
        if(Input.GetKey(KeyCode.W))
        {
            lastMovement += forwardVector * speed;
        }

        if(Input.GetKey(KeyCode.S))
        {
            lastMovement += -forwardVector * speed;
        }

        if(Input.GetKey(KeyCode.D))
        {
            lastMovement += rightVector * speed;
        }

        if(Input.GetKey(KeyCode.A))
        {
            lastMovement += -rightVector * speed;
        }

        lastMovement.y = 0;
    
        // don't move into a wall, but move if you can move 
        //Debug.DrawRay(this.transform.position, lastMovement, Color.magenta, 0.1f);
        if(!Physics.Raycast(this.transform.position, lastMovement, out hit, collisionDistance))
        {
            transform.position += lastMovement * Time.deltaTime;            
        }

        Debug.DrawRay(this.transform.position, velocity, Color.red);

        if(Physics.Raycast(this.transform.position, velocity, out hit, collisionDistance))
        {
            velocity = -velocity / 4;
        }
    }

    private void InFlight()
    {
        if(Input.GetKeyDown(KeyCode.Space))
        {
            if(lastTap + exitFlightCooldown > Time.time)
            {
                isFlying = false;
            }
            else
            {
                lastTap = Time.time;
            }
        }

        Vector3 flyingVector = new Vector3(cam.transform.forward.x, cam.transform.forward.y, cam.transform.forward.z).normalized;
        Vector3 flyingRightVector = new Vector3(cam.transform.right.x, cam.transform.right.y, cam.transform.right.z).normalized;

        flightAcceleration = Vector3.zero;
        if(Input.GetKey(KeyCode.W))
        {
            flightAcceleration += flyingVector * flightSpeed;
        }

        if(Input.GetKey(KeyCode.S))
        {
            flightAcceleration += -flyingVector * flightSpeed;
        }

        if(Input.GetKey(KeyCode.D))
        {
            flightAcceleration += flyingRightVector * flightSpeed;
        }

        if(Input.GetKey(KeyCode.A))
        {
            flightAcceleration += -flyingRightVector * flightSpeed;
        }


        velocity = Vector3.SmoothDamp(velocity, flightAcceleration, ref velSmoothTime, drag);

        velocity = Vector3.ClampMagnitude(velocity, flightSpeed);
        this.transform.Translate(velocity * Time.deltaTime);

        RaycastHit hit;
        if(Physics.Raycast(this.transform.position, velocity, out hit, collisionDistance))
        {
            isFlying = false;
        }
    }

    private void UpdateLookRotation()
    {
        float mouseX = Input.GetAxis("Mouse X") * sensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * sensitivity;

        yRot += mouseX;
        xRot -= mouseY;
        xRot = Mathf.Clamp(xRot, -90, 90);
        zRot = Mathf.SmoothDamp(zRot, 0, ref zRotSmoothVel, flinchTime);

        cam.transform.eulerAngles = startingRot.eulerAngles + new Vector3(xRot, yRot, zRot);
    }

    public void SetPosition(Vector3 pos)
    {
        transform.position = pos;
    }
}