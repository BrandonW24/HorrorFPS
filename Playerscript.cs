using ML.SDK;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Windows;
using static System.Collections.Specialized.BitVector32;

public class Playerscript : MonoBehaviour
{
    public GameObject[] weaponVisuals;
    public GameObject[] shootingPositions;
    public GameObject[] weaponsVFX;
    public int[] kb_values;
    private Transform playerCamera;
    private GameObject avatarObj;
    private MLPlayer player;
    public Animator mechAni;
    // public GameObject[] ShootingPositions;
    // public GameObject[] ShootingVFX;

    // Start is called before the first frame update
    void Start()
    {
        //   rb = GetComponent<Rigidbody>();
        // rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        StartCoroutine(InitializePlayer());
        //  secondHandScript = (DualWielding)secondHand.GetComponent(typeof(DualWielding));
        /*
        if (AmmoBar != null)
        {
            AmmoBar.maxValue = ammo_max;
            AmmoBar.value = ammo;

            ClipsBar.maxValue = maxClips;
            ClipsBar.value = clips;
        }
        else
        {
            Debug.LogError("HealthBar Slider reference is not set.");
        }
        */
        // AnimatorStateInfo stateInfo = mechAni.GetCurrentAnimatorStateInfo(0);

        ActivateWeapon(currentWeaponIndex); // Activate the first weapon on start


    }


    IEnumerator InitializePlayer()
    {
        while (player == null)
        {
            Debug.Log("Waiting started");
            yield return new WaitForSeconds(2f);
            player = ML.SDK.MassiveLoopRoom.GetLocalPlayer();
            Debug.Log("Checking player...");

            if (player != null)
            {
                Debug.Log("Searching for Player Complete");
                avatarObj = player.AvatarTrackedObject;
               //station.SettlePlayer(player);
            }

            GameObject mainCameraObject = GameObject.FindGameObjectWithTag("MainCamera");
            if (mainCameraObject != null)
            {
                playerCamera = mainCameraObject.transform;
                Debug.Log("Player camera found: " + playerCamera.position);
                gameObject.transform.parent = playerCamera;
            }
            else
            {
                Debug.LogWarning("Waiting for player to be initialized...");
            }
        }
        Debug.Log("Waiting ended");
    }

    public void HandleDesktopModeInput()
    {

    }

    private const float DM_KEY_TRESHOLD = 0.1f;
    public bool isAiming = false;
    private bool isTriggerPressed = false; // Track if the button is being held down
    private bool isGripPressed = false; // Track if the grip button is being held down
    public bool isAutomatic = false;
    private bool isReloading = false; // Flag to check if reloading
    private bool isFiring = false;

    public int ammo = 30; // Bullets per clip
    public int clips = 5; // Total clips available
    public int maxClips = 5;
    public float reloadTime = 2.0f; // Time taken to reload
    public int ammo_max;
    public float fireRate = 0.1f; // Time between shots in automatic mode
    public Transform raycastOrigin; // Transform for custom raycast origin
    public GameObject[] impactEle;
    private GameObject hitgameobject;


    public float[] fireRates;                // Fire rate for each weapon
    public int[] maxAmmoPerWeapon;           // Maximum ammo per clip for each weapon
    public int[] maxClipsPerWeapon;          // Maximum clips for each weapon

    public GameObject impactHole;


    // Switch to the next weapon
    private int currentWeaponIndex = 0; // Track the active weapon

    void SwitchWeapon()
    {
        // Deactivate current weapon
        weaponVisuals[currentWeaponIndex].SetActive(false);

        // Update weapon index
        currentWeaponIndex = (currentWeaponIndex + 1) % weaponVisuals.Length;

        // Activate new weapon
        ActivateWeapon(currentWeaponIndex);
    }

    IEnumerator NonAutomaticFireCooldown()
    {
        isFiring = true;
        yield return new WaitForSeconds(fireRate); // Prevent firing again until fireRate time has passed
        isFiring = false;
    }

