using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * This script holds any reusable stuff that doesn't fit elsewhere.
 */

public class UtilityScript : MonoBehaviour
{

}

//****************************************************************************************************************************************************************************************************************************************************

public struct Shot
// Struct representing data needed for a single bullet shot of an AttackPattern.
// Intended to be owned in a list by an AttackPattern.
{
    public float shotParamTime;                         // The time at which this shot will be fired. The first shot of any AttackPattern fires at t=0.
    public int shotParamSpawnIndex;                     // The location to spawn this bullet at. Expressed as an index of the owner AttackPattern's patternParamSpawns.
    public int shotParamBulletIndex;                    // The bullet prefab to use. Expressed as an index of the owner AttackPattern's patternParamBulletZoo.
    public Dictionary<string, float> shotParamConfig;   // Dictionary of extra parameters to pass to the selected bullet prefab's initialization function.

    public Shot(float time, int spawnIndex, int bulletIndex, Dictionary<string, float> config = null)
    {
        shotParamTime = time;
        shotParamSpawnIndex = spawnIndex;
        shotParamBulletIndex = bulletIndex;
        shotParamConfig = config;
    }
}

//****************************************************************************************************************************************************************************************************************************************************

public class AttackPattern
// Class used to represent a timed sequence of bullet shots from a specific enemy.
// The second-smallest unit of enemy behavior control. The only smaller unit is the individual bullet.
// Mutating instances of this class after initialization is not an anticipated use case.
// However, modification functions are exposed to support more convenient initalization procedures.
{
    private float patternParamDuration;                             // Total duration of the attack pattern.
    private List<Shot> patternParamShots = new List<Shot>();        // Timeline of individual shots conprising the attack pattern.

    public AttackPattern() { }

    public void AddShot(float postDelay, int spawnIndex, int bulletIndex, Dictionary<string, float> config = null)
    // Appends a shot to the attack pattern. Intended for use while assembling an attack pattern object.
    {
        patternParamShots.Add(new Shot(patternParamDuration, spawnIndex, bulletIndex, config));
        patternParamDuration += postDelay;
    }

    public float GetDuration()
    {
        return (patternParamDuration);
    }

    public Shot GetShot(int i)
    {
        return (patternParamShots[i]);
    }

    public int GetCount()
    {
        return (patternParamShots.Count);
    }
}

//****************************************************************************************************************************************************************************************************************************************************

public class BulletManager
// Class used by individual enemies to manage the bullets they shoot.
// Responsible for low-level manipulation of bullets shot by an individual enemy.
// Provides abstractions used by EnemyScript to control bullets, e.g. shooting and deletion.
// Also provides convenient ways to access aggregate information and perform aggregate operations.
// This class does not interact with AttackPatterns in any way. Interfacing between AttackPatterns and BulletManagers is the responsibility of EnemyScript.
{
    private static GameObject[] bulletZoo;      // Palette of bullet prefabs, which will be loaded from Resources.
    private List<GameObject> bulletList;        // List of bullet gameobjects shot by the enemy this BulletManager belongs to.
    private List<Vector2> bulletSpawns;         // Palette of bullet spawn locations, expressed as world-space Vector2 positions.

    static BulletManager()
    // Initializes shared bullet zoo by reading from Resources/Prefabs/Bullets.
    // These assets are loaded in alphabetical order, thus, in order to avoid changing index:bullet mappings when adding new bullets, all files in this folder are named "<index>_<descriptive name>".
    {
        bulletZoo = Resources.LoadAll<GameObject>("Prefabs/Bullets");
    }

    public BulletManager(List<Vector2> spawns)
    {
        bulletList = new List<GameObject>();
        bulletSpawns = spawns;
    }

    public void Shoot(Shot shot)
    // Shoots a Shot. Does not check the timestamp of the shot.
    {
        GameObject new_bullet = GameObject.Instantiate(bulletZoo[shot.shotParamBulletIndex]);
        new_bullet.GetComponent<BulletScript>().Initialize(bulletSpawns[shot.shotParamSpawnIndex], shot.shotParamConfig);
        bulletList.Add(new_bullet);
    }

    public void CullBullets()
    // Remove all bullets that have been marked for deletion.
    // Call every frame.
    {
        int i = 0;
        while (i<bulletList.Count)
        {
            if (bulletList[i] == null || bulletList[i].GetComponent<BulletScript>().ReadyForDeletion())
            {
                GameObject.Destroy(bulletList[i]);
                bulletList.RemoveAt(i);
            }
            else
            {
                i = i + 1;
            }
        }
    }

    public void ClearBullets()
    // Kill all bullets from this enemy gracefully.
    {
        foreach (GameObject bullet in bulletList)
        { bullet.GetComponent<BulletScript>().BulletDeath(); }
    }
}

