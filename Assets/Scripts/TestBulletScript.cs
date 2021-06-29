using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestBulletScript : BulletScript
// Simple test of a concrete bullet instance.
{
    private Vector2 velocity;

    protected override void InitializeSpecific(Dictionary<string, float> config)
    // Initializes bullet-specific stuff. bulletParamDeathAnimationName and bulletParamDeathDuration should be specified here.
    {
        bulletParamDeathAnimationName = "test_bullet_death";
        bulletParamDeathDuration = 0.25f;
        velocity = new Vector2(Mathf.Cos(config["theta"]), Mathf.Sin(config["theta"])) * config["speed"];
    }

    protected override void UpdateSpecific()
    // Update function specific to bullet type.
    {
        SetPosition(((Vector2) transform.position) + Time.deltaTime * velocity);
        // Debug.Log(bulletPlayer.transform.position);
    }

}