    void ActivateWeapon(int index)
    {
        // Activate the new weapon visual
        weaponVisuals[index].SetActive(true);

        // Set the weapon-specific properties
        raycastOrigin = shootingPositions[index].transform;
        fireRate = fireRates[index];
        ammo = maxAmmoPerWeapon[index];
        clips = maxClipsPerWeapon[index];
        ammo_max = maxAmmoPerWeapon[index]; // Track the max ammo for reloading
        kbValue = kb_values[index];

        if (currentWeaponIndex == 0 || currentWeaponIndex == 2 || currentWeaponIndex == 4 || currentWeaponIndex == 7)
        {
            isAutomatic = true;
        }
        else
        {
            isAutomatic = true;
        }

        // Retrieve and assign the Animator from the active weapon
        mechAni = weaponVisuals[index].GetComponent<Animator>();

        if (mechAni == null)
        {
            Debug.LogError("Animator not found on weapon index " + index);
        }
    }

    IEnumerator WaitForReload()
    {

      /*  if (!reloadSound.isPlaying)
        {
            reloadSound.Play();
        }*/

        isReloading = true;
        yield return new WaitForSeconds(2f);
        //  mechAni.SetBool("shoot", false);
        //  mechAni.SetBool("reload", false);
        //  mechAni.SetBool("idle", true);
        AnimatorStateInfo stateInfo = mechAni.GetCurrentAnimatorStateInfo(0);

        if (!stateInfo.IsName("Base Layer.Recharge"))
        {
            mechAni.Play("Base Layer.Recharge", 0, 0);
        }

        ammo = ammo_max; // Reload ammo (reset to full clip)
     //   AmmoBar.value = ammo;
        clips--;
     //   ClipsBar.value = clips;
        isReloading = false;

        Debug.Log("Reloading Done");

    }

    void FireBullet()
    {
        if (ammo > 0)
        {
            if (currentWeaponIndex == 2) // Check if it's the shotgun
            {
                FireShotgunBlast(); // Fire multiple projectiles for the shotgun
            }
            else
            {
                // Regular single bullet firing logic
                FireSingleRay();
            }

            ammo--; // Decrease ammo count after firing
        }
        else if (clips > 0)
        {
            Debug.Log("Out of ammo, reloading...");
            StartCoroutine(WaitForReload());
        }
    }