//****************************************************************************************************************************************************************************************************************************************************

public abstract class BulletScript : MonoBehaviour
// Parent class of all bullets. Provides shared functionality of bullets.
// Children need only implement the two abstract functions specified.
{
    // OBJECTS

    protected static GameObject bulletPlayer = null;
    protected SpriteRenderer bulletSpriteRenderer;
    protected Animator bulletAnimator;

    // PARAMETER VARIABLES

    protected Vector2 bulletParamSpawnPosition;     // The location at which the bullet was spawned.
    protected float bulletParamSpawnTime;           // The time at which the bullet was spawned.

    protected float bulletParamDeathDuration;       // Duration of the animation to play upon bullet death.
    protected string bulletParamDeathAnimationName; // String name of the animation to play upon bullet death.

    // STATE VARIABLES

    protected float bulletStateDeleteTime;          // Bullets will be deleted by their containing BulletManager if this flag is positive and less than Time.time.

    public void Initialize(Vector2 spawnPosition, Dictionary<string, float> config)
    // Initializes base bullet parameters and state. Also find the player if necessary.
    // Then calls bullet-specific initializer.
    {
        // OBJECTS
        if (bulletPlayer == null) { bulletPlayer = GameObject.Find("Player"); }

        bulletSpriteRenderer = GetComponent<SpriteRenderer>();
        bulletAnimator = GetComponent<Animator>();

        // PARAMETER VARIABLES
        bulletParamSpawnPosition = spawnPosition;
        bulletParamSpawnTime = Time.time;

        // STATE VARIABLES
        bulletStateDeleteTime = -1;
        transform.position = spawnPosition;

        // Call child initializer. bulletParamDeathAnimationName and bulletParamDeathDuration should be specified here.
        InitializeSpecific(config);
    }

    protected abstract void InitializeSpecific(Dictionary<string, float> config);
    // Initializes bullet-specific stuff. bulletParamDeathAnimationName and bulletParamDeathDuration should be specified here.

    protected void SetPosition(Vector2 newPosition)
    // Set new bullet position.
    { transform.position = newPosition; }

    protected void SetSpriteRotation(float newTheta)
    // Set new bullet sprite rotation.
    { transform.rotation.eulerAngles.Set(0f, 0f, newTheta); }

    protected void SetScale(float newScale)
    // Set new bullet scale. The sprite and colliders are scaled together.
    { transform.localScale.Set(newScale, newScale, 1); }

    protected void SetColor(float r, float g, float b)
    // Set new bullet sprite renderer color.
    { bulletSpriteRenderer.color = new Color(r, g, b); }

    void Start() { }

    void Update()
    // Unity built-in. Called every frame.
    { UpdateSpecific(); }

    protected abstract void UpdateSpecific();
    // Update function specific to bullet type.

    public bool ReadyForDeletion()
    // Returns true if the bullet has finished its death animation and is ready to be deleted.
    {
        if ((bulletStateDeleteTime > 0) && (Time.time > bulletStateDeleteTime)) { return true; }
        else { return false; }
    }

    public void BulletDeath()
    // Handles events that occur when a bullet dies, usually from hitting a wall.
    {
        // bulletAnimator.Play(bulletParamDeathAnimationName);
        bulletStateDeleteTime = Time.time;// + bulletParamDeathDuration;
    }

    protected void CollisionWall()
    // Handles the events when a bullet hits a wall.
    { BulletDeath(); }

    void OnTriggerStay2D(Collider2D col)
    // Unity built-in. Called every frame during collider collision.
    // Responsible for detecting collisions with walls.
    {
        if (col.gameObject.layer == LayerMask.NameToLayer("Walls")) { CollisionWall(); }
    }
}

//****************************************************************************************************************************************************************************************************************************************************

