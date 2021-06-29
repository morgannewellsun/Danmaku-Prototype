using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//****************************************************************************************************************************************************************************************************************************************************

public class PlayerInputManager
// Class used to manage raw player inputs. 
// This allows input customization to be abstracted even further than the Unity input manager allows.
// This class performs necessary edge detection and preprocessing on all player input systems.
{
    private int rawInputCount;
    private int parsedInputCount;

    private int[] rawInputsOld;
    private int[] rawInputsNew;
    private int[] parsedInputs;

    public PlayerInputManager()
    {
        rawInputCount = 8;
        parsedInputCount = 8;
        rawInputsOld = new int[rawInputCount];
        rawInputsNew = new int[rawInputCount];
        parsedInputs = new int[parsedInputCount];
    }

    private void GetNewRawInputs()
    // Gets and updates raw inputs. Intended to be called by UpdateInputs() only.
    {
        int[] inputs = new int[rawInputCount];

        // Get raw inputs using Unity input manager.
        inputs[0] = Input.GetButton("up") ? 1 : 0;
        inputs[1] = Input.GetButton("down") ? 1 : 0;
        inputs[2] = Input.GetButton("left") ? 1 : 0;
        inputs[3] = Input.GetButton("right") ? 1 : 0;
        inputs[4] = Input.GetButton("shoot") ? 1 : 0;
        inputs[5] = Input.GetButton("dash") ? 1 : 0;
        inputs[6] = Input.GetButton("focus") ? 1 : 0;
        inputs[7] = Input.GetButton("bomb") ? 1 : 0;

        // Push new inputs onto input queue.
        rawInputsOld = rawInputsNew;
        rawInputsNew = inputs;
    }

    private void ParseNewRawInputs()
    // Parses raw inputs into a more useful form, depending on what they do. Intended to be called by UpdateInputs() only.
    {
        int[] parsed = new int[parsedInputCount];

        // Net up/down and left/right.
        parsed[0] = rawInputsNew[0] - rawInputsNew[1];
        parsed[1] = rawInputsNew[3] - rawInputsNew[2];

        // Shooting state is 1 if button is held down, -1 if just released, 0 otherwise.
        if (rawInputsNew[4] == 1) { parsed[2] = 1; }
        else if (rawInputsOld[4] > rawInputsNew[4]) { parsed[2] = -1; }
        else { parsed[2] = 0; }

        // Dash state is 1 if button was just pressed, 0 otherwise.
        if (rawInputsOld[5] < rawInputsNew[5]) { parsed[3] = 1; }
        else { parsed[3] = 0; }

        // Focus state needs no parsing.
        parsed[4] = rawInputsNew[6];

        // Bomb state is 1 if button was just pressed, 0 otherwise.
        if (rawInputsOld[7] < rawInputsNew[7]) { parsed[5] = 1; }
        else { parsed[5] = 0; }

        // Retain most recent (X, Y) != (0, 0).
        if (!(parsed[0] == 0 && parsed[1] == 0))
        {
            parsed[6] = parsed[0];
            parsed[7] = parsed[1];
        }
        else
        {
            parsed[6] = parsedInputs[6];
            parsed[7] = parsedInputs[7];
        }

        // Update parsed input attribute.
        parsedInputs = parsed;
    }

    public void UpdateInputs()
    // Gets raw inputs, then parses them. Call this function once per graphics frame.
    {
        GetNewRawInputs();
        ParseNewRawInputs();
    }

    public int GetInputY()
    // Gets parsed Y input.
    { return parsedInputs[0]; }

    public int GetInputX()
    // Gets parsed X input.
    { return parsedInputs[1]; }

    public Vector2 GetInputDirection()
    // Gets Vector2 direction of current inputs, normalized to length 1.
    {
        Vector2 direction = new Vector2(GetInputX(), GetInputY());
        return direction.normalized;
    }

    public int GetInputXPrevNonZero()
    // From the most recent (X, Y) != (0, 0), gets X.
    { return parsedInputs[7]; }

    public int GetInputYPrevNonZero()
    // From the most recent (X, Y) != (0, 0), gets Y.
    { return parsedInputs[6]; }

    public Vector2 GetInputDirectionNonZero()
    // From the most recent (X, Y) != (0, 0), gets the normalized direction.
    {
        Vector2 direction = new Vector2(GetInputXPrevNonZero(), GetInputYPrevNonZero());
        return direction.normalized;
    }

    public int GetInputShoot()
    // Gets parsed shooting input.
    { return parsedInputs[2]; }

    public int GetInputDash()
    // Gets parsed dash input.
    { return parsedInputs[3]; }

    public int GetInputFocus()
    // Gets parsed focus input.
    { return parsedInputs[4]; }

    public int GetInputBomb()
    // Gets parsed bomb input.
    { return parsedInputs[5]; }
}