    GameObject effectInstance = null; // Initialize it to null first
    GameObject effectHole = null;
    public int kbValue = 2;
    void FireSingleRay()
    {
        RaycastHit hit;
        var ray = new Ray(raycastOrigin.position, raycastOrigin.forward);

        // Fire the raycast
        if (Physics.Raycast(ray, out hit))
        {
            var hitGameObject = hit.collider.gameObject;

            // Check the hit object's name to determine the material
            if (hitGameObject.name.Contains("Dirt"))
            {
                // Instantiate dirt impact effect
                effectInstance = Instantiate(impactEle[3], hit.point, Quaternion.identity);
                effectHole = Instantiate(impactHole, hit.point, Quaternion.identity);
            }
            else if (hitGameObject.name.Contains("Metal"))
            {
                // Instantiate metal impact effect
                effectInstance = Instantiate(impactEle[6], hit.point, Quaternion.identity);
                effectHole = Instantiate(impactHole, hit.point, Quaternion.identity);

            }
            else if (hitGameObject.name.Contains("Wood"))
            {
                // Instantiate wood impact effect
                effectInstance = Instantiate(impactEle[10], hit.point, Quaternion.identity);
                effectHole = Instantiate(impactHole, hit.point, Quaternion.identity);

            }
            else if (hitGameObject.name.Contains("Concrete"))
            {
                // Instantiate concrete impact effect
                effectInstance = Instantiate(impactEle[1], hit.point, Quaternion.identity);
                effectHole = Instantiate(impactHole, hit.point, Quaternion.identity);

            }
            else if (hitGameObject.name.Contains("Enemy"))
            {
                // Instantiate concrete impact effect
                effectInstance = Instantiate(impactEle[2], hit.point, Quaternion.identity);
                effectHole = Instantiate(impactHole, hit.point, Quaternion.identity);

            }
            else if (hitGameObject.name.Contains("Water"))
            {
                // Instantiate concrete impact effect
                effectInstance = Instantiate(impactEle[9], hit.point, Quaternion.identity);
                effectHole = Instantiate(impactHole, hit.point, Quaternion.identity);

            }
            else
            {
                // Default impact effect
                effectInstance = Instantiate(impactEle[0], hit.point, Quaternion.identity);
                effectHole = Instantiate(impactHole, hit.point, Quaternion.identity);

            }
            effectHole.transform.LookAt(hit.point + hit.normal);
            effectInstance.transform.LookAt(hit.point + hit.normal);

            Destroy(effectInstance, 20);

            // Set the hit effect parent to the hit object
            effectHole.transform.parent = hitGameObject.transform;

            Destroy(effectHole, 20);

            // Additional logic for hitting specific targets (e.g., drones)
            if (hitGameObject.name.Contains("Drone"))
            {
                Debug.Log("Drone hit!");
                // Apply damage logic here...
            }

            if (hit.rigidbody != null)
            {
                Debug.Log("Found a rigidbody");

                // Calculate knockback direction
                Vector3 knockbackDirection = hit.point - raycastOrigin.position;
                knockbackDirection = knockbackDirection.normalized; // Normalize the vector to get direction only

                // Apply knockback force
                hit.rigidbody.AddForce(knockbackDirection * kbValue, ForceMode.Impulse);
                if (hitGameObject.name.Contains("Docterwho"))
                {
                    hit.rigidbody.AddForce(knockbackDirection * kbValue * 20, ForceMode.Impulse);
                    // Apply damage logic here...
                }
            }
        }

        // Instantiate muzzle flash effect at shooting position
        var muzzleFlash = Instantiate(weaponsVFX[currentWeaponIndex], raycastOrigin.position, raycastOrigin.rotation);
        Destroy(muzzleFlash, 1);
    }