public class EnemyState
// Object representing an individual enemy state. Contains a pool of attacks to repeatedly randomly sample, as well as conditions for advancing to the next stage.
{
    // PARAMETER VARIABLES
    private List<AttackPattern> stateParamAttacks;
    private int stateParamDamageLimit;
    private float stateParamTimeLimit;

    // STATE VARIABLES
    private int stateStateDamageTarget;
    private float stateStateTimeTarget;

    public EnemyState(List<AttackPattern> attacks, int damage, float time)
    {
        stateParamAttacks = attacks;
        stateParamDamageLimit = damage;
        stateParamTimeLimit = time;
    }

    public List<AttackPattern> Activate(int initialDamage, float initialTime)
    // Initializes the state advancement counters, and returns the attack pattern pool corresponding to this state.
    //This should be done as soon as advancement from the previous state occurs. It is the owner EnemyScript's responsibility to do this.
    {
        stateStateDamageTarget = initialDamage + stateParamDamageLimit;
        stateStateTimeTarget = initialTime + stateParamTimeLimit;
        return (stateParamAttacks);
    }

    public bool CheckAdvancement(int damage, float time)
    // Checks if advancement to the next stage should occur. This function only works correctly on an activated state.
    {
        return ((damage >= stateStateDamageTarget) || (time >= stateStateTimeTarget));
    }
}

//****************************************************************************************************************************************************************************************************************************************************

public abstract class EnemyScript : MonoBehaviour
// Parent of all specific enemy control scripts. Provides shared functionality for enemies.
// Structured as a series of states, each of which contains a pool of attack patterns to repeatedly randomly sample, as well as conditions for advancing to the next stage.
{
    // OBJECTS
    protected BulletManager enemyBullets;

    // PARAMETER VARIABLES
    protected List<Vector2> enemyParamBulletSpawns;
    protected List<EnemyState> enemyParamStates;

    // STATE VARIABLES
    protected int enemyStateStateIndex;
    protected int enemyStateDamageTaken;
    protected float enemyStateFightStartTime;
    protected List<AttackPattern> enemyStateCurrentPool = null;
    protected int enemyStatePatternIndex;
    protected float enemyStatePatternStartTime;
    protected int enemyStateShotIndex;

    void Start()
    // Unity built-in function. Called at the beginning of the game.
    {
        // PARAMETER VARIABLES
        enemyParamBulletSpawns = new List<Vector2>();
        PopulateEnemyBulletSpawnLocations();
        enemyParamStates = new List<EnemyState>();
        PopulateEnemyStates();

        // OBJECTS
        enemyBullets = new BulletManager(enemyParamBulletSpawns);

        // STATE VARIABLES
        enemyStateStateIndex = 0;
        enemyStateDamageTaken = 0;
        enemyStateFightStartTime = -1f;

        StartFight();
    }

    protected abstract void PopulateEnemyBulletSpawnLocations();
    // Fill in enemyParamBulletSpawns.

    protected abstract void PopulateEnemyStates();
    // Fill in enemyParamStates.

    public void StartFight()
    {
        enemyStateFightStartTime = Time.time + 0.01f;
        enemyStateCurrentPool = enemyParamStates[0].Activate(enemyStateDamageTaken, enemyStateFightStartTime);
        enemyStatePatternIndex = Random.Range(0, enemyStateCurrentPool.Count);
        enemyStatePatternStartTime = Time.time;
        enemyStateShotIndex = 0;
    }

    protected void Maintenance()
    // Highest-level general-purpose every-frame maintenance function. Intended to be called from Update only.
    {
        enemyBullets.CullBullets();
    }

    void Update()
    // Unity built-in function. Called every frame.
    {
        if (enemyStateFightStartTime > 0) // Don't do anything if the fight hasn't started yet.
        {

            // Perform maintenance.
            Maintenance();

            // Check for state changes.
            //todo

            // Shoot bullets according to current attack pattern.
            while ((enemyStateShotIndex < enemyStateCurrentPool[enemyStatePatternIndex].GetCount()) && (enemyStateCurrentPool[enemyStatePatternIndex].GetShot(enemyStateShotIndex).shotParamTime + enemyStatePatternStartTime <= Time.time))
            {
                enemyBullets.Shoot(enemyStateCurrentPool[enemyStatePatternIndex].GetShot(enemyStateShotIndex));
                enemyStateShotIndex = enemyStateShotIndex + 1;
            }

            // Switch to new attack pattern if done with current one.
            if (enemyStateCurrentPool[enemyStatePatternIndex].GetDuration() + enemyStatePatternStartTime >= Time.time)
            {
                //todo
            }
        }
    }
}