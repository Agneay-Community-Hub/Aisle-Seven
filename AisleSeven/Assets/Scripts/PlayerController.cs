using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public static PlayerController I;
    public static float Sensitivity = 2.0f;

    public Camera Cam;
    public Light Flash;
    public Interactable Focus;
    public bool FlashOn;
    public float Battery = 1f;
    public bool LookLocked;
    public bool Sprinting { get; private set; }
    public bool Moving { get; private set; }
    public bool InAisle7 => transform.position.z > Aisle7.EntranceZ + 0.4f;

    CharacterController cc;
    float yaw, pitch, vy, stepDist, bob, msgCooldown;

    void Awake()
    {
        I = this;
        cc = gameObject.AddComponent<CharacterController>();
        cc.height = 1.75f; cc.radius = 0.3f; cc.center = new Vector3(0, 0.875f, 0); cc.stepOffset = 0.3f;

        var camGo = new GameObject("PlayerCam");
        camGo.tag = "MainCamera";
        camGo.transform.SetParent(transform, false);
        camGo.transform.localPosition = new Vector3(0, 1.62f, 0);
        Cam = camGo.AddComponent<Camera>();
        Cam.nearClipPlane = 0.03f; Cam.farClipPlane = 140f; Cam.fieldOfView = 70f;
        Cam.clearFlags = CameraClearFlags.SolidColor; Cam.backgroundColor = Color.black;
        camGo.AddComponent<AudioListener>();

        var fl = new GameObject("Flashlight");
        fl.transform.SetParent(camGo.transform, false);
        fl.transform.localPosition = new Vector3(0.18f, -0.15f, 0.1f);
        Flash = fl.AddComponent<Light>();
        Flash.type = LightType.Spot; Flash.spotAngle = 46f; Flash.range = 24f; Flash.intensity = 2.6f;
        Flash.color = new Color(1f, 0.94f, 0.8f); Flash.enabled = false;
        Flash.renderMode = LightRenderMode.ForcePixel;
    }

    public void Teleport(Vector3 pos, float yawDeg)
    {
        cc.enabled = false;
        transform.position = pos;
        cc.enabled = true;
        yaw = yawDeg; pitch = 0; vy = 0;
        Apply();
    }

    public void SetLook(float y, float p) { yaw = y; pitch = p; Apply(); }

    void Apply()
    {
        transform.rotation = Quaternion.Euler(0, yaw, 0);
        Cam.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
    }

    void Update()
    {
        var gm = GameManager.I;
        Focus = null;
        Flash.enabled = FlashOn;
        if (gm == null || !gm.PlayerActive) { Sprinting = false; Moving = false; return; }
        float dt = Time.deltaTime;

        if (!LookLocked)
        {
            yaw += Input.GetAxis("Mouse X") * Sensitivity;
            pitch -= Input.GetAxis("Mouse Y") * Sensitivity;
            pitch = Mathf.Clamp(pitch, -82f, 82f);
            Apply();
        }

        Vector3 input = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));
        if (gm.MovementLocked) input = Vector3.zero;
        input = Vector3.ClampMagnitude(input, 1f);
        Moving = input.sqrMagnitude > 0.01f;
        Sprinting = Moving && input.z > 0.1f && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));
        float speed = Sprinting ? 5.0f : 2.8f;
        if (input.z < -0.1f) speed *= 0.78f;

        Vector3 move = transform.TransformDirection(input) * speed;
        vy = cc.isGrounded ? -1f : vy - 15f * dt;
        move.y = vy;
        cc.Move(move * dt);

        if (Moving)
        {
            stepDist += speed * dt;
            bob += speed * dt * 2.3f;
            if (stepDist > (Sprinting ? 1.7f : 1.25f))
            {
                stepDist = 0;
                Sfx.I.Play(Sfx.I.step, 0.32f, Random.Range(0.85f, 1.15f));
            }
        }
        Cam.transform.localPosition = new Vector3(0, 1.62f + (Moving ? Mathf.Sin(bob) * 0.03f : 0f), 0);

        // Flashlight
        if (Input.GetKeyDown(KeyCode.F))
        {
            if (FlashOn) FlashOn = false;
            else if (Battery > 0.05f) FlashOn = true;
            Sfx.I.Play(Sfx.I.click, 0.7f);
        }
        if (FlashOn)
        {
            Battery -= dt * (InAisle7 ? 0.055f : 0.03f);
            if (Battery <= 0) { Battery = 0; FlashOn = false; Sfx.I.Play(Sfx.I.click, 0.7f, 0.6f); }
        }
        else Battery = Mathf.Min(1f, Battery + dt * 0.05f);
        Flash.enabled = FlashOn;
        Flash.intensity = Battery < 0.15f && Random.value < 0.15f ? 0.6f : 2.6f;

        // Interaction ray
        if (Physics.Raycast(Cam.transform.position, Cam.transform.forward, out var hit, 2.7f, ~0, QueryTriggerInteraction.Collide))
        {
            var it = hit.collider.GetComponentInParent<Interactable>();
            if (it != null && it.Usable) Focus = it;
        }
        if (Focus == null && gm.CanRingUp && gm.NearRegister) Focus = gm.RegisterI;
        if (Focus != null && Input.GetKeyDown(KeyCode.E)) Focus.OnUse?.Invoke();

        msgCooldown -= dt;
    }

    void OnControllerColliderHit(ControllerColliderHit h)
    {
        if (h.collider.name == "DoorBlock" && msgCooldown <= 0 && GameManager.I != null)
        {
            msgCooldown = 6f;
            GameManager.I.Notify("The doors won't open for you. Not until six.", false);
        }
    }
}