//****************************************************************************************************************************************************************************************************************************************************

public class StatusEffect
// Class representing a status effect label.
{
    public float startTime;
    public float endTime;
    public string name;

    public StatusEffect(string n, float s, float e)
    {
        name = n;
        startTime = s;
        endTime = e;
    }
}

//****************************************************************************************************************************************************************************************************************************************************

public class StatusEffectManager
// Class used to manage any "status effects" on the player. 
// This system should be used to keep track of all temporary "states" that a player can enter (e.g. ability cooldowns, temporary invulnerability)
// Note that this system merely keeps tracks of the status effect LABELS, but does not implement the effects of specific status effects.
{
    private List<StatusEffect> effectList;

    public StatusEffectManager()
    {
        effectList = new List<StatusEffect>();
    }

    public void CullEffects()
    // Removes status effects that have expired.
    {
        int i = 0;
        while (i < effectList.Count)
        {
            if (effectList[i].endTime < Time.time)
            {
                // GameObject.Destroy(effectList[i]);
                effectList.RemoveAt(i);
            }
            else
            {
                i = i + 1;
            }
        }
    }

    public bool CheckEffect(string name)
    // Returns whether or not the player is affected by the specified status effect.
    {
        foreach (StatusEffect effect in effectList)
        {
            if (effect.name == name && effect.startTime < Time.time && effect.endTime > Time.time)
            {
                return (true);
            }
        }
        return (false);
    }

    public float CheckEffectDuration(string name)
    // Returns the total duration of the specified status effect, if the player is affected by it. If the player is not affected by the specified status effect, 0 is returned.
    {
        foreach (StatusEffect effect in effectList)
        {
            if (effect.name == name && effect.startTime < Time.time && effect.endTime > Time.time)
            {
                return (effect.endTime - effect.startTime);
            }
        }
        return (0);
    }

    public float CheckEffectProgress(string name)
    // Returns the fraction of elapsed duration of the specified status effect, if the player is affected by it. If the player is not affected by the specified status effect, 0 is returned.
    {
        foreach (StatusEffect effect in effectList)
        {
            if (effect.name == name && effect.startTime < Time.time && effect.endTime > Time.time)
            {
                return ((Time.time - effect.startTime) / (effect.endTime - effect.startTime));
            }
        }
        return (0);
    }

    public bool AddEffect(string name, float duration, bool force = false)
    // If the player is not affected by the specified effect, adds a status effect to the player with specified name and duration, and returns true.
    // Otherwise: If force == false, returns false. If force == true, deletes the previous effect and adds a new one, then returns true.
    {
        if (CheckEffect(name))
        {
            if (force)
            {
                RemoveEffect(name);
                effectList.Add(new StatusEffect(name, Time.time, Time.time + duration));
                return (true);
            }
            else
            {
                return (false);
            }
        }
        else
        {
            effectList.Add(new StatusEffect(name, Time.time, Time.time + duration));
            return (true);
        }
    }

    public bool RemoveEffect(string name)
    // If the player is affected by the specified effect, removes it and returns true. Otherwise, returns false.
    {
        foreach (StatusEffect effect in effectList)
        {
            if (effect.name == name)
            {
                effectList.Remove(effect);
                return (true);
            }
        }
        return (false);
    }
}

