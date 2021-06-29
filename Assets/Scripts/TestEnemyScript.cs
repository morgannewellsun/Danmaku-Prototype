using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestEnemyScript : EnemyScript
{
    protected override void PopulateEnemyBulletSpawnLocations()
    {
        enemyParamBulletSpawns.Add(new Vector2(22.5f, -13.5f));
    }

    protected override void PopulateEnemyStates()
    {
        AttackPattern testAttackPattern = new AttackPattern();
        float deltaTime = 0.02f;
        float speed = 4f;
        float maxRadSpeed = 0.5f;
        float theta = 0;
        int nSteps = 1000;
        for (int iteration = 0; iteration < 100; iteration++)
        {
            for (float step = 0f; step < (Mathf.PI * 2); step += ((Mathf.PI * 2) / nSteps))
            {
                float radSpeed = maxRadSpeed * Mathf.Sin(step);
                theta = (theta + radSpeed) % (Mathf.PI * 2);
                Dictionary<string, float> paramDict = new Dictionary<string, float>();
                paramDict["theta"] = theta;
                paramDict["speed"] = speed;
                testAttackPattern.AddShot(deltaTime, 0, 0, paramDict);
            }
        }
        List<AttackPattern> testAttackPatternList = new List<AttackPattern>();
        testAttackPatternList.Add(testAttackPattern);
        enemyParamStates.Add(new EnemyState(testAttackPatternList, 1000000, 100000f));
    }
}