    void FireShotgunBlast()
    {
        int pellets = 8; // Number of pellets fired in one shotgun blast
        float spreadAngle = 5f; // Spread angle of the shotgun blast in degrees

        for (int i = 0; i < pellets; i++)
        {
            Vector3 spreadDirection = GetRandomDirectionInCone(raycastOrigin.forward, spreadAngle);
            RaycastHit hit;
            var ray = new Ray(raycastOrigin.position, spreadDirection);

            // Fire each pellet raycast
            if (Physics.Raycast(ray, out hit))
            {
                var hitGameObject = hit.collider.gameObject;
                // Check the hit object's name to determine the material
                if (hitGameObject.name.Contains("Dirt"))
                {
                    // Instantiate dirt impact effect
                    effectInstance = Instantiate(impactEle[3], hit.point, Quaternion.identity);
                    effectHole = Instantiate(impactHole, hit.point, Quaternion.identity);
                }
                else if (hitGameObject.name.Contains("Metal"))
                {
                    // Instantiate metal impact effect
                    effectInstance = Instantiate(impactEle[6], hit.point, Quaternion.identity);
                    effectHole = Instantiate(impactHole, hit.point, Quaternion.identity);
                }
                else if (hitGameObject.name.Contains("Wood"))
                {
                    // Instantiate wood impact effect
                    effectInstance = Instantiate(impactEle[10], hit.point, Quaternion.identity);
                    effectHole = Instantiate(impactHole, hit.point, Quaternion.identity);
                }
                else if (hitGameObject.name.Contains("Concrete"))
                {
                    // Instantiate concrete impact effect
                    effectInstance = Instantiate(impactEle[1], hit.point, Quaternion.identity);
                    effectHole = Instantiate(impactHole, hit.point, Quaternion.identity);
                }
                else if (hitGameObject.name.Contains("Enemy"))
                {
                    // Instantiate concrete impact effect
                    effectInstance = Instantiate(impactEle[2], hit.point, Quaternion.identity);
                    effectHole = Instantiate(impactHole, hit.point, Quaternion.identity);
                }
                else if (hitGameObject.name.Contains("Water"))
                {
                    // Instantiate concrete impact effect
                    effectInstance = Instantiate(impactEle[9], hit.point, Quaternion.identity);
                    effectHole = Instantiate(impactHole, hit.point, Quaternion.identity);
                }
                else
                {
                    // Default impact effect
                    effectInstance = Instantiate(impactEle[0], hit.point, Quaternion.identity);
                    effectHole = Instantiate(impactHole, hit.point, Quaternion.identity);
                }

                effectHole.transform.LookAt(hit.point + hit.normal);
                effectInstance.transform.LookAt(hit.point + hit.normal);

                Destroy(effectInstance, 20);

                // Set the hit effect parent to the hit object
                effectHole.transform.parent = hitGameObject.transform;

                Destroy(effectHole, 20);

                // Set the hit effect parent to the hit object
               // effectInstance.transform.parent = hitGameObject.transform;

                // Additional logic for hitting specific targets (e.g., drones)
                if (hitGameObject.name.Contains("Drone"))
                {
                    Debug.Log("Drone hit by pellet!");
                    // Apply damage logic here...
                }

                if (hit.rigidbody != null)
                {
                    Debug.Log("Found a rigidbody");

                    // Calculate knockback direction
                    Vector3 knockbackDirection = hit.point - raycastOrigin.position;
                    knockbackDirection = knockbackDirection.normalized; // Normalize the vector to get direction only

                    // Apply knockback force
                    hit.rigidbody.AddForce(knockbackDirection * kbValue, ForceMode.Impulse);

                    if (hitGameObject.name.Contains("Docterwho"))
                    {
                        hit.rigidbody.AddForce(knockbackDirection * kbValue * 20, ForceMode.Impulse);
                        // Apply damage logic here...
                    }
                }
            }
        }

        // Instantiate muzzle flash effect at shooting position
        var muzzleFlash = Instantiate(weaponsVFX[currentWeaponIndex], raycastOrigin.position, raycastOrigin.rotation);
        Destroy(muzzleFlash, 1);
    }

    // Function to calculate a random direction within a cone
    Vector3 GetRandomDirectionInCone(Vector3 forward, float angle)
    {
        Quaternion randomRotation = Quaternion.Euler(
            Random.Range(-angle, angle),
            Random.Range(-angle, angle),
            0);

        return randomRotation * forward;
    }
    IEnumerator AutomaticFire()
    {
        // mechAni.SetBool("shoot", true);
        //  mechAni.SetBool("idle", false);
        while (isFiring)
        {
            FireBullet();
            yield return new WaitForSeconds(fireRate);
        }
    }

    // Variables for sliding
    /*Not used
     * 
    private bool isSliding = false;
    private float slideSpeed = 10f;  // Increase the speed when sliding
    private float normalSpeed = 5f;  // Normal walking/running speed
    private float slideDuration = 2f;  // Slide duration in seconds
    private float slideTimeElapsed = 0f;
    */