//****************************************************************************************************************************************************************************************************************************************************

public class PlayerScript : MonoBehaviour
// Core script controlling the player. Intended to be a singleton (not enforced due to laziness).
{
    // OBJECTS

    private Rigidbody2D playerRigidbody;                // Player's attached Rigidbody2D component.
    private CircleCollider2D playerCollider;            // Player's attached CircleCollider2D component.
    private Animator playerAnimator;                    // Player's attached Animator component.

    private PlayerInputManager playerInputs;            // Input parsing manager.
    public StatusEffectManager playerStatusEffects;     // Player's status effect label manager.

    // PARAMETER VARIABLES

    private float playerParamBulletHitboxRadius;        // Radius of the player's bullet hitbox. Smaller than the radius of the collider used for terrain.

    private float playerParamMovementSpeedFocused;      // Focused movement speed. Slower.
    private float playerParamMovementSpeedUnfocused;    // Unfocused movement speed. Faster.

    private int playerParamHealthTotal;                 // Player's maximum health.
    private float playerParamHealthInvulnTime;          // Duration of the player's invulnerability after being hit.

    private float playerParamDashCooldownTime;          // Cooldown time for the player's dash.
    private float playerParamDashDistance;              // Distance the player dashes.
    private float playerParamDashDistanceMinimum;       // Minimum distance the player can dash before the dash is considered invalid.
    private float playerParamDashDuration;              // Time the animation for a full-distance dash lasts.

    // STATE VARIABLES

    private int playerStateHealthRemaining;             // Player's current health.

    private float[] playerStateDashDistances;           // Maximum current dash distances for all eight directions.
    private Vector2 playerStateDashOrigin;              // Dash origin position.
    private Vector2 playerStateDashTarget;              // Dash target position.

    void Start()
    // Unity built-in. Called upon script initialization.
    {
        // OBJECTS

        playerRigidbody = GetComponent<Rigidbody2D>();
        playerCollider = GetComponent<CircleCollider2D>();
        playerAnimator = GetComponent<Animator>();

        playerInputs = new PlayerInputManager();
        playerStatusEffects = new StatusEffectManager();

        // PARAMETER VARIABLES

        playerParamBulletHitboxRadius = 0.075f;
        
        playerParamMovementSpeedFocused = 2f;
        playerParamMovementSpeedUnfocused = 5f;

        playerParamHealthTotal = 3;
        playerParamHealthInvulnTime = 2f;

        playerParamDashCooldownTime = 1f;
        playerParamDashDistance = 3f;
        playerParamDashDistanceMinimum = 0.01f;
        playerParamDashDuration = 0.1f;

        // STATE VARIABLES

        playerStateHealthRemaining = playerParamHealthTotal;

        playerStateDashDistances = new float[] { 0, 0, 0, 0, 0, 0, 0, 0 };
        playerStateDashOrigin = new Vector2();
        playerStateDashTarget = new Vector2();
    }

    private void ApplyInputVelocity()
    // Applies a velocity to the player's rigidbody based on focus and movement inputs. Intended to be called by ProcessMovement() only.
    {
        float speed = playerInputs.GetInputFocus() == 0 ? playerParamMovementSpeedUnfocused : playerParamMovementSpeedFocused;
        playerRigidbody.velocity = playerInputs.GetInputDirection() * speed;
    }

    private void ApplyMovementAnimation()
    // Applies animations for player movement. Intended to be called by ProcessMovement() only.
    {
        if (playerInputs.GetInputX() == 0 && playerInputs.GetInputY() == 0)
        {
            if (playerInputs.GetInputXPrevNonZero() == 0 && playerInputs.GetInputYPrevNonZero() == -1)
            { playerAnimator.Play("player_idle_down",0,0f); }
            else if (playerInputs.GetInputXPrevNonZero() == 1 && playerInputs.GetInputYPrevNonZero() == -1)
            { playerAnimator.Play("player_idle_downright"); }
            else if (playerInputs.GetInputXPrevNonZero() == -1 && playerInputs.GetInputYPrevNonZero() == -1)
            { playerAnimator.Play("player_idle_downleft"); }
            else if (playerInputs.GetInputXPrevNonZero() == 1 && playerInputs.GetInputYPrevNonZero() == 0)
            { playerAnimator.Play("player_idle_right"); }
            else if (playerInputs.GetInputXPrevNonZero() == -1 && playerInputs.GetInputYPrevNonZero() == 0)
            { playerAnimator.Play("player_idle_left"); }
            else if (playerInputs.GetInputXPrevNonZero() == 0 && playerInputs.GetInputYPrevNonZero() == 1)
            { playerAnimator.Play("player_idle_up"); }
            else if (playerInputs.GetInputXPrevNonZero() == 1 && playerInputs.GetInputYPrevNonZero() == 1)
            { playerAnimator.Play("player_idle_upright"); }
            else if (playerInputs.GetInputXPrevNonZero() == -1 && playerInputs.GetInputYPrevNonZero() == 1)
            { playerAnimator.Play("player_idle_upleft"); }
        } else
        {
            if (playerInputs.GetInputX() == 0 && playerInputs.GetInputY() == -1)
            { playerAnimator.Play("player_walk_down"); }
            else if (playerInputs.GetInputX() == 1 && playerInputs.GetInputY() == -1)
            { playerAnimator.Play("player_walk_downright"); }
            else if (playerInputs.GetInputX() == -1 && playerInputs.GetInputY() == -1)
            { playerAnimator.Play("player_walk_downleft"); }
            else if (playerInputs.GetInputX() == 1 && playerInputs.GetInputY() == 0)
            { playerAnimator.Play("player_walk_right"); }
            else if (playerInputs.GetInputX() == -1 && playerInputs.GetInputY() == 0)
            { playerAnimator.Play("player_walk_left"); }
            else if (playerInputs.GetInputX() == 0 && playerInputs.GetInputY() == 1)
            { playerAnimator.Play("player_walk_up"); }
            else if (playerInputs.GetInputX() == 1 && playerInputs.GetInputY() == 1)
            { playerAnimator.Play("player_walk_upright"); }
            else if (playerInputs.GetInputX() == -1 && playerInputs.GetInputY() == 1)
            { playerAnimator.Play("player_walk_upleft"); }
        }
        
    }

    private void ProcessMovement()
    // Highest-level movement function. Intended to be called by Update() only.
    {
        if (!playerStatusEffects.CheckEffect("no movement"))
        {
            ApplyInputVelocity();
            ApplyMovementAnimation();
        } else
        {
            playerRigidbody.velocity = Vector2.zero;
        }
    }

    private void ProcessShooting()
    // Highest-level shooting function. Intended to be called by Update() only.
    {
        //placeholder
    }

    private bool CheckDashValidity(Vector2 direction, float distance)
    // Checks to see if a dash with specified direction and distance is valid. Returns true if dash is valid. Intended to be called by UpdateDashDistances() only.
    {
        // Check to see if the path to the target is clear.
        RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, distance, LayerMask.GetMask(new string[] { "Enemies", "Walls" }));
        if (hit.collider == null)
        {
            // If path to target is clear, check area around target using eight raycasts.
            Vector2 targetPosition = new Vector2(transform.position.x, transform.position.y) + direction.normalized * distance;
            Vector2[] directions = new Vector2[8];
            directions[0] = Vector2.right;
            directions[1] = Vector2.right + Vector2.up;
            directions[2] = Vector2.up;
            directions[3] = Vector2.up + Vector2.left;
            directions[4] = Vector2.left;
            directions[5] = Vector2.left + Vector2.down;
            directions[6] = Vector2.down;
            directions[7] = Vector2.down + Vector2.right;
            bool hitRadial = false;
            for (int i = 0; i < 8; i++)
            {
                hit = Physics2D.Raycast(targetPosition, directions[i], playerCollider.radius, LayerMask.GetMask(new string[] { "Enemies", "Walls" }));
                if (hit.collider != null) { hitRadial = true; }
            }
            if (hitRadial) { return false; }
            else { return true; }
        }
        else
        {
            return false;
        }
    }

    private void UpdateDashDistances()
    // Finds the maximum dash distance in all eight directions and saves in a state array. Intended to be called by Maintenance() only.
    {
        float[] distances = new float[8];
        for (int i = 0; i < 8; i++) { distances[i] = playerParamDashDistance; }
        Vector2[] directions = new Vector2[8];
        directions[0] = Vector2.right;
        directions[1] = Vector2.right + Vector2.up;
        directions[2] = Vector2.up;
        directions[3] = Vector2.up + Vector2.left;
        directions[4] = Vector2.left;
        directions[5] = Vector2.left + Vector2.down;
        directions[6] = Vector2.down;
        directions[7] = Vector2.down + Vector2.right;
        for (int i = 0; i < 8; i++)
        {
            // Perform binary search for closest dash distance.
            int maxPower = 20;
            for (int power = 1; power <= maxPower; power++)
            {
                if (!CheckDashValidity(directions[i], distances[i]))
                {
                    distances[i] -= playerParamDashDistance * (float)(System.Math.Pow(0.5f, power));
                } else
                {
                    float newDistance = distances[i] + playerParamDashDistance * (float)(System.Math.Pow(0.5f, power));
                    if (newDistance <= playerParamDashDistance) { distances[i] += playerParamDashDistance * (float)(System.Math.Pow(0.5f, power)); }
                }
            }
            // Make sure the search ends on a valid final distance. If the player is pressed against a wall, this will reduce it to zero.
            if (!CheckDashValidity(directions[i], distances[i])) { distances[i] -= playerParamDashDistance * (float)(System.Math.Pow(0.5f, maxPower)); }
        }
        playerStateDashDistances = distances;
    }

    private void ExecuteDashValid(float distance)
    // Executes a dash in the direction the player is currently facing using the maximum distance calculated by UpdateDashDistances() (which must be run prior). Intended to be called by ProcessDash() only.
    {
        playerStatusEffects.AddEffect("dash cooldown", playerParamDashCooldownTime);
        playerStatusEffects.AddEffect("dashing", playerParamDashDuration * distance / playerParamDashDistance);
        playerStatusEffects.AddEffect("no movement", playerParamDashDuration * distance / playerParamDashDistance);
        playerStatusEffects.AddEffect("invulnerable", playerParamDashDuration * distance / playerParamDashDistance);
        playerStatusEffects.AddEffect("time stop", playerParamDashDuration * distance / playerParamDashDistance);

        playerStateDashOrigin = transform.position;
        playerStateDashTarget = ((Vector2) transform.position) + playerInputs.GetInputDirection() * distance;
    }

    private void ExecuteDashCooldown()
    // Gives the player feedback that the dash is on cooldown. Intended to be called by ProcessDash() only.
    {
        //placeholder
        Debug.Log("dash on cooldown");
    }

    private void ExecuteDashInvalid()
    // Gives the player feedback that a dash is invalid. Intended to be called by ProcessDash() only.
    {
        //placeholder
        Debug.Log("dash invalid");
    }

    private void ProcessDash()
    // Highest-level dashing function. Intended to be called by Update() only.
    {
        if (playerStatusEffects.CheckEffect("dashing"))
        {
            transform.position = Vector2.Lerp(playerStateDashOrigin, playerStateDashTarget, playerStatusEffects.CheckEffectProgress("dashing"));
        }
        else if ((playerInputs.GetInputDash() == 1) && !playerStatusEffects.CheckEffect("no dash"))
        {
            if (!playerStatusEffects.CheckEffect("dash cooldown"))
            {
                float distance = 0;
                if (playerInputs.GetInputX() == 1 && playerInputs.GetInputY() == 0) { distance = playerStateDashDistances[0]; }
                else if (playerInputs.GetInputX() == 1 && playerInputs.GetInputY() == 1) { distance = playerStateDashDistances[1]; }
                else if (playerInputs.GetInputX() == 0 && playerInputs.GetInputY() == 1) { distance = playerStateDashDistances[2]; }
                else if (playerInputs.GetInputX() == -1 && playerInputs.GetInputY() == 1) { distance = playerStateDashDistances[3]; }
                else if (playerInputs.GetInputX() == -1 && playerInputs.GetInputY() == 0) { distance = playerStateDashDistances[4]; }
                else if (playerInputs.GetInputX() == -1 && playerInputs.GetInputY() == -1) { distance = playerStateDashDistances[5]; }
                else if (playerInputs.GetInputX() == 0 && playerInputs.GetInputY() == -1) { distance = playerStateDashDistances[6]; }
                else if (playerInputs.GetInputX() == 1 && playerInputs.GetInputY() == -1) { distance = playerStateDashDistances[7]; }

                if ((distance > playerParamDashDistanceMinimum) || (playerInputs.GetInputX() == 0 && playerInputs.GetInputY() == 0))
                {
                    ExecuteDashValid(distance);
                } else
                {
                    ExecuteDashInvalid();
                }

            } else
            {
                ExecuteDashCooldown();
            }
        }
    }

    private void ProcessBomb()
    // Highest-level bomb function. Intended to be called by Update() only.
    {
        //placeholder
    }

    private void Maintenance()
    // Highest-level general maintainence/updating function. Intended to be called by Update() only.
    {
        playerInputs.UpdateInputs();
        playerStatusEffects.CullEffects();
        UpdateDashDistances();
    }

    void Update()
    // Unity built-in. Called every frame.
    {
        Maintenance();
        ProcessMovement();
        ProcessShooting();
        ProcessDash();
        ProcessBomb();
    }

    private void ZeroHealth()
    // Called when the player drops to zero health remaining. To be called by ApplyDamage only.
    {
        // Placeholder
        playerStateHealthRemaining = playerParamHealthTotal;
        transform.position = new Vector2(22.5f, -31.5f);
        Debug.Log("death");
    }

    private void ApplyDamage()
    // Called when the player takes one damage. To be called by CollisionBullet and CollisionEnemy only.
    {
        Debug.Log("damage taken");
        playerStateHealthRemaining = Mathf.Max(playerStateHealthRemaining - 1, 0);
        playerStatusEffects.AddEffect("invulnerable", playerParamHealthInvulnTime);
        if (playerStateHealthRemaining == 0) { ZeroHealth(); }
    }

    private void CollisionBullet(GameObject bullet)
    // Handles a player collision with an enemy bullet. To be called by OnCollisionStay2D only.
    {
        if (!playerStatusEffects.CheckEffect("invulnerable"))
        {
            ApplyDamage();
            Destroy(bullet);
        }
    }

    void OnTriggerStay2D(Collider2D col)
    // Unity built-in. Called every frame during collider collision.
    {
        if (col.gameObject.layer == LayerMask.NameToLayer("Bullets"))
        {
            // The player's terrain collider is substantially larger than the player's bullet hitbox, and thus is only used to determine which bullets to check distance for.
            if (Vector2.Distance(transform.position, col.gameObject.transform.position) <= playerParamBulletHitboxRadius + col.gameObject.GetComponent<CircleCollider2D>().radius)
            {
                CollisionBullet(col.gameObject);
            }
        }
    }
}