    //Getting the material the player is standing on to play the correct sound effects.
    public AudioClip[] footStepSFXConcrete;
    public AudioClip[] footStepSFXDirt;
    public AudioClip[] footStepSFXMud;
    public AudioClip[] footStepSFXGrass;
    public AudioClip[] footStepSFXMetal;
    public AudioClip[] footStepSFXWater;
    public AudioClip[] footStepSFXWood;
    public string currentStandingMaterial;
    public AudioSource footAutoSource;

    
    //For this, I would normally use a switch case statement here, but for some reason
    //that was not producing reliable results.
    void PlayFootstepSound()
    {
        // Normalize the string by trimming spaces and converting to lowercase
        string material = currentStandingMaterial.Trim().ToLower();
        Debug.Log("Current material (normalized): " + material);

        AudioClip clipToPlay = null;

        // Choose the correct audio clip array based on the material using if-else
        if (material == "concrete")
        {
            Debug.Log("Concrete material detected");
            clipToPlay = footStepSFXConcrete[Random.Range(0, footStepSFXConcrete.Length)];
        }
        else if (material == "dirt")
        {
            Debug.Log("Dirt material detected");
            clipToPlay = footStepSFXDirt[Random.Range(0, footStepSFXDirt.Length)];
        }
        else if (material == "mud")
        {
            Debug.Log("Mud material detected");
            clipToPlay = footStepSFXMud[Random.Range(0, footStepSFXMud.Length)];
        }
        else if (material == "grass")
        {
            Debug.Log("Grass material detected");
            clipToPlay = footStepSFXGrass[Random.Range(0, footStepSFXGrass.Length)];
        }
        else if (material == "metal")
        {
            Debug.Log("Metal material detected");
            clipToPlay = footStepSFXMetal[Random.Range(0, footStepSFXMetal.Length)];
        }
        else if (material == "water")
        {
            Debug.Log("Water material detected");
            clipToPlay = footStepSFXWater[Random.Range(0, footStepSFXWater.Length)];
        }
        else if (material == "wood")
        {
            Debug.Log("Wood material detected");
            clipToPlay = footStepSFXWood[Random.Range(0, footStepSFXWood.Length)];
        }
        else
        {
            Debug.LogWarning($"Unrecognized material: {material}");
        }

        // Log the selected clip before playing
        if (clipToPlay != null)
        {
            Debug.Log("Clip to play: " + clipToPlay.name);
            footAutoSource.PlayOneShot(clipToPlay);
        }
        else
        {
            Debug.LogWarning("No clip selected for material: " + material);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Trigger entered with: " + other.name);

        if (other.name.ToLower().Contains("metal"))
        {
            currentStandingMaterial = "metal";
        }
        else if (other.name.ToLower().Contains("dirt"))
        {
            currentStandingMaterial = "dirt";
        }
        else if (other.name.ToLower().Contains("concrete"))
        {
            currentStandingMaterial = "concrete";
        }
        else if (other.name.ToLower().Contains("grass"))
        {
            currentStandingMaterial = "grass";
        }
        else if (other.name.ToLower().Contains("wood"))
        {
            currentStandingMaterial = "wood";
        }
        else if (other.name.ToLower().Contains("water"))
        {
            currentStandingMaterial = "water";
        }
        else if (other.name.ToLower().Contains("mud"))
        {
            currentStandingMaterial = "mud";
        }

        Debug.Log("Material set to: " + currentStandingMaterial);
    }


    private float footstepTimer = 0f;  // Timer to control the interval between steps
    private float footstepInterval = 0.5f;  // Interval between steps (in seconds)

    // Update is called once per frame
    void Update()
    {
        footstepTimer -= Time.deltaTime;

        if (player != null)
        {
            AnimatorStateInfo stateInfo = mechAni.GetCurrentAnimatorStateInfo(0);
            if (MassiveLoopClient.IsInDesktopMode)
            {

                if (player.UserInput != null)
                {
                    // Handle right mouse button (TriggerPress2) for aiming
                    if (player.UserInput.TriggerPress2 && !isTriggerPressed)
                    {
                        // Debug.Log("TriggerPress2 pressed");
                        isTriggerPressed = true; // Set to true to indicate the button is held down

                        if (!isAiming)
                        {
                            if (!stateInfo.IsName("Base Layer.Aiming_Idle"))
                            {
                                mechAni.Play("Base Layer.Aiming_Idle", 0, 0);
                                isAiming = true;
                                mechAni.SetBool("isAiming", true);
                            }
                        }
                        else
                        {
                            if (!stateInfo.IsName("Base Layer.idle"))
                            {
                                mechAni.Play("Base Layer.idle", 0, 0);
                                isAiming = false;
                                mechAni.SetBool("isAiming", false);
                            }
                        }
                    }
                    // Ensure the toggle only happens when the button is released
                    else if (!player.UserInput.TriggerPress2 && isTriggerPressed)
                    {
                        isTriggerPressed = false; // Reset once the button is released
                    }

                    // Handle left mouse button (TriggerPress1) for shooting
                    if (player.UserInput.TriggerPress1)
                    {
                        if (isReloading) return; // Do nothing if reloading

                        // Debug.Log("TriggerPress1 pressed");
                        if (!isAiming)
                        {
                            if (!stateInfo.IsName("Base Layer.Singl_Shot"))
                            {
                                mechAni.Play("Base Layer.Singl_Shot", 0, 0);
                            }
                        }
                        else
                        {
                            if (!stateInfo.IsName("Base Layer.Aiming_Shot"))
                            {
                                mechAni.Play("Base Layer.Aiming_Shot", 0, 0);
                            }
                        }

                        if (isAutomatic)
                        {
                            if (!isFiring)
                            {
                                isFiring = true;
                                StartCoroutine(AutomaticFire());
                            }
                        }
                        else
                        {
                            FireBullet();
                            StartCoroutine(NonAutomaticFireCooldown());
                            isFiring = false;
                        }

                    }
                    else
                    {
                        if (isAutomatic)
                        {
                            isFiring = false;
                        }
                    }

                    if (player.UserInput.CTRL)
                    {
                        //  Debug.Log("CTRL pressed");
                        if (!stateInfo.IsName("Base Layer.Recharge"))
                        {
                            mechAni.Play("Base Layer.Recharge", 0, 0);
                        }
                    }

                    //Crouch check
                    if (player.UserInput.CPress)
                    {
                       // Debug.Log("C pressed");
                    }

                    // Forward movement
                    if (player.UserInput.Joy1.y > DM_KEY_TRESHOLD || player.UserInput.Joy1.y < -DM_KEY_TRESHOLD || player.UserInput.Joy1.x > DM_KEY_TRESHOLD || player.UserInput.Joy1.x < -DM_KEY_TRESHOLD)
                    {
                        // Handle normal walking animation
                        if (!isAiming && !stateInfo.IsName("Base Layer.Walk"))
                        {
                            mechAni.Play("Base Layer.Walk", 0, 0);
                        }
                        else if (isAiming && !stateInfo.IsName("Base Layer.Aiming_Walk"))
                        {
                            mechAni.Play("Base Layer.Aiming_Walk", 0, 0);
                        }

                        // If it's time to play the next footstep sound
                        if (footstepTimer <= 0f)
                        {
                            Debug.Log("Attempting to play sound");
                            PlayFootstepSound();  // Play footstep sound
                            footstepTimer = footstepInterval;  // Reset timer for next footstep sound
                        }
                    }
                    else
                    {
                        // Stop footsteps when not moving
                        footstepTimer = 0f;
                    }

                    // Handle weapon switching with Grip1
                    if (player.UserInput.Grip1 > DM_KEY_TRESHOLD && !isGripPressed)
                    {
                        isGripPressed = true; // Button pressed
                        SwitchWeapon();
                    }
                    else if (player.UserInput.Grip1 <= DM_KEY_TRESHOLD && isGripPressed)
                    {
                        isGripPressed = false; // Reset once button is released
                    }


                    if (player.UserInput.Grip2 > DM_KEY_TRESHOLD)
                    {
                        Debug.Log("Grip 2");

                    }
                }
            }
            else
            {


            }
        }


    }
}
